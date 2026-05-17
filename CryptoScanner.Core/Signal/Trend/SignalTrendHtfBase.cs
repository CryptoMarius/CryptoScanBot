#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal.Trend;

/// <summary>
/// Trend-CONTINUATION strategy. Enters in the direction of the already-established
/// trend, after a pullback. Composes three independently toggleable filters on top
/// of a pullback-break entry:
///
///   1. Optional HTF bias — Primary trend on the next 1-2 higher timeframes must agree
///      with the signal direction (or be sideways). Triple Screen / Dow tide.
///   2. Optional ADX regime — current-interval ADX must indicate a real trend, not chop.
///   3. Continuation trigger — TrendBos.Trend on the active interval matches our direction,
///      the most recent ZigZag pivot is the pullback extreme (L for long, H for short),
///      and the current candle is the first to close past that pivot.
///
/// Step-in moment is the breakout candle close (clean entry statistics).
///
/// NOTE: this strategy is intentionally NOT triggered on CHoCH or BOS *events*.
/// CHoCH is a reversal trigger (already covered by SignalBosChoch); BOS events are
/// not persisted by TrendIntervalBos. Instead it reads the established trend and the
/// latest pullback pivot directly — which yields many more entry opportunities in
/// trending markets and avoids the "event too old" starvation a reversal-only trigger
/// causes during long trending phases.
///
/// Subclasses provide directional details via the three abstract members below.
/// </summary>
public abstract class SignalTrendHtfBase : SignalCreateBase
{
    /// <summary>Direction-specific: Bullish for Long subclass, Bearish for Short.</summary>
    protected abstract CryptoTrendIndicator RequiredTrend { get; }

    /// <summary>Direction-specific: 'L' (swing-low pullback) for Long, 'H' for Short.</summary>
    protected abstract char ExpectedPivotType { get; }

    /// <summary>Did the current candle close break past the pullback pivot in our direction?</summary>
    protected abstract bool IsBreakConfirmed(decimal close, decimal pivotValue);


    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        var settings = GlobalData.Settings.Signal.TrendHtf;

        // All log lines start with this prefix so grepping the LogTab for a specific
        // symbol/interval/side gives a clean filter ribbon.
        string tag = $"TrendHtf {SignalSide} {Symbol.Name} {Interval.Name} {CandleLast.Candle.OpenTime.ToDateTime().ToLocalTime():HH:mm}";

        //// Coarse interval gate — same as the existing trend strategies.
        //if (Interval.IntervalPeriod <= CryptoIntervalPeriod.interval3m)
        //{
        //    // Skipped silently — debug log not even emitted to avoid flooding for 1m/3m candles.
        //    return false;
        //}

        // Make sure TrendPrimary + TrendBos are up to date for every interval of this symbol.
        // Both the HTF bias check (TrendPrimary on higher intervals) and the trend/pivot read
        // (TrendBos on this interval) depend on this run.
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;

        // ---- Filter 1: HTF bias ----
        if (settings.HtfFilterEnabled)
        {
            // Guard against the noise on the low timeframes
            var period = Interval.IntervalPeriod;
            if (period < CryptoIntervalPeriod.interval5m)
                period = CryptoIntervalPeriod.interval5m;

            HtfBias bias = TrendBiasTools.GetHtfBias(Symbol, period, settings.HtfLevels, out string biasExplanation);
            bool allowed = RequiredTrend == CryptoTrendIndicator.Bullish
                ? TrendBiasTools.AllowsLong(bias)
                : TrendBiasTools.AllowsShort(bias);

            GlobalData.AddTextToLogTab($"{tag} HTF {biasExplanation} allowed={allowed}");

            if (!allowed)
            {
                ExtraText = $"HTF bias {bias} blocks {SignalSide} ({biasExplanation})";
                GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText}");
                return false;
            }
        }

        // ---- Filter 2: ADX regime ----
        if (settings.AdxFilterEnabled)
        {
            double? adx = CandleLast.CandleData?.Adx;
            GlobalData.AddTextToLogTab($"{tag} ADX={adx?.ToString("N1") ?? "null"} min={settings.AdxMinValue:N1}");

            if (!TrendRegimeTools.IsTrending(adx, settings.AdxMinValue))
            {
                ExtraText = adx == null
                    ? "ADX not available"
                    : $"ADX {adx.Value:N1} below {settings.AdxMinValue:N1} (ranging)";
                GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText}");
                return false;
            }
        }

        // ---- Continuation trigger: trend + pullback pivot + fresh break ----
        CryptoTrendData data = SymbolInterval.TrendBos;

        GlobalData.AddTextToLogTab($"{tag} TrendBos trend={data.Trend} pivot={data.LastPivotType?.ToString() ?? "null"}@{data.LastPivotValue:N8} pivotTime={data.LastPivotTime?.ToDateTime().ToLocalTime():HH:mm}");

        // Trend on the active interval must align with our side.
        if (data.Trend != RequiredTrend)
        {
            ExtraText = $"TrendBos {data.Trend}, need {RequiredTrend}";
            GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText}");
            return false;
        }

        // Most recent ZigZag pivot must be the pullback extreme in our direction
        // (Low for long entries, High for short entries).
        if (data.LastPivotType != ExpectedPivotType ||
            data.LastPivotValue == null ||
            data.LastPivotTime == null)
        {
            ExtraText = $"waiting for pullback pivot (need ZigZag {ExpectedPivotType}, have {data.LastPivotType?.ToString() ?? "null"})";
            GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText}");
            return false;
        }

        // Anti-stale: protect against firing on ancient pivots after a bot restart that
        // loads historical data where price has been past the pivot the entire time.
        CandleTime cutoff = CandleLast.Candle.OpenTime - settings.MaxEventAgeCandles * Interval.Duration;
        if (data.LastPivotTime < cutoff)
        {
            ExtraText = "pullback pivot too old";
            GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText} (pivotTime < cutoff {cutoff.ToDateTime().ToLocalTime():HH:mm}, maxAge={settings.MaxEventAgeCandles} candles)");
            return false;
        }

        // Current candle must close past the pivot in our direction.
        decimal pivotValue = data.LastPivotValue.Value;
        if (!IsBreakConfirmed(CandleLast.Candle.Close, pivotValue))
        {
            ExtraText = $"waiting for break past pullback {ExpectedPivotType} {pivotValue:N8}";
            GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText} (close={CandleLast.Candle.Close:N8})");
            return false;
        }

        // Anti-duplicate: only fire on the FIRST candle that breaks the pivot. If the previous
        // candle's close was already past it, the break is not fresh — either we already fired
        // and the bar advanced, or we just discovered this on bot start. Either way: skip.
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
        {
            ExtraText = "no previous candle for fresh-break check";
            GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText}");
            return false;
        }
        if (IsBreakConfirmed(prev.Candle.Close, pivotValue))
        {
            ExtraText = $"pullback {ExpectedPivotType} {pivotValue:N8} already broken in previous candle";
            GlobalData.AddTextToLogTab($"{tag} REJECT: {ExtraText} (prevClose={prev.Candle.Close:N8})");
            return false;
        }

        ExtraText = $"TrendHtf {SignalSide} break @ {CandleLast.Candle.Close:N8} (pullback {ExpectedPivotType} {pivotValue:N8})";
        GlobalData.AddTextToLogTab($"{tag} FIRE — {ExtraText}");
        ScannerLog.Logger.Debug($"TrendHtf.IsSignal FIRE {Symbol.Name} {Interval.Name} {SignalSide} close={CandleLast.Candle.Close:N8} pullback={pivotValue:N8}");
        return true;
    }


    /// <summary>
    /// Give up when the BOS/CHoCH structure has reverted against us, or when the trader
    /// fails to pick up the signal within GiveUpCandles bars after it fired.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        var settings = GlobalData.Settings.Signal.TrendHtf;
        CryptoTrendIndicator opposite = RequiredTrend == CryptoTrendIndicator.Bullish
            ? CryptoTrendIndicator.Bearish
            : CryptoTrendIndicator.Bullish;

        if (SymbolInterval.TrendBos.Trend == opposite)
        {
            ExtraText = $"TrendBos reverted to {opposite}";
            return true;
        }

        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + settings.GiveUpCandles * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = $"give up after {settings.GiveUpCandles} candles";
            return true;
        }

        return false;
    }
}
#endif