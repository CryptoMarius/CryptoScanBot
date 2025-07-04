using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Model;

// Last calculated trend & date for a symbol/interval
// (needs to be calculated each time a candle is finished on a interval)
public class CryptoTrendData
{
    public long? Time { get; set; }
    public float? Percentage { get; set; } // only for symbol level
    public CryptoTrendIndicator Trend { get; set; }

    // 1 candle back 
    public long? PrevTime { get; set; }
    public float? PrevPercentage { get; set; } // only for symbol level
    public CryptoTrendIndicator PrevTrend { get; set; }


    public void Reset()
    {
        Time = null;
        Percentage = null;
        Trend = CryptoTrendIndicator.Sideways;

        PrevTime = null;
        PrevPercentage = null;
        PrevTrend = CryptoTrendIndicator.Sideways;
    }
}
