using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Analyzers.Choch.Signal;

/// <summary>
/// Short signal on a Change of Character: the ZigZag-derived structure has reversed from
/// Bullish to Bearish (a Lower Low was made in a previous uptrend).
/// </summary>
public abstract class SignalChochShortBase : SignalCreateBase
{
    /// <summary>Selects which trend slot (Primary vs Secondary) this signal reads from.</summary>
    protected abstract TrendType TrendType { get; }

    /// <summary>
    /// When true, IsSignal waits for an opposite ZigZag pivot AFTER the CHoCH event and
    /// for the current candle to break back through that pivot in the CHoCH direction.
    /// Effect: the signal fires LATER than the direct variant.
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


    private static readonly HashSet<(string, string, string)> _loggedNoChoch = [];

    public static void ResetDiagnosticLog()
    {
        _loggedNoChoch.Clear();
    }

    public override bool IsSignal()
    {
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
                ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} short: {ExtraText} (events={trendData.StructureEvents.Count}, trend={trendData.Trend})");
            return false;
        }

        if (trendData.Trend != CryptoTrendIndicator.Bearish)
        {
            ExtraText = $"CHoCH but trend={trendData.Trend}";
            if (debugLog)
                ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} short: {ExtraText} (eventTime={lastChoCh.Time.ToDateTime()})");
            return false;
        }

        if (!trendData.LastFiredStructureEventTimes.ContainsKey(SignalStrategy))
        {
            trendData.LastFiredStructureEventTimes[SignalStrategy] = lastChoCh.Time;
            ExtraText = "warm start: existing CHoCH adopted silently";
            if (debugLog)
                ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} short: {ExtraText} (eventTime={lastChoCh.Time.ToDateTime()}, trend={trendData.Trend})");
            return false;
        }

        if (trendData.LastFiredStructureEventTimes.TryGetValue(SignalStrategy, out var firedAt) &&
            firedAt == lastChoCh.Time)
        {
            ExtraText = "CHoCH already fired";
            return false;
        }

        if (debugLog)
            ScannerLog.Logger.Info($"CHoCH diag {Symbol.Name} {Interval.Name} {SignalStrategy} short: NEW event! firedAt={firedAt.ToDateTime()} new={lastChoCh.Time.ToDateTime()} trend={trendData.Trend}");

        if (RequirePullback)
        {
            CandleTime pivotAfter = requireBos
                ? firedAt
                : lastChoCh.Time;
            if (trendData.LastPivotType != 'H' ||
                trendData.LastPivotTime == null ||
                trendData.LastPivotTime <= pivotAfter)
            {
                ExtraText = "waiting for pullback pivot (ZigZag High after CHoCH)";
                return false;
            }

            if (requireBos && !trendData.HasBosAfterLastChoCh())
            {
                ExtraText = "waiting for BOS confirmation after CHoCH";
                return false;
            }

            if (CandleLast.Candle.Close >= trendData.LastPivotValue)
            {
                ExtraText = $"price {CandleLast.Candle.Close:N8} not below pullback high {trendData.LastPivotValue:N8}";
                return false;
            }

            if (CandleLast.Candle.Close >= CandleLast.Candle.Open)
            {
                ExtraText = "no bearish candle";
                return false;
            }

            trendData.LastFiredStructureEventTimes[SignalStrategy] = lastChoCh.Time;
            _overrideSignalPrice = null;
            ExtraText = $"CHoCH pullback → Bearish ({TrendType})";
            return true;
        }

        trendData.LastFiredStructureEventTimes[SignalStrategy] = lastChoCh.Time;
        _overrideSignalPrice = lastChoCh.Price;
        ExtraText = $"CHoCH → Bearish ({TrendType})";
        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // Setup invalidated when the BOS trend has flipped back to Bullish.
        if (GetBosTrend().Trend == CryptoTrendIndicator.Bullish)
        {
            ExtraText = $"BOS trend ({TrendType}) reverted to bullish";
            return true;
        }

        return base.GiveUp(signal);
    }
}
