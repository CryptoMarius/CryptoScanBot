using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Mexc.Spot;

internal class MexcWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}
/*
- are there limits for Mexc? Yes, they are called rate limits
  https://mexcdevelop.github.io/apidocs/spot_v3_en/#limits
  It is based on a weight per endpoint, counted per IP address (and a separate one per account)
*/

/// <summary>
/// Delays the caller when too many requests are made (based on the weight of the action).
///
/// Mexc counts per endpoint: "Each endpoint with IP limits has an independent 500 every 10 second
/// limit". The weight of the endpoints this scanner uses:
///   GET /api/v3/klines           weight 1   (Candle.GetCandlesForInterval)
///   GET /api/v3/exchangeInfo     weight 10  (Symbol.GetSymbolsAsync)
///   GET /api/v3/ticker/24hr      weight 40  when asked for every symbol at once (Symbol.GetSymbolsAsync)
///
/// Because the limit is per endpoint and klines is by far the most used one, a single counter over
/// all endpoints is the conservative choice: it can only book more weight than any one endpoint
/// actually used. Exceeding the limit is expensive - Mexc blocks the endpoint for ten minutes - so
/// the boundary stays below the official 500 per 10 seconds.
/// </summary>
public static class LimitRate
{
    public static long CurrentWeight { get; set; }
    static private List<MexcWeight> List { get; } = new List<MexcWeight>();

    public static void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // Official limit = 500 weight per 10 seconds, per endpoint, per IP address

            // Remove the registrations older than the measuring window
            while (true)
            {
                // Current time.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // A moment 10 seconds ago (the window Mexc measures over)
                long removeBeforeDate = unix - 10;

                while (List.Count > 0)
                {
                    MexcWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                // 400 of the 500 allowed weight leaves room for the requests that are already on
                // their way, and for the difference between our clock and the one at the exchange
                // Op de helft van wat de exchange toestaat blijven, zodat een andere applicatie op
                // deze machine (de limiet geldt per IP-adres, niet per proces) er nog naast past.
                // Mexc staat 20 aanvragen per seconde toe (de guard in de library: "Limit of 20 per 00:00:01").
                // 100 per 10 seconden is 10 per seconde, de helft daarvan. Was 400 per 10 seconden = 40
                // per seconde, het dubbele van wat de exchange toestaat - dit getal deed dus niets en de
                // begrenzer van de library was de enige die remde.
                if (CurrentWeight > 100)
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
                    MexcWeight item = new();
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
