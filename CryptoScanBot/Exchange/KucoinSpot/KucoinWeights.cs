using CryptoScanBot.Intern;

namespace CryptoScanBot.Exchange.KucoinSpot;

internal class KucoinWeight
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
public static class KucoinWeights
{
    static public long CurrentWeight { get; set; }
    static private List<KucoinWeight> List { get; } = new();

    static public void WaitForFairWeight(long newWeight)
    {

        Monitor.Enter(List);
        try
        {
            // https://docs.kucoin.com/#sandbox
            // Officiele limiet = 500/10
            // https://docs.kucoin.com/futures/#requests

            // De registraties ouder dan 1 minuut verwijderen
            while (true)
            {
                // Huidige tijd.
                DateTimeOffset dateTimeOffset = DateTime.UtcNow;
                long unix = dateTimeOffset.ToUnixTimeSeconds();

                // Een tijdstip 20 seconden geleden (ook reeds ruim genomen)
                long removeBeforeDate = unix - 10;

                while (List.Count > 0)
                {
                    KucoinWeight item = List[0];
                    if (item.Time <= removeBeforeDate)
                    {
                        CurrentWeight -= item.Weight;
                        List.RemoveAt(0);
                    }
                    else break;
                }

                // To many request is het enige wat ik tetug zie komen?

                if (CurrentWeight > 50)
                {
                    GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} delay needed for weight: {CurrentWeight} (because of rate limits)");
                    Thread.Sleep(2500);
                }
                else
                {
                    CurrentWeight += newWeight;

                    // En een nieuwe registratie toevoegen
                    KucoinWeight item = new();
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
