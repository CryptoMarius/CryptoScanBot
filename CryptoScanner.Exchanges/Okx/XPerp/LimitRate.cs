using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Okx.XPerp;

/// <summary>
/// One registered request, so the weight it used can be released again once it falls outside the window.
/// </summary>
internal class WeightEntry
{
    public long Time { get; set; }
    public long Weight { get; set; }
}

/// <summary>
/// Delays a caller when too many requests have gone out recently, counted over a rolling window of
/// 20 seconds. Every market keeps its own counter, the same way Okx Spot and Okx Perpetual do; the
/// budget below is the one the Okx Perpetual market has been running on.
/// https://www.okx.com/docs-v5/en/#overview-rate-limit
/// </summary>
public static class LimitRate
{
    public static long CurrentWeight { get; set; }
    static private List<WeightEntry> List { get; } = [];

    public static void WaitForFairWeight(long newWeight)
    {
        Monitor.Enter(List);
        try
        {
            while (true)
            {
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Drop the registrations that are older than the window, and give back their weight
                long removeBeforeDate = unix - 20;
                while (List.Count > 0)
                {
                    WeightEntry item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                if (CurrentWeight > 300)
                {
                    GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} delay needed for weight: {CurrentWeight} (rate limits)");
                    // Release the lock while waiting. Sleeping inside Monitor.Enter(List) queues
                    // every other fetch thread behind this one, after which they each sleep in
                    // turn instead of re-testing the (by then lowered) weight.
                    Monitor.Exit(List);
                    try
                    {
                        Thread.Sleep(2500);
                    }
                    finally
                    {
                        Monitor.Enter(List);
                    }
                }
                else
                {
                    CurrentWeight += newWeight;

                    // And register the new request
                    DateTimeOffset dateTimeOffset2 = DateTime.UtcNow;
                    WeightEntry item = new()
                    {
                        Time = dateTimeOffset2.ToUnixTimeSeconds(),
                        Weight = newWeight,
                    };
                    List.Add(item);

                    break;
                }
            }
        }
        finally
        {
            Monitor.Exit(List);
        }
    }
}
