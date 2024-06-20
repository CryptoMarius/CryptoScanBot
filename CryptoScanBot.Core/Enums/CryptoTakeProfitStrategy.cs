namespace CryptoScanBot.Core.Enums;

public enum CryptoTakeProfitStrategy
{
    FixedPercentage, // Het opgegeven vaste percentage
    TrailViaKcPsar, // stop limit sell op de bovenste KC/PSAR
    //DynamicPercentage, // Het percentage adhv BB breedte
}
