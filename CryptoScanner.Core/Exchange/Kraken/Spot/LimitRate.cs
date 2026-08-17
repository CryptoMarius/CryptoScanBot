using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Kraken.Spot;

internal class KrakenWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}

/// <summary>
/// Delays the caller when too many requests are sent to the exchange, counted as weight over a
/// moving window of 20 seconds.
///
/// Kraken states about one request per second for the public endpoints
/// (https://docs.kraken.com/api/docs/guides/spot-rest-ratelimits), but that is the sustained rate
/// they guarantee, not the point where requests start failing: a burst of 20 OHLC requests measured
/// on 14-08-2026 was served at 9 requests per second without a single "EAPI:Rate limit exceeded".
/// The counter based limit of the private endpoints (a tier dependent maximum of 15 to 20 that
/// decays 0.33 to 1 per second) does not apply here - the scanner only reads public data.
///
/// The real guard is the rate limiter of the client library (KrakenExchange.RateLimiter, which knows
/// the limit per endpoint and is hooked up in Api.ExchangeDefaults). This class is the coarse brake
/// in front of it, keeping a burst of parallel candle fetches within what the exchange was measured
/// to accept.
/// </summary>
public static class LimitRate
{
    // Weight per 20 seconds, so 20 is 1 request per second - the sustained rate the exchange
    // documents for the public endpoints.
    //
    // This used to be 200 (10 per second), on the strength of the burst measurement in the remarks
    // above. That measurement does not survive a real run: the night of 17-08-2026 produced 667
    // "Too many requests" over 3.6 hours, about 13 per minute and spread evenly over the whole run
    // instead of piling up at the start. No other exchange came above 12 error lines in total. The
    // load itself is unremarkable - 1299 candle requests, where Coinbase did 3262 without a single
    // complaint - so the burst is what Kraken objects to, not the volume. A cold start fires the
    // fetch threads of dozens of symbols at once and ten of them go out per second.
    //
    // One per second is no bottleneck: those 1299 requests over 3.6 hours average 0.1 per second,
    // so the brake only flattens the peaks.
    private const long MaximumWeightPerWindow = 20;

    public static long CurrentWeight { get; set; }
    static private List<KrakenWeight> List { get; } = new List<KrakenWeight>();

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
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // A moment 20 seconds ago
                long removeBeforeDate = unix - 20;

                while (List.Count > 0)
                {
                    KrakenWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                if (CurrentWeight > MaximumWeightPerWindow)
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

                    // And add a new registration
                    KrakenWeight item = new();
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
