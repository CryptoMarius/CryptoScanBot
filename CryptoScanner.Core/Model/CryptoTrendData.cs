using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Model;

// Last calculated trend & date for a symbol/interval
// (needs to be calculated each time a candle is finished on a interval)
public class CryptoTrendData
{
    public CandleTime? Time { get; set; }
    public float? Percentage { get; set; } // only for symbol level
    public CryptoTrendIndicator Trend { get; set; }

    // 1 candle back
    public CandleTime? PrevTime { get; set; }
    public float? PrevPercentage { get; set; } // only for symbol level
    public CryptoTrendIndicator LastTrend { get; set; } = CryptoTrendIndicator.Unknown;
    public CryptoTrendIndicator PrevTrend { get; set; }


    public void Reset()
    {
        Time = null;
        Percentage = null;
        Trend = CryptoTrendIndicator.Unknown;

        PrevTime = null;
        PrevPercentage = null;
        LastTrend = CryptoTrendIndicator.Unknown;
        PrevTrend = CryptoTrendIndicator.Unknown;
    }
}
