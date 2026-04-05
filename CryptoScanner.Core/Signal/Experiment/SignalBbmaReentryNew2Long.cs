using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Reentry Long entry signal (Oma Ally method).
///
/// Fires when the actual ENTRY condition is met on TF1, confirmed by the multi-timeframe
/// structure. Per the PDF (chapter 6): after a CSD on TF1, wait for price to correct back
/// into the 510 buy zone — THAT is the trade entry.
///
/// Entry condition (TF1=Reentry):
///   - Bullish CSD is active: LWMA5(low) > LWMA10(low)
///   - Price has pulled back to the 510 buy zone: close ≤ LWMA5(low)
///     OR wick retest: low below LWMA5, close above LWMA10
///
/// MTF structure required:
///   TF3 = Reentry (HTF directional anchor — already in reentry after its own CSD)
///   TF2 = any meaningful state (Reentry, Extreme, Mlv, MagicExtreme — confirms setup active on mid TF)
///   TF1 = Reentry (entry condition — the actual trade entry per PDF)
///
/// Fixed BBMA timeframe pairs:
///   5m→15m→1h,  15m→1h→4h,  1h→4h→1d,  4h→1d→1w
/// </summary>
public class SignalBbmaReentryNew2Long : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.Ema50 == null
           || data.CandleData.Wma05Low == null
           || data.CandleData.Wma10Low == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.BollingerBandsPercentage == null
           )
            return false;

        return true;
    }


    /// <summary>
    /// Classifies the BBMA state of a candle for Long setups (uses LWMA5/10 on lows).
    /// Priority: MagicExtreme → Extreme(TypeA) → Extreme(TypeB) → Extreme(Advance) → Reentry → Mlv → None
    ///
    /// allowWickDetection: disable for TF2/TF3 because their candles are still forming —
    /// wick levels are not yet final, but MA positions are reliable.
    /// </summary>
    private BbmaTfState ClassifyState(MyData data, bool allowWickDetection = true)
    {
        double wma5Low = data.CandleData!.Wma05Low!.Value;
        double wma10Low = data.CandleData!.Wma10Low!.Value;
        double bbLower = data.CandleData!.BollingerBandsLowerBand!.Value;

        // MagicExtreme (Magic Extreme): both MAs below BB.Lower
        if (wma5Low < bbLower && wma10Low < bbLower)
            return BbmaTfState.MagicExtreme;

        // Extreme (Type A): LWMA5(low) below BB.Lower
        if (wma5Low < bbLower)
            return BbmaTfState.Extreme;


        decimal low = data.Candle.Low;
        decimal close = data.Candle.Close;
        decimal open = data.Candle.Open;

        if (allowWickDetection)
        {
            // Extreme (Type B): wick rejection of BB.Lower
            decimal bbLowerDec = (decimal)bbLower;
            if (low < bbLowerDec && close > bbLowerDec && open > bbLowerDec)
                return BbmaTfState.Extreme;

            // Extreme (Advance): wick rejection of EMA50
            decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
            if (low < ema50 && close > ema50 && open > ema50)
                return BbmaTfState.Extreme;
        }

        // Reentry: bullish CSD active + price reached the 510 buy zone
        //   Standard : close at or below LWMA5(low) — in or beyond the zone
        //   MA Retest: wick dipped below LWMA5(low), close recovered above LWMA10(low)
        if (wma5Low > wma10Low)
        {
            decimal wma5Dec = (decimal)wma5Low;
            decimal wma10Dec = (decimal)wma10Low;
            bool priceInZone = close <= wma5Dec;
            bool maRetest = allowWickDetection && low < wma5Dec && close > wma10Dec;
            if (priceInZone || maRetest)
                return BbmaTfState.Reentry;
        }

        // Mlv (Market Loss Volume): LWMA5(low) above BB.Lower but below LWMA10(low) — pre-CSD
        if (wma5Low >= bbLower && wma5Low < wma10Low)
            return BbmaTfState.Mlv;

        return BbmaTfState.None;
    }


    /// <summary>
    /// Phase 2: Allow entry only once TF1 has reached Reentry state.
    /// Called on every new candle after the alert signal was created.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        BbmaTfState state1 = ClassifyState(CandleLast);
        if (state1 != BbmaTfState.Reentry)
        {
            ExtraText = $"waiting Reentry — TF1 currently {TfStateCode(state1)}";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        ExtraText = "Reentry reached — entry allowed";
        GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
        return true;
    }


    /// <summary>
    /// Phase 3: Abandon the signal when the setup has expired.
    ///   - More than MaxWaitCandles elapsed without a Reentry, or
    ///   - CSD still active (wma5 > wma10) but price closed below SMA20
    ///     (the bullish reversal has definitively failed).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";

        // Too many candles elapsed without a Reentry
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + MaxWaitCandles * Interval.Duration < CandleLast?.Candle.OpenTime.Minutes)
        {
            ExtraText = $"Stop after {GlobalData.Settings.Trading.EntryRemoveTime} candles";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return true;
        }

        // Pattern invalidated: CSD still active but price closed below SMA20
        // — the reversal move has failed and a genuine Reentry will not follow
        double wma5Low = CandleLast!.CandleData!.Wma05Low!.Value;
        double wma10Low = CandleLast.CandleData!.Wma10Low!.Value;
        double sma20 = CandleLast.CandleData!.Sma20!.Value;
        if (wma5Low > wma10Low && (double)CandleLast.Candle.Close < sma20)
        {
            ExtraText = "GiveUp: CSD active but close below SMA20 — bullish reversal failed";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return true;
        }

        return false;
    }


    public override bool IsSignal()
    {
        // Checklist google
        // file:///D:/Shares/Marius/Documents/Crypto/BbMa/Grok/Poging%201/Google%20-%20Fact%20sheet.htm

        ExtraText = "";

        // Find the higher timeframes
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;


        //// BB width filter
        //if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        //{
        //    ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
        //    return false;
        //}

        // file:///D:/Shares/Marius/Documents/Crypto/BbMa/Grok/Poging%201/Google%20-%20Fact%20sheet.htm


        // --------------------------
        // 3 Lager Tijdframe (LTF): De Execute (Entry)
        // 3.2
        BbmaTfState state1 = ClassifyState(CandleLast);
        if (state1 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF1 ({Interval.Name}) not in reentry state ({TfStateCode(state1)})";
            //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // 3.1 Is there a CSM Buy? (Candle closes above bb.upper)
        if (!CheckCsmLong(Interval, CandleLast))
        {
            ExtraText = "No CSM present on TF1";
            //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }


        // --------------------------
        // 2 Middelste Tijdframe (MTF): De Validatie
        var result2 = IndicatorDataList.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, period2);
        if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
        {
            ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name})";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }


        // 2.1 Is er een MHV Buy? (Prijs kan niet meer onder de Lower BB sluiten).
        if (DetectMlv(result2.higherInterval.Interval, CandleLast) != BbmaState.ValidMLV)
        {
            ExtraText = "No MLV/MHV present on TF2";
            //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // 2.2 Is er een Extreme Buy zichtbaar? (MA 5 Low steekt buiten de Lower BB).
        BbmaTfState state2 = ClassifyState(result2.candle);
        //if (state2 != BbmaTfState.Extreme)
        //{
        //    ExtraText = $"TF2 ({result2.higherInterval.Interval.Name}) not an extreme ({TfStateCode(state2)})";
        //    //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
        //    return false;
        //}

        // 2.3 Sluit de prijs boven de Mid BB? (Bevestiging van kracht).
        if (result2.candle.Candle.Close < (decimal)result2.candle.CandleData.Sma20!.Value)
        {
            ExtraText = $"TF2 ({result2.higherInterval.Interval.Name}) not below sma20 ({TfStateCode(state2)})";
            //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }


        // --------------------------
        // 1 Hoger Tijdframe (HTF): De Setup
        var result3 = IndicatorDataList.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, period2);
        if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
        {
            ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name})";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // 1.1 Zit de prijs boven de EMA 50? (Trendfilter)
        // Trend filter on TF3 (highest TF): EMA50 below mid-BB (SMA20) = bullish AddTextToLogTab($"BBMA2 {Symbol.Name}
        // Per PDF: trend direction is determined on the highest timeframe, not on TF1.
        double ema50Tf3 = result3.candle.CandleData!.Ema50!.Value;
        double midBbTf3 = result3.candle.CandleData!.Sma20!.Value;
        if (ema50Tf3 >= midBbTf3)
        {
            ExtraText = $"TF3 EMA50 ({ema50Tf3:N6}) not below mid-BB — bearish on HTF, no Long";
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // 1.2 Is er een Re-entry Buy zone? (Prijs raakt de MA 5/10 LOW aan).
        BbmaTfState state3 = ClassifyState(result3.candle, allowWickDetection: false);
        if (state3 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not in Reentry state ({TfStateCode(state3)}{TfStateCode(state2)}{TfStateCode(state1)})";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // 1.3 Is de Mid BB stijgend of vlak? (Niet scherp omlaag).
        // This might be a problem codewise?
        if (!GetPrevCandle(result3.higherInterval.Interval, result3.candle, out MyData? prevCandle))
        {
            ExtraText = $"Error TF3 get prevcandle";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }
        if (midBbTf3 >= prevCandle!.CandleData!.Sma20!.Value)
        {
            ExtraText = $"Error TF3 going up ({TfStateCode(state3)}{TfStateCode(state2)}{TfStateCode(state1)})";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // MTF code: TF3→TF2→TF1 (highest to lowest).
        // Because TF1 is always R (entry condition) and TF3 is always R (HTF anchor),
        // the entry-phase codes are the PDF alert codes with TF1 replaced by R:
        //   PDF alert RRE  → entry code RRR  (TF2=Reentry)
        //   PDF alert REM  → entry code RER  (TF2=Extreme, from M alert)
        //   PDF alert REE  → entry code RER  (TF2=Extreme, from E alert)
        //   PDF alert RMEE → entry code RMR  (TF2=MLV, from MagicExtreme alert)
        string code = TfStateCode(state3) + TfStateCode(state2) + TfStateCode(state1);
        if (code == "REM" || code == "RRE" || code == "RME" || code == "REE")
        {
            ExtraText = $"{code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
            return true;
        }


        ExtraText = $"invalid MTF code {code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
        GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
        return false;
    }
}
#endif
