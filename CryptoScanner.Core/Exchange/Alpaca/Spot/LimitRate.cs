using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

internal class AlpacaWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }
}

/// <summary>
/// Rate limiter for Alpaca API calls.
/// Free tier: 200 calls per minute.
/// We use a 20-second sliding window with a conservative limit of 50 calls.
/// </summary>
public static class LimitRate
{
    public static long CurrentWeight { get; set; }
    static private List<AlpacaWeight> List { get; } = [];

    public static void WaitForFairWeight(long newWeight)
    {
        Monitor.Enter(List);
        try
        {
            while (true)
            {
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Remove registrations older than 20 seconds
                long removeBeforeDate = unix - 20;
                while (List.Count > 0)
                {
                    AlpacaWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                // Free tier: 200 calls/min => ~67/20sec, stay well under with 50
                if (CurrentWeight > 50)
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
                    AlpacaWeight item = new();
                    DateTimeOffset dateTimeOffset2 = DateTime.UtcNow;
                    item.Time = dateTimeOffset2.ToUnixTimeSeconds();
                    item.Weight = newWeight;
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
