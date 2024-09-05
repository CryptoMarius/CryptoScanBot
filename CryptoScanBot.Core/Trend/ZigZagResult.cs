using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

public class ZigZagResult
{
    public required char PointType { get; set; } // indicates a specific point and type e.g. H or L
    public required double Value { get; set; }
    public required CryptoCandle Candle { get; set; }

    public bool Dominant { get; set; } = false;
    public CryptoCandle? InvalidOn { get; set; } = null;


    public decimal Top { get; set; }
    public decimal Bottom { get; set; }
    public double Percentage { get; set; }
}
