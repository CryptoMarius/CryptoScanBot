using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Choch;

/// <summary>
/// Long signal on a Change of Character: the ZigZag-derived structure has reversed from
/// Bearish to Bullish (a Higher High was made in a previous downtrend).
///
/// Subclasses pick the trend-type slot (Primary / Secondary) and decide whether the signal
/// fires immediately on the CHoCH event (direct), or only after a confirmed pullback +
/// breakthrough back through the pullback pivot (pullback variant).
/// </summary>
public abstract class SignalChochLongBase : SignalCreateBase
{
    /// <summary>Selects which trend slot (Primary vs Secondary) this signal reads from.</summary>
    protected abstract TrendType TrendType { get; }

    /// <summary>
    /// When true, IsSignal waits for an opposite ZigZag pivot AFTER the CHoCH event and
    /// for the current candle to break back through that pivot in the CHoCH direction.
    /// Effect: the signal fires LATER than the direct variant (often several candles later).
    /// </summary>
    protected virtual bool RequirePullback => false;

    private SettingsZigZag TrendSettings => TrendType == TrendType.Primary
        ? GlobalData.Settings.Trend.Primary
        : GlobalData.Settings.Trend.Secondary;

    private CryptoTrendData GetBosTrend() => TrendType == TrendType.Primary
        ? SymbolInterval.TrendBosPrimary
        : SymbolInterval.TrendBosSecondary;

    private decimal? _overrideSignalPrice;
    public override decimal? OverrideSignalPrice => _overrideSignalPrice;


    public override bool IsSignal()
    {
        // Skip the very noisy lower timeframes — mirrors SignalTrend
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, TrendSettings).Result;

        CryptoTrendData data = GetBosTrend();

        // Must be an active CHoCH event reported by TrendIntervalBos
        if (data.LastStructureEvent != CryptoStructureEvent.ChoCh ||
            data.LastStructureEventTime == null)
        {
            ExtraText = "no CHoCH";
            return false;
        }

        // The reversal must point in our direction
        if (data.Trend != CryptoTrendIndicator.Bullish)
        {
            ExtraText = $"CHoCH but trend={data.Trend}";
            return false;
        }

        // Warm-start guard: on the very first evaluation of this strategy after a scanner
        // restart, LastFiredStructureEventTimes is empty (it's in-memory only). Without this
        // check the scanner would re-emit historical CHoCH events that the user already saw
        // before the restart. Adopt the current event silently and let only NEW CHoCH events
        // (with a later LastStructureEventTime) fire from this point on.
        if (!data.LastFiredStructureEventTimes.ContainsKey(SignalStrategy))
        {
            data.LastFiredStructureEventTimes[SignalStrategy] = data.LastStructureEventTime.Value;
            ExtraText = "warm start: existing CHoCH adopted silently";
            return false;
        }

        // Fire only once per CHoCH event per strategy. Direct and pullback variants share
        // this trend-data slot, but each tracks its own fire-time so they don't block
        // each other.
        if (data.LastFiredStructureEventTimes.TryGetValue(SignalStrategy, out var firedAt) &&
            firedAt == data.LastStructureEventTime.Value)
        {
            ExtraText = "CHoCH already fired";
            return false;
        }

        if (RequirePullback)
        {
            // Pullback variant — wait for a NEW ZigZag Low to form AFTER the CHoCH event
            // and for the current candle to close back above that pullback pivot. This
            // mirrors the SMC "CHoCH then retest" play: take the entry on the confirmed
            // break of the pullback's high, not on the CHoCH itself.
            if (data.LastPivotType != 'L' ||
                data.LastPivotTime == null ||
                data.LastPivotTime <= data.LastStructureEventTime.Value)
            {
                ExtraText = "waiting for pullback pivot (ZigZag Low after CHoCH)";
                return false;
            }

            if (CandleLast.Candle.Close <= data.LastPivotValue)
            {
                ExtraText = $"price {CandleLast.Candle.Close:N8} not above pullback low {data.LastPivotValue:N8}";
                return false;
            }

            if (CandleLast.Candle.Close <= CandleLast.Candle.Open)
            {
                ExtraText = "no bullish candle";
                return false;
            }

            data.LastFiredStructureEventTimes[SignalStrategy] = data.LastStructureEventTime.Value;
            // Pullback variant: signal price = current candle close (the breakthrough),
            // NOT the original CHoCH swing price — those are several candles apart now.
            _overrideSignalPrice = null;
            ExtraText = $"CHoCH pullback → Bullish ({TrendType})";
            return true;
        }

        // Direct variant — fire immediately on the CHoCH event
        data.LastFiredStructureEventTimes[SignalStrategy] = data.LastStructureEventTime.Value;
        _overrideSignalPrice = data.LastStructureEventPrice;
        ExtraText = $"CHoCH → Bullish ({TrendType})";
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // Setup invalidated when the BOS trend has flipped back to Bearish — applies to
        // both variants (a direct entry waiting to step in OR a pullback entry that
        // already fired but the trader hasn't picked up yet).
        if (GetBosTrend().Trend == CryptoTrendIndicator.Bearish)
        {
            ExtraText = $"BOS trend ({TrendType}) reverted to bearish";
            return true;
        }

        return base.GiveUp(signal);
    }
}
