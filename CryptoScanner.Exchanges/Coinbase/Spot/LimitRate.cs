using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Coinbase.Spot;

internal class CoinbaseWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}
/*
- are there limits for Coinbase? Yes, they are called rate limits
  https://docs.cdp.coinbase.com/coinbase-app/advanced-trade-apis
  It is a plain request count per second, counted per IP address for the public endpoints
  and per api key for the private ones
*/

/// <summary>
/// Delays the caller when too many requests are made (based on the weight of the action).
///
/// Coinbase counts requests, not weight: 10 per second per IP address for the public endpoints and
/// 30 per second per api key for the private ones (the values the CryptoExchange.Net rate limiter of
/// the package uses as well, CoinbaseRestPublic and CoinbaseRestPrivate). The scanner only calls
/// public endpoints - the symbol list and the candles both have a /market/ variant that needs no
/// credentials - so 10 per second is the boundary that applies, and every call counts as 1.
///
/// Note that the package enforces those same limits itself and waits when needed; this class is the
/// coarser layer in front of it, to keep a burst of candle requests from queuing up there.
///
/// The websocket has limits of its own (750 connections and 8 unauthenticated messages per second,
/// both per IP address); those bound the subscription layout and are documented at the
/// SetDefaultOptions call in Api.
/// </summary>
public static class LimitRate
{
    public static long CurrentWeight { get; set; }
    static private List<CoinbaseWeight> List { get; } = new List<CoinbaseWeight>();

    public static void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // Official limit = 10 requests per second, per IP address, for the public endpoints

            // Remove the registrations older than the measuring window
            while (true)
            {
                // Current time.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // A moment 10 seconds ago. Measuring over ten seconds instead of one keeps a short
                // burst from being punished while the average still lands on the allowed 10 per second
                long removeBeforeDate = unix - 10;

                while (List.Count > 0)
                {
                    CoinbaseWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                // 80 of the 100 requests that fit in the window, which leaves room for the requests
                // that are already on their way and for the difference between our clock and theirs
                if (CurrentWeight > 80)
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
                    CoinbaseWeight item = new();
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
