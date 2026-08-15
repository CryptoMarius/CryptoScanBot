using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

internal class BitvavoWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }
}

/// <summary>
/// Rate limiter for Bitvavo REST API calls.
/// Bitvavo allows 1000 weight per minute. A candle or market request costs 1 weight; the 24 hour
/// ticker costs 25 when it is asked for every market at once, and the callers book it as such.
/// We use a conservative sliding-window limit of 200 per 20 seconds to stay safe.
/// </summary>
public static class LimitRate
{
    public static long CurrentWeight { get; set; }
    static private List<BitvavoWeight> List { get; } = [];

    public static void WaitForFairWeight(long newWeight)
    {
        Monitor.Enter(List);
        try
        {
            while (true)
            {
                DateTimeOffset now = DateTime.UtcNow;
                long unix = now.ToUnixTimeSeconds();

                // Remove entries older than 20 seconds
                long removeBeforeDate = unix - 20;
                while (List.Count > 0)
                {
                    BitvavoWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                // 1000 weight/min => ~333/20sec; stay well under with 200
                if (CurrentWeight > 200)
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
                    BitvavoWeight item = new()
                    {
                        Time = now.ToUnixTimeSeconds(),
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
