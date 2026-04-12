using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

/// <summary>
/// Fires a Short signal on a bearish BOS (Break of Structure) or bearish CHoCH (Change of Character).
///
/// CHoCH Short: a Lower Low in a bullish trend → trend reversal to bearish.
/// BOS Short:   a Lower Low in an already bearish trend → continuation confirmed.
///
/// Uses TrendBos (BOS/CHoCH algorithm), which reacts faster than Dow Theory
/// at the cost of potentially more reversals.
/// </summary>
public class SignalBosChochShort : SignalCreateBase
{
    public override bool IsSignal()
    {
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        CryptoTrendData data = SymbolInterval.TrendBos;

        // Only fire on a CHoCH (reversal to bearish), not on a BOS (continuation)
        if (data.LastStructureEvent != CryptoStructureEvent.ChoCh
            || data.LastStructureEventTime == null
            || data.Trend != CryptoTrendIndicator.Bearish)
        {
            ExtraText = "no bearish CHoCH";
            return false;
        }

        // Don't fire again on the same structural event
        if (data.LastFiredStructureEventTime == data.LastStructureEventTime)
        {
            ExtraText = "already fired for this event";
            return false;
        }

        data.LastFiredStructureEventTime = data.LastStructureEventTime;
        ExtraText = "CHoCH Short (reversal)";
        return true;
    }
}
