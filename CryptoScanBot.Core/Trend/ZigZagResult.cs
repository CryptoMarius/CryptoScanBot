using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Trend;

[Serializable]
public class ZigZagResult
{
    public required char PointType { get; set; } // indicates a specific point and type e.g. H or L
    public required double Value { get; set; }
    public required CryptoCandle Candle { get; set; }

    // Some call this Strong or Weak instead of Dominant, its the same concept
    public bool Dominant { get; set; } = false;

    public decimal Top { get; set; }
    public decimal Bottom { get; set; }
    public double Percentage { get; set; }

    public CryptoCandle? InvalidOn { get; set; } = null;

    public int Index { get; set; }

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
