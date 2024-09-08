using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trend;

[Serializable]
public class ZigZagResult
{
    public required char PointType { get; set; } // indicates a specific point and type e.g. H or L
    public required double Value { get; set; }
    public required CryptoCandle Candle { get; set; }

    public bool Dominant { get; set; } = false;

    public decimal Top { get; set; }
    public decimal Bottom { get; set; }
    public double Percentage { get; set; }

    public CryptoCandle? InvalidOn { get; set; } = null;

    public void ReusePoint(CryptoCandle candle, double value)
    {
        // Intention is to reset stuff because we are going to reuse a pivot point, clear the other stuff
        Value = value;
        Candle = candle;

        // Reset other stuff
        Dominant = false;
        Top = 0;
        Bottom = 0;
        Percentage = 0;
        InvalidOn = null;
    }
}
