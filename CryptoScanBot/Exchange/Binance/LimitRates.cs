using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange.Binance;

internal class BinanceWeight
{
    public long Time { get; set; }
    public long Weight { get; set; }

}

/// <summary>
/// Deze class verzorgt een vertraging als je teveel aanvragen doet (via de weight van de actie)
/// JKorf commented on May 1, 2019
/// Hi, I've added the response headers to the WebCallResult object. I also added a helper method to quickly retrieve the used weight header:
/// var weight = client.GetAllOrders("ETHBTC").ResponseHeaders.UsedWeight(); (obviously you should check for errors)
/// </summary>
public static class LimitRates
{
    static public long CurrentWeight { get; set; }
    static private List<BinanceWeight> List { get; } = new List<BinanceWeight>();

    static public void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // Officiele limiet = 1,200 request weight per minute

            // De registraties ouder dan 1 minuut verwijderen
            while (true)
            {
                // Huidige tijd.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Een tijdstip 60 seconden geleden
                long removeBeforeDate = unix - 60;

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
                if (CurrentWeight > 600)
                {
                    GlobalData.AddTextToLogTab($"{Api.ExchangeName} delay needed for weight: {CurrentWeight} (because of rate limits)");
                    Thread.Sleep(2500);
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
