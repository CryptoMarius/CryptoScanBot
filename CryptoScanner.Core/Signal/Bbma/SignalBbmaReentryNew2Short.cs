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

#if DEBUG
public class SignalBbmaReentryNew2Short : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;

    /// <summary>
    /// Exacte check op HTF voor Short Re-entry na CSM (Oma Ally BBMA)
    /// Gebruikt uitsluitend de reeds berekende data in candle.CandleData
    /// </summary>
    private bool CheckHtf(CryptoInterval interval, MyData current)
    {
        //decimal sma20 = (decimal)current.CandleData.Sma20!.Value;
        decimal wma5High = (decimal)current.CandleData.Wma05High!.Value;
        decimal wma10High = (decimal)current.CandleData.Wma10High!.Value;

        //// Not a ranging chart
        //if (wma10High > sma20)
        //{
        //    ExtraText = $"HTF Wma10Low not below mid-BB - ranging";
        //    GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {interval.Name} {SignalSide} {ExtraText}");
        //    return false;
        //}

        // TODO: Not sure if this the right way (i see different approaches)
        // Reentry after csm, wick should pierce through one of the wma's
        if (!(current.Candle.High > wma5High || current.Candle.High > wma10High))
            return false;


        // Did we have a CSM x candles back
        bool hadCsm = false;
        MyData? prev = current;
        for (int i = 0; i < 30 && i >= 0; i++)
        {
            if (!GetPrevCandle(interval, prev, out prev))
                return false;

            decimal bbLower = (decimal)prev!.CandleData.BollingerBandsLowerBand!.Value;
            if (prev.Candle.Close < bbLower)
            {
                hadCsm = true;
                break;
            }
        }
        if (!hadCsm)
            return false;

        return true;
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
        BbmaState stateLtfNow = BbmaStateShort(candleLtf);
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


            stateLtf = BbmaStateShort(candleLtf!);

            // Not the band-crossing moment yet (e.g. None) — keep walking back
            if (stateLtf == BbmaState.Extreme || stateLtf == BbmaState.MagicExtreme
                || stateLtf == BbmaState.Csm || stateLtf == BbmaState.Mlv)
                break;
        }
        string code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);

        if (!(stateLtf == BbmaState.Extreme || stateLtf == BbmaState.MagicExtreme ||
            stateLtf == BbmaState.Csm || stateLtf == BbmaState.Mlv))
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
        stateMtf = BbmaStateShort(resultMtf.candle);
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
        stateHtf = BbmaStateShort(resultHtf.candle); // just to show something
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
        // Trend filter on TF3: EMA50 above mid-BB (SMA20) = bearish bias
        double wma05HighHtf = resultHtf.candle.CandleData!.Wma05High!.Value;
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        if (ema50Htf <= midBbHtf || wma05HighHtf >= midBbHtf)
        {
            ExtraText = $"HTF EMA50 not above mid-BB — bullish bias, no Short";
            GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {resultHtf.higherInterval.Interval.Name} {SignalSide} {code} {ExtraText}");
            return false;
        }


        stateHtf = BbmaState.Reentry; // Assume..
        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle))
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
#endif
