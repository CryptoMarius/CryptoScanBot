namespace CryptoSbmScanner.Model;

public enum CryptoTrendIndicator
{
    trendSideways,
    trendBullish,
    trendBearish
}

public class CryptoSymbolInterval
{
    // Het interval
    public CryptoInterval Interval { get; set; }

    // De laatste datum dat de candles aansluiten c.q. zijn gesynchroniseerd met de exchange
    public long? LastCandleSynchronized { get; set; }

    // Bewaar de laatste trend
    public CryptoTrendIndicator TrendIndicator { get; set; }
    public DateTime? TrendInfoDate { get; set; }

    // De candles voor dit interval
    public SortedList<long, CryptoCandle> CandleList { get; set; } = new SortedList<long, CryptoCandle>();
}
