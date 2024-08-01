using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagResult
{
    public char PointType { get; set; } // indicates a specific point and type e.g. H or L
    public double Value { get; set; }

    public CryptoCandle? Candle { get; set; }
}
