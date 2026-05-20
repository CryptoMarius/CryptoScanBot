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
    //public float? PrevPercentage { get; set; } // only for symbol level
    public CryptoTrendIndicator LastTrend { get; set; } = CryptoTrendIndicator.Unknown;
    public CryptoTrendIndicator PrevTrend { get; set; }

    // Last BOS/CHoCH structure event detected by TrendIntervalBos.
    // Time and Price refer to the swing-point candle at which the break occurred,
    // NOT the candle on which the calculation was last run.
    public CryptoStructureEvent LastStructureEvent { get; set; }
    public CandleTime? LastStructureEventTime { get; set; }
    public decimal? LastStructureEventPrice { get; set; }
    // Tracks the event time of the last fired signal — prevents re-firing on the same event
    public CandleTime? LastFiredStructureEventTime { get; set; }

    // Last confirmed ZigZag pivot — used by AllowStepIn to detect pullbacks after a signal.
    // 'H' = swing high, 'L' = swing low.
    public char? LastPivotType { get; set; }
    public decimal? LastPivotValue { get; set; }
    public CandleTime? LastPivotTime { get; set; }

    // Pivot one before LastPivot — opposite type by ZigZag construction (a high is always
    // preceded by a low and vice versa). Lets consumers reach BOTH the most recent low and
    // the most recent high in one lookup, without walking the ZigZag list. Filled from
    // ZigZagList[^2] when at least two pivots exist; null until the second pivot forms.
    public char? PrevPivotType { get; set; }
    public decimal? PrevPivotValue { get; set; }
    public CandleTime? PrevPivotTime { get; set; }


    public void Reset()
    {
        Time = null;
        Percentage = null;
        Trend = CryptoTrendIndicator.Unknown;

        PrevTime = null;
        //PrevPercentage = null;
        LastTrend = CryptoTrendIndicator.Unknown;
        PrevTrend = CryptoTrendIndicator.Unknown;

        LastStructureEvent = CryptoStructureEvent.None;
        LastStructureEventTime = null;
        LastStructureEventPrice = null;
        LastFiredStructureEventTime = null;

        LastPivotType = null;
        LastPivotValue = null;
        LastPivotTime = null;

        PrevPivotType = null;
        PrevPivotValue = null;
        PrevPivotTime = null;
    }
}
