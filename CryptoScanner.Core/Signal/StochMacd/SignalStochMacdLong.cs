using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

#if DEBUG
namespace CryptoScanner.Core.Signal.StochMacd;

/// <summary>
/// Long variant of the Stoch + MACD crossover strategy.
///
/// IsSignal — opens the entry window (cheap checks first, expensive last):
///   1. Bollinger Bands width within the configured range.
///   2. Stochastic oversold.
///   3. Trend filter : TrendPrimary + TrendSecondary on the active interval must be Bullish.
///
/// AllowStepIn — the actual entry trigger:
///   4. MACD histogram cross UP through zero (equivalent to MacdValue crossing above MacdSignal —
///      histogram = MacdValue - MacdSignal, so a zero-cross of one ⇔ a line-cross of the other).
///
/// The strategy fires IsSignal once on the BB+Stoch+Trend candle, then patiently waits for
/// the MACD cross to materialize on a subsequent bar. Default GiveUp (Settings.Trading.EntryRemoveTime)
/// invalidates the signal when no cross appears in time.
///
/// On a hit the proposed SL (most recent swing low) and TP (entry + RRR × risk, fee-adjusted) are
/// computed at IsSignal time and exposed via <see cref="SignalCreateBase.OverrideSlPrice"/>
/// / <see cref="SignalCreateBase.OverrideTpPrice"/>.
/// </summary>
public class SignalStochMacdLong : SignalStochMacdBase
{
    private decimal? _proposedSl;
    private decimal? _proposedTp;

    public override decimal? OverrideSlPrice => _proposedSl;
    public override decimal? OverrideTpPrice => _proposedTp;

    public override bool GiveUp(CryptoSignal signal)
    {
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + 30 * Interval.Duration < CandleLast?.Candle.OpenTime.Minutes)
        {
            ExtraText = $"Stop after {GlobalData.Settings.Trading.EntryRemoveTime} candles";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Actual entry trigger: MACD histogram crosses up through zero on the current candle.
    /// IsSignal only opened the window (BB + stoch OS + trend); this is where the trade actually
    /// gets the green light. Re-evaluated on every candle while the signal is in TryStepIn state.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (!TryGetMacdHistogram(out double prevH, out double currH))
        {
            ExtraText = "no prev candle for macd cross check";
            return false;
        }
        if (!(prevH <= 0 && currH > 0))
        {
            ExtraText = $"waiting for macd cross up (prev={prevH:N4}, curr={currH:N4})";
            return false;
        }

        ExtraText = $"macd cross up (prev={prevH:N4}, curr={currH:N4})";
        return true;
    }


    public override bool IsSignal()
    {
        _proposedSl = null;
        _proposedTp = null;
        ExtraText = "";

        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // 2. Stoch oversold
        if (!CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        var settings = GlobalData.Settings.Signal.StochMacd;

        // ********************************************************************
        // 3. Trend filter — TrendPrimary (Dow-theory ZigZag) on the active interval must be Bullish.
        //    Replaces the older "close > SMA200" check, aligning this strategy with the rest of the
        //    trend infrastructure (same source as SignalTrend / SignalTrendHtf).
        // Dont trade against the trend (only check current interval)
        if (settings.RequireTrendFilter && !CheckTrendPrimary())
            return false;
        if (settings.RequireTrendFilter && !CheckTrendSecondary())
            return false;


        // Compute proposed SL/TP. When a valid swing-low is found these are exposed via
        // OverrideSlPrice / OverrideTpPrice so the trader can pick them up.
        // The MACD-cross trigger that *fires* the actual entry happens in AllowStepIn —
        // this method only opens the window.
        decimal close = CandleLast.Candle.Close;
        if (TryFindSwingLow(settings.SwingLookback, settings.SwingPivotBars, out decimal swingLow)
            && swingLow < close)
        {
            decimal risk = close - swingLow;
            decimal rawTp = close + settings.RiskRewardRatio * risk;
            decimal tp = AdjustTpForFees(close, rawTp);
            _proposedSl = swingLow;
            _proposedTp = tp;
            string feeNote = settings.IncludeFeesInTp && Symbol.Exchange.FeeRate > 0
                ? $", fee {Symbol.Exchange.FeeRate:N3}% → rawTp={rawTp:N6}"
                : "";
            ExtraText = $"window open @ {close} | sl={swingLow:N6} (risk {risk:N6}) | tp={tp:N6} (rrr={settings.RiskRewardRatio}{feeNote}) — waiting for macd cross up";
        }
        else
        {
            ExtraText = $"window open @ {close} | no valid swing-low in last {settings.SwingLookback} bars — waiting for macd cross up";
        }
        return true;
    }


}
#endif
