using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

#if DEBUG
namespace CryptoScanner.Core.Signal.StochMacd;

/// <summary>
/// Short variant — mirror of <see cref="SignalStochMacdLong"/>.
///
/// IsSignal opens the entry window (BB width, Stoch OB, Trend bearish); AllowStepIn waits
/// for the MACD histogram to cross DOWN through zero on a subsequent candle.
/// </summary>
public class SignalStochMacdShort : SignalStochMacdBase
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
    /// Actual entry trigger: MACD histogram crosses down through zero on the current candle.
    /// IsSignal only opened the window (BB + stoch OB + trend); this is where the trade actually
    /// gets the green light. Re-evaluated on every candle while the signal is in TryStepIn state.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (!TryGetMacdHistogram(out double prevH, out double currH))
        {
            ExtraText = "no prev candle for macd cross check";
            return false;
        }
        if (!(prevH >= 0 && currH < 0))
        {
            ExtraText = $"waiting for macd cross down (prev={prevH:N4}, curr={currH:N4})";
            return false;
        }

        ExtraText = $"macd cross down (prev={prevH:N4}, curr={currH:N4})";
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

        // 2. Stoch overbought
        if (!CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }


        var settings = GlobalData.Settings.Signal.StochMacd;

        // ********************************************************************
        // 1. Trend filter — TrendPrimary (Dow-theory ZigZag) on the active interval must be Bearish.
        //    Replaces the older "close < SMA200" check, aligning this strategy with the rest of the
        //    trend infrastructure (same source as SignalTrend / SignalTrendHtf).
        // Dont trade against the trend (only check current interval)
        if (settings.RequireTrendFilter && !CheckTrendPrimary())
            return false;
        if (settings.RequireTrendFilter && !CheckTrendSecondary())
            return false;


        // The MACD-cross trigger that *fires* the actual entry happens in AllowStepIn —
        // this method only opens the window.


        // Compute proposed SL (swing high above entry) and TP, exposed via the override hooks.
        decimal close = CandleLast.Candle.Close;
        if (TryFindSwingHigh(settings.SwingLookback, settings.SwingPivotBars, out decimal swingHigh) && swingHigh > close)
        {
            decimal risk = swingHigh - close;
            decimal rawTp = close - settings.RiskRewardRatio * risk;
            decimal tp = AdjustTpForFees(close, rawTp);
            _proposedSl = swingHigh;
            _proposedTp = tp;
            string feeNote = settings.IncludeFeesInTp && Symbol.Exchange.FeeRate > 0
                ? $", fee {Symbol.Exchange.FeeRate:N3}% → rawTp={rawTp:N6}"
                : "";
            ExtraText = $"window open @ {close} | sl={swingHigh:N6} (risk {risk:N6}) | tp={tp:N6} (rrr={settings.RiskRewardRatio}{feeNote}) — waiting for macd cross down";
        }
        else
        {
            ExtraText = $"window open @ {close} | no valid swing-high in last {settings.SwingLookback} bars — waiting for macd cross down";
        }
        return true;
    }


}
#endif
