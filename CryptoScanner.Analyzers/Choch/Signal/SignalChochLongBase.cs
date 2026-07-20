using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

#if DEBUG
namespace CryptoScanner.Analyzers.Choch.Signal;

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


    // Tracks (symbol, interval, strategy) combos that already logged "no CHoCH" so we
    // don't flood the log with the same message every candle.
    private static readonly HashSet<(string, string, CryptoSignalStrategy)> _loggedNoChoch = [];

    public static void ResetDiagnosticLog()
    {
        _loggedNoChoch.Clear();
        SignalChochShortBase.ResetDiagnosticLog();
    }

    public override bool IsSignal()
    {
        // Skip the very noisy lower timeframes — mirrors SignalTrend
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval5m)
            return false;

        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, TrendSettings).Result;

        CryptoTrendData trendData = GetBosTrend();
        bool debugLog = GlobalData.Settings.General.DebugSignalCreate;

        bool requireBos = RequirePullback && ChochPlugin.Settings.RequireBosConfirmation;
        var lastChoCh = trendData.LastChoCh();
        if (lastChoCh == null)
        {
            ExtraText = "no CHoCH";
            if (debugLog && _loggedNoChoch.Add((Symbol.Name, Interval.Name, SignalStrategy)))
                ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} long: {ExtraText} (events={trendData.StructureEvents.Count}, trend={trendData.Trend})");
            return false;
        }

        if (trendData.Trend != CryptoTrendIndicator.Bullish)
        {
            ExtraText = $"CHoCH but trend={trendData.Trend}";
            if (debugLog)
                ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} long: {ExtraText} (eventTime={lastChoCh.Time.ToDateTime()})");
            return false;
        }

        // Warm-start guard: on the very first evaluation after a restart,
        // adopt the current event silently so only NEW events fire.
        if (!trendData.LastFiredStructureEventTimes.ContainsKey(SignalStrategy))
        {
            trendData.LastFiredStructureEventTimes[SignalStrategy] = lastChoCh.Time;
            ExtraText = "warm start: existing CHoCH adopted silently";
            if (debugLog)
                ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} long: {ExtraText} (eventTime={lastChoCh.Time.ToDateTime()}, trend={trendData.Trend})");
            return false;
        }

        if (trendData.LastFiredStructureEventTimes.TryGetValue(SignalStrategy, out var firedAt) &&
            firedAt == lastChoCh.Time)
        {
            ExtraText = "CHoCH already fired";
            return false;
        }

        if (debugLog)
            ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} long: NEW event! firedAt={firedAt.ToDateTime()} new={lastChoCh.Time.ToDateTime()} trend={trendData.Trend}");

        if (RequirePullback)
        {
            CandleTime pivotAfter = requireBos
                ? firedAt
                : lastChoCh.Time;
            if (trendData.LastPivotType != 'L' ||
                trendData.LastPivotTime == null ||
                trendData.LastPivotTime <= pivotAfter)
            {
                ExtraText = "waiting for pullback pivot (ZigZag Low after CHoCH)";
                return false;
            }

            if (requireBos && !trendData.HasBosAfterLastChoCh())
            {
                ExtraText = "waiting for BOS confirmation after CHoCH";
                return false;
            }

            if (CandleLast.Candle.Close <= trendData.LastPivotValue)
            {
                ExtraText = $"price {CandleLast.Candle.Close:N8} not above pullback low {trendData.LastPivotValue:N8}";
                return false;
            }

            if (CandleLast.Candle.Close <= CandleLast.Candle.Open)
            {
                ExtraText = "no bullish candle";
                return false;
            }

            trendData.LastFiredStructureEventTimes[SignalStrategy] = lastChoCh.Time;
            _overrideSignalPrice = null;
            ExtraText = $"CHoCH pullback → Bullish ({TrendType})";
            return true;
        }

        // Direct variant — fire immediately on the CHoCH event
        trendData.LastFiredStructureEventTimes[SignalStrategy] = lastChoCh.Time;
        _overrideSignalPrice = lastChoCh.Price;
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
#endif