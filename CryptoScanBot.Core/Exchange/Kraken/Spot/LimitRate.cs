using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

internal class KrakenWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}
/*
- zijn er limiten voor bybit? Ja, dat heet rate limits
  https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-ratelimits
  het is gebaseerd op het aantal verzoeken per seconde wat je naar exchange stuurt
  Je krijgt deze informatie ook terug zo te zien, eens zien wat het is
*/

/// <summary>
/// Deze class verzorgt een vertraging als je teveel aanvragen doet (via de weight van de actie)
/// JKorf commented on May 1, 2019
/// Hi, I've added the response headers to the WebCallResult object. I also added a helper method to quickly retrieve the used weight header:
/// var weight = client.GetAllOrders("ETHBTC").ResponseHeaders.UsedWeight(); (obviously you should check for errors)
/// </summary>
public static class LimitRate
{
    static public long CurrentWeight { get; set; }
    static private List<KrakenWeight> List { get; } = new List<KrakenWeight>();

    static public void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // https://bybit-exchange.github.io/docs/v5/rate-limit
            // Officiele limiet = 120 requests per second for 5 consecutive seconds

            // De registraties ouder dan 1 minuut verwijderen
            while (true)
            {
                // Huidige tijd.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Een tijdstip 20 seconden geleden (ook reeds ruim genomen)
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

                // Het is nu een beetje gokken, 120*5 = 600 calls, met 300 per 20 sec blijven we daar RUIM onder lijkt me
                // Maar het is ook niet plezierig om gebanned te worden, dus begin maar ietwat voorzichtig lijkt me..
                if (CurrentWeight > 300)
                {
                    GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} delay needed for weight: {CurrentWeight} (because of rate limits)");
                    Thread.Sleep(2500);
                }
                else
                {
                    CurrentWeight += newWeight;

                    // En een nieuwe registratie toevoegen
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
