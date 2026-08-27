using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Binance.Perpetual;

internal class BinanceWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}

/// <summary>
/// Deze class verzorgt een vertraging als je teveel aanvragen doet (via de weight van de actie)
/// JKorf commented on May 1, 2019
/// Hi, I've added the response headers to the HttpResult object. I also added a helper method to quickly retrieve the used weight header:
/// var weight = client.GetAllOrders("ETHBTC").ResponseHeaders.UsedWeight(); (obviously you should check for errors)
/// </summary>
public static class LimitRate
{
    /// <summary>Half of what the exchange allows per minute, in request weight.</summary>
    private const long MaximumWeightPerWindow = 1200;
    private const long WindowSeconds = 60;

    /// <summary>
    /// Weight of one klines request. Binance scales it with the number of candles asked for and
    /// this scanner always asks for the maximum, which is the most expensive bracket.
    /// </summary>
    public const long KlineWeight = 10;

    public static long CurrentWeight { get; set; }
    static private List<BinanceWeight> List { get; } = [];

    public static void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // Binance Perpetual allows 2400 request WEIGHT per minute per IP address (the guard inside the
            // library: "Limit of 2400 per 00:01:00"). We take half of that, because the limit counts
            // per ADDRESS and not per process: nineteen scanners run on this machine and another
            // trading application has to fit next to them.
            //
            // Weight, not requests. This used to be 600 per SECOND while counting every call as one
            // unit, and that is the wrong measure here: Binance charges a klines call by the number
            // of candles asked for, so the thousand-candle requests this scanner lives on are worth
            // far more than one. The callers pass the real weight (see KlineWeight).

            // De registraties ouder dan 1 minuut verwijderen
            while (true)
            {
                // Huidige tijd.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Een tijdstip 60 seconden geleden
                long removeBeforeDate = unix - WindowSeconds;

                while (List.Count > 0)
                {
                    BinanceWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                // De officiele limiet is 1200. maar daar zit ik regelmatig boven, daarom drastisch terug gezet naar 600
                // (er draaien ook diverse taken en socket streams die de nodige weight gebruiken, dus lager is veiliger)
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

                    // En een nieuwe registratie toevoegen
                    BinanceWeight item = new();
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
