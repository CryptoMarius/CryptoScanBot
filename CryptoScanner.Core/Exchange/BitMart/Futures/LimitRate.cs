using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.BitMart.Futures;

internal class BitMartWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}

/// <summary>
/// Delays the caller when too many requests are sent to the exchange, counted as weight over a
/// moving window of 2 seconds.
///
/// BitMart counts requests per endpoint per IP address over a sliding window
/// (https://developer-pro.bitmart.com/en/futuresv2/), the endpoint this scanner leans on being:
///   GET /contract/public/kline    12 times / 2 seconds  (Candle.GetCandlesForInterval)
///
/// The client library knows that same number and waits by itself (BitMartExchange.RateLimiter,
/// hooked up in Api.ExchangeDefaults), but it books a request the moment it sends it while the
/// exchange counts the moment it arrives. Running at exactly 12 per 2 seconds therefore still
/// collects a "Server rate limit exceeded" now and then - which is what happened during the candle
/// catch-up at startup, where five fetch threads keep the kline endpoint busy without a pause
/// (16-08-2026: three refused requests, both sessions, always in the first seconds).
/// This class is the coarse brake in front of it: 10 of the 12 calls, so the boundary itself is
/// never touched.
///
/// Unlike the other exchanges the registrations are kept in milliseconds. A window of 2 seconds
/// measured in whole seconds would hold a registration anywhere between 2 and 3 seconds, which
/// costs up to a third of the allowed speed for no reason at all.
/// </summary>
public static class LimitRate
{
    // Weight per window, of the 12 requests BitMart allows for the kline endpoint
    private const long MaximumWeightPerWindow = 10;

    // The window BitMart measures over
    private const long WindowMilliseconds = 2000;

    public static long CurrentWeight { get; set; }
    static private List<BitMartWeight> List { get; } = new List<BitMartWeight>();

    // Waiting is normal operation here, so the log is throttled to one line per minute
    private static DateTime LastLogged = DateTime.MinValue;

    public static void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // Remove the registrations older than the window
            while (true)
            {
                // Current time
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeMilliseconds();

                // A moment 2 seconds ago
                long removeBeforeDate = unix - WindowMilliseconds;

                while (List.Count > 0)
                {
                    BitMartWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                if (CurrentWeight + newWeight > MaximumWeightPerWindow && List.Count > 0)
                {
                    // Wait until the oldest registration leaves the window, no longer than that.
                    // The extra 25 ms keeps a rounding difference from waking us a tick too early.
                    long delay = List[0].Time - removeBeforeDate + 25;

                    DateTime now = DateTime.UtcNow;
                    if (now - LastLogged >= TimeSpan.FromMinutes(1))
                    {
                        LastLogged = now;
                        GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} delay needed for weight: {CurrentWeight} (rate limits, throttled log)");
                    }

                    // Release the lock while waiting. Sleeping inside Monitor.Enter(List) queues
                    // every other fetch thread behind this one, after which they each sleep in
                    // turn instead of re-testing the (by then lowered) weight.
                    Monitor.Exit(List);
                    try
                    {
                        Thread.Sleep((int)delay);
                    }
                    finally
                    {
                        Monitor.Enter(List);
                    }
                }
                else
                {
                    CurrentWeight += newWeight;

                    // And add a new registration
                    BitMartWeight item = new();
                    DateTimeOffset dateTimeOffset2 = DateTime.UtcNow;
                    item.Time = dateTimeOffset2.ToUnixTimeMilliseconds();
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
