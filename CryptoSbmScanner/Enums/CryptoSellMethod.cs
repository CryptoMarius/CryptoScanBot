namespace CryptoSbmScanner.Enums;

public enum CryptoSellMethod
{
    FixedPercentage, // Het opgegeven vaste percentage
    DynamicPercentage, // Het percentage adhv BB breedte
    TrailViaKcPsar // stop limit sell op de bovenste KC/PSAR
}
