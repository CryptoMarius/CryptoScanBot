using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

/* https://share.google/aimode/cb5CF0MrCDCKw2JCS

Het Beslissingsschema (De Cyclus)

De cyclus beweegt zich van een oververhitte markt(Extreme) naar een nieuwe trend(Momentum)
en biedt daartussen verschillende instapmomenten.

    Extreme: Een Moving Average(MA5 / 10) steekt buiten de Bollinger Bands(BB) uit.
             Dit is het eerste signaal van uitputting.
    TPW(Take Profit Wajib): De prijs keert terug naar de MA5 / 10 of Mid BB.
             Winstneming is hier verplicht ("Wajib").
    MHV(Market Has No Volume) : De prijs probeert de trend te hervatten maar slaagt er
             niet in de buitenste BB te doorbreken.Dit toont zwakte aan.
    CSD / CSAK(Candlestick Direction / Arah Kukuh) : Een "sterke" kaars die de Mid BB en MA5 / 10
             doorbreekt, wat de nieuwe richting bevestigt.
    Re-entry(na CSD) : De prijs trekt tijdelijk terug naar de MA5/10 zone voor een veilige instap in de nieuwe trend.
    CSM(Candlestick Momentum): De prijs breekt met kracht door de buitenste BB, wat een sterke trendbevestiging is.
    Re-entry(na CSM) : Na een momentum - uitbraak keert de prijs vaak terug naar de MA5/10 voor een tweede instapkans.
*/

public class SignalBbmaReentryNew2Long : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;

    /// <summary>
    /// Exacte check op HTF voor Long Re-entry na CSM (Oma Ally BBMA)
    /// Gebruikt uitsluitend de reeds berekende data in candle.CandleData
    /// </summary>
    /// <param name="interval">The HTF interval (used for walking back through candles)</param>
    /// <param name="current">The HTF candle (provides indicator levels: WMA5Low, WMA10Low)</param>
    /// <param name="ltfCandle">The current LTF candle (provides real-time price for the wick check)</param>
    private bool CheckHtf(CryptoInterval interval, MyData current, MyData ltfCandle)
    {
        decimal ema50 = (decimal)current.CandleData.Ema50!.Value;
        decimal sma20 = (decimal)current.CandleData.Sma20!.Value;
        decimal wma5Low = (decimal)current.CandleData.Wma05Low!.Value;
        decimal wma10Low = (decimal)current.CandleData.Wma10Low!.Value;

        // BB is expanding, not a ranging chart https://youtu.be/tOQb6RRhbLA?t=102
        if (wma10Low < sma20 || wma10Low < ema50)
        {
            ExtraText = $"HTF Wma10Low not above mid-BB - ranging?";
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Use LTF candle price against HTF MA levels: more real-time than checking the HTF candle's own wick,
        // which can be hours stale on higher intervals (e.g. 4h/1d).
        // Reentry after csm, wick should pierce through one of the wma's
        if (!(ltfCandle.Candle.Close < wma5Low || ltfCandle.Candle.Close < wma10Low)) // Was low, replaced with Close
            return false;


        // Did we have a CSM x candles back
        bool hadCsm = false;
        MyData? prev = current;
        for (int i = 0; i < 30 && i >= 0; i++)
        {
            if (!GetPrevCandle(interval, prev, out prev))
                return false;

            decimal bbUpper = (decimal)prev!.CandleData.BollingerBandsUpperBand!.Value;
            if (prev.Candle.Close > bbUpper)
            {
                hadCsm = true;
                break;
            }
        }
        if (!hadCsm)
            return false;

        return true;
    }


    /// <summary>
    /// Entry timing filter: checks the 5m chart for WMA05High crossing below WMA10High.
    /// This indicates the pullback has just entered the MA zone on the 5m — the optimal Long re-entry moment.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        CryptoInterval interval5m = Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m).Interval;

        // Ensure 5m indicator data is available in IndicatorDataList
        if (!IndicatorDataList.PrepareIndicators(Symbol, interval5m, CandleLast.Candle.OpenTime))
            return false;

        // Align current time down to the nearest 5m boundary
        CandleTime time5m = CandleLast.Candle.OpenTime - (CandleLast.Candle.OpenTime % interval5m.Duration);

        if (!IndicatorDataList.TryGetCandle(interval5m, time5m, out MyData? current5m) || current5m == null)
            return false;

        if (!GetPrevCandle(interval5m, current5m, out MyData? prev5m) || prev5m == null)
            return false;

        if (current5m.CandleData.Wma05High == null || current5m.CandleData.Wma10High == null ||
            prev5m.CandleData.Wma05High == null || prev5m.CandleData.Wma10High == null)
            return false;

        decimal wma05HighNow  = (decimal)current5m.CandleData.Wma05High.Value;
        decimal wma10HighNow  = (decimal)current5m.CandleData.Wma10High.Value;
        decimal wma05HighPrev = (decimal)prev5m.CandleData.Wma05High.Value;
        decimal wma10HighPrev = (decimal)prev5m.CandleData.Wma10High.Value;

        // Crossover on 5m: WMA05High just dropped below WMA10High
        return wma05HighNow < wma10HighNow && wma05HighPrev >= wma10HighPrev;
    }


    /// <summary>
    /// Invalidates the setup when the current candle is a Short Extreme.
    /// A bearish extreme after a bullish CSM means the setup has been overridden — give up.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        BbmaState state = BbmaStateShort(CandleLast);
        return state == BbmaState.Extreme || state == BbmaState.MagicExtreme;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        MyData? candleLtf = CandleLast;

        // LTF must be in Reentry state - entry
        BbmaState stateLtfNow = BbmaStateLong(candleLtf);
        if (stateLtfNow != BbmaState.Reentry)
        {
            ExtraText = $"LTF not in Reentry ({TfStateCode(stateLtfNow)})";
            return false;
        }

        // Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
            return false;

        // Walk back through LTF candles to find the preceding extreme (or otherwise)
        BbmaState stateLtf = BbmaState.Reentry;
        BbmaState stateMtf = BbmaState.None;
        BbmaState stateHtf = BbmaState.None;

        for (int i = 0; i < MaxWaitCandles; i++)
        {
            if (!GetPrevCandle(candleLtf, out candleLtf))
            {
                ExtraText = $"insufficient LTF history for lookback ({i} candles checked)";
                return false;
            }


            stateLtf = BbmaStateLong(candleLtf!);

            // Not the band-crossing moment yet (e.g. None) — keep walking back
            if (stateLtf == BbmaState.Extreme || stateLtf == BbmaState.MagicExtreme
                || stateLtf == BbmaState.Mlv) //|| stateLtf == BbmaState.Csm 
                break;
        }
        string code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);

        if (!(stateLtf == BbmaState.Extreme || stateLtf == BbmaState.MagicExtreme 
            || stateLtf == BbmaState.Mlv)) //stateLtf == BbmaState.Csm
        {
            ExtraText = $"LTF unexpected state";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }


        // --------------------------
        // Middle timeframe (MTF)
        var resultMtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, candleLtf!.Candle.OpenTime, mtf);
        if (!resultMtf.success || resultMtf.candle == null || !IndicatorsOkay(resultMtf.candle))
        {
            ExtraText = $"no data for MTF ({resultMtf.higherInterval.Interval.Name})";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }
        stateMtf = BbmaStateLong(resultMtf.candle);
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);



        // --------------------------
        // Highest timeframe (HTF)
        var resultHtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, candleLtf.Candle.OpenTime, htf);
        if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
        {
            ExtraText = $"no data for HTF";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }
        stateHtf = BbmaStateLong(resultHtf.candle); // just to show something
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);


        // Extreme on the MTF?
        if (stateMtf != BbmaState.Extreme)
        {
            ExtraText = $"MTF not an extreme";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {resultMtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }



        // --------------------------
        // Highest timeframe (HTF)

        // Zit de prijs boven de EMA 50? (Trendfilter)
        // Trend filter on TF3 EMA50 below mid-BB (SMA20) = bullish bias
        double wma05LowHtf = resultHtf.candle.CandleData!.Wma05Low!.Value;
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        if (ema50Htf >= midBbHtf || wma05LowHtf >= midBbHtf)
        {
            ExtraText = $"HTF ema50 not below mid-BB - bearish bias";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }


        stateHtf = BbmaState.Reentry; // Assume..
        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle, CandleLast))
        {
            ExtraText = $"HTF not in reentry state";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }




        // --------------------------
        // Check...
        // MTF code: TF3→TF2→TF1 (highest to lowest).
        // Because TF1 is always R (entry condition) and TF3 is always R (HTF anchor),
        // the entry-phase codes are the PDF alert codes with TF1 replaced by R:
        //   PDF alert RRE  → entry code RRR  (TF2=Reentry)
        //   PDF alert REM  → entry code RER  (TF2=Extreme, from M alert)
        //   PDF alert REE  → entry code RER  (TF2=Extreme, from E alert)
        //   PDF alert RMEE → entry code RMR  (TF2=MLV, from MagicExtreme alert)
        code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);
        if (code == "RRE" || code == "REM" || code == "REE" || code == "RMEE")
        {
            ExtraText = $"{code} {resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name}";
            return true;
        }

        ExtraText = $"code {code} not valid ({resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name})";
        return false;
    }
}
