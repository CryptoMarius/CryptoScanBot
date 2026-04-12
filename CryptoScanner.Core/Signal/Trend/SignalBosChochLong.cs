using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

/// <summary>
/// Fires a Long signal on a bullish BOS (Break of Structure) or bullish CHoCH (Change of Character).
///
/// CHoCH Long: a Higher High in a bearish trend → trend reversal to bullish.
/// BOS Long:   a Higher High in an already bullish trend → continuation confirmed.
///
/// Uses TrendBos (BOS/CHoCH algorithm), which reacts faster than Dow Theory
/// at the cost of potentially more reversals.
/// </summary>
public class SignalBosChochLong : SignalCreateBase
{
    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendBos;

        // Only fire on a CHoCH (reversal to bullish), not on a BOS (continuation)
        if (data.LastStructureEvent != CryptoStructureEvent.ChoCh
            || data.LastStructureEventTime == null
            || data.Trend != CryptoTrendIndicator.Bullish)
        {
            ExtraText = "no bullish CHoCH";
            return false;
        }

        // Don't fire again on the same structural event
        if (data.LastFiredStructureEventTime == data.LastStructureEventTime)
        {
            ExtraText = "already fired for this event";
            return false;
        }

        data.LastFiredStructureEventTime = data.LastStructureEventTime;
        ExtraText = "CHoCH Long (reversal)";
        return true;
    }
}
