using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Reentry Short entry signal (Oma Ally method).
///
/// Fires when the actual ENTRY condition is met on TF1, confirmed by the multi-timeframe
/// structure. Per the PDF (chapter 6): after a CSD on TF1, wait for price to correct back
/// into the 510 sell zone — THAT is the trade entry.
///
/// Entry condition (TF1=Reentry):
///   - Bearish CSD is active: LWMA5(high) &lt; LWMA10(high)
///   - Price has pulled back to the 510 sell zone: close ≥ LWMA5(high)
///     OR wick retest: high above LWMA5, close below LWMA10
///
/// MTF structure required:
///   TF3 = Reentry (HTF directional anchor — already in reentry after its own CSD)
///   TF2 = any meaningful state (Reentry, Extreme, M, MagicExtreme — confirms setup active on mid TF)
///   TF1 = Reentry (entry condition — the actual trade entry per PDF)
///
/// Fixed BBMA timeframe pairs:
///   5m→15m→1h,  15m→1h→4h,  1h→4h→1d,  4h→1d→1w
/// </summary>
public class SignalBbmaReentryNewShort : SignalBbmaBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Ema50 == null
           || data.CandleData.Wma05High == null
           || data.CandleData.Wma10High == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    private static string TfStateCode(BbmaTfState state) => state switch
    {
        BbmaTfState.MagicExtreme => "EE",
        BbmaTfState.Extreme  => "E",
        BbmaTfState.M  => "M",
        BbmaTfState.Reentry  => "R",
        _              => "-"
    };


    /// <summary>
    /// Classifies the BBMA state of a candle for Short setups (uses LWMA5/10 on highs).
    /// Priority: MagicExtreme → Extreme(TypeA) → Extreme(TypeB) → Extreme(Advance) → Reentry → M → None
    ///
    /// allowWickDetection: disable for TF2/TF3 because their candles are still forming —
    /// wick levels are not yet final, but MA positions are reliable.
    /// </summary>
    private BbmaTfState ClassifyStateShort(MyData data, bool allowWickDetection = true)
    {
        double wma5High  = data.CandleData!.Wma05High!.Value;
        double wma10High = data.CandleData!.Wma10High!.Value;
        double bbUpper   = data.CandleData!.BollingerBandsUpperBand!.Value;


        // MagicExtreme (Magic Extreme): both MAs above BB.Upper
        if (wma5High > bbUpper && wma10High > bbUpper)
            return BbmaTfState.MagicExtreme;

        // Extreme (Type A): LWMA5(high) above BB.Upper
        if (wma5High > bbUpper)
            return BbmaTfState.Extreme;


        decimal high = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal open = data.Candle.Open;

        if (allowWickDetection)
        {
            decimal bbUpperDec = (decimal)bbUpper;

            // Extreme (Type B): wick rejection of BB.Upper
            if (high > bbUpperDec && close < bbUpperDec && open < bbUpperDec)
                return BbmaTfState.Extreme;

            // Extreme (Advance): wick rejection of EMA50
            double ema50 = data.CandleData!.Ema50!.Value;
            decimal ema50Dec = (decimal)ema50!.Value;
            if (high > ema50Dec && close < ema50Dec && open < ema50Dec)
                return BbmaTfState.Extreme;
        }

        // Reentry (Reentry): bearish CSD active + price reached the 510 sell zone
        //   Standard : close at or above LWMA5(high) — in or beyond the zone
        //   MA Retest: wick spiked above LWMA5(high), close recovered below LWMA10(high)
        if (wma5High < wma10High)
        {
            decimal wma5Dec  = (decimal)wma5High;
            decimal wma10Dec = (decimal)wma10High;
            bool priceInZone = close >= wma5Dec;
            bool maRetest    = allowWickDetection && high > wma5Dec && close < wma10Dec;
            if (priceInZone || maRetest)
                return BbmaTfState.Reentry;
        }

        // M (MHV phase): LWMA5(high) below BB.Upper but above LWMA10(high) — pre-CSD
        if (wma5High <= bbUpper && wma5High > wma10High)
            return BbmaTfState.M;

        return BbmaTfState.None;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // BB width filter
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Step 1: TF1 must be in Reentry state — this IS the entry condition per PDF chapter 6:
        // CSD has occurred on TF1 and price has pulled back into the 510 sell zone.
        BbmaTfState state1 = ClassifyStateShort(CandleLast);
        if (state1 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF1 ({Interval.Name}) not in Reentry state ({TfStateCode(state1)})";
            return false;
        }

        // EMA50 trend filter: EMA50 must be above mid-BB (SMA20) for bearish bias
        // Per PDF: EMA50 above mid-BB = downtrend
        double ema50Tf1 = CandleLast.CandleData!.Ema50!.Value;
        double midBbTf1 = CandleLast.CandleData!.Sma20!.Value;
        if (ema50Tf1 <= midBbTf1)
        {
            ExtraText = $"EMA50 ({ema50Tf1:N6}) not above mid-BB — bullish bias, no Short";
            return false;
        }


        // Step 2: Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;

        
        // Step 3: TF2 state (wick detection disabled — candle still forming on higher TF)
        var result2 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period2);

        if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
        {
            ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name})";
            return false;
        }

        BbmaTfState state2 = ClassifyStateShort(result2.candle, allowWickDetection: false);
        if (state2 == BbmaTfState.None)
        {
            ExtraText = $"TF2 ({result2.higherInterval.Interval.Name}) has no clear BBMA state";
            return false;
        }

        
        // Step 4: TF3 must be in Reentry state — HTF directional anchor
        // Per PDF: the highest timeframe shows a completed reentry (CSD + price in zone)
        var result3 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period3);

        if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
        {
            ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name})";
            return false;
        }

        BbmaTfState state3 = ClassifyStateShort(result3.candle, allowWickDetection: false);
        if (state3 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not in R state (is {TfStateCode(state3)})";
            return false;
        }

        // MTF code: TF3→TF2→TF1 (highest to lowest)
        // Valid entry codes derived directly from the PDF MTF table (chapter 7):
        //   RRR — TF2=Reentry (entry counterpart of PDF alert code RRE)
        //   RER — TF2=Extreme (entry counterpart of PDF alert codes REM and REE)
        //   RMR — TF2=MLV    (entry counterpart of PDF alert code RMEE)
        // TF1 is always R (the actual entry condition), TF3 is always R (HTF anchor).
        string code = TfStateCode(state3) + TfStateCode(state2) + TfStateCode(state1);

        if (code == "REM" || code == "RRE" || code == "REE" || code == "RMEE")
        {
            ExtraText = $"{code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
            return true;
        }

        ExtraText = $"invalid MTF code {code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
        return false;
    }
}
#endif
