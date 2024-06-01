namespace CryptoScanBot.Core.Trend;

public class ZigZagResult
{
    public string PointType { get; set; } // indicates a specific point and type e.g. H or L
    public double Value { get; set; }

    // To avoid reference to the candle (because of GC)
    public DateTime Date { get; set; }
    public double? Rsi { get; set; }
    //public CryptoCandle Candle { get; set; }
}
