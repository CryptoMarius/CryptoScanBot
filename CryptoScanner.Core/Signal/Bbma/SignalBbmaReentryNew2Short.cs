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
    CSM(Candlestick Momentum): De prijs breekt met kracht door de buitenste BB, wat
             een sterke trendbevestiging is.
    Re-entry(na CSM) : Na een momentum - uitbraak keert de prijs vaak terug naar de MA5/10 voor een tweede instapkans.
*/

#if DEBUG
public class SignalBbmaReentryNew2Short : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;

    public override bool IsSignal()
    {
        // Checklist google
        // file:///D:/Shares/Marius/Documents/Crypto/BbMa/Grok/Poging%201/Google%20-%20Fact%20sheet.htm
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        MyData? candleTf1 = CandleLast;

        // TF1 must currently be in Reentry state — this is the entry moment
        BbmaState state1Now = BbmaStateLong(candleTf1);
        //if (state1Now != BbmaState.Reentry)
        //{
        //    ExtraText = $"TF1 not in Reentry ({TfStateCode(state1Now)})";
        //    return false;
        //}
        if (!(state1Now == BbmaState.Extreme || state1Now == BbmaState.MagicExtreme))
        {
            ExtraText = $"TF1 ({Interval.Name}) not in reentry state ({TfStateCode(state1Now)})";
            //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;

        // Walk back through TF1 history to find the preceding alert candle
        // Skip any Reentry candles — the Reentry may have started a few candles ago.
        // Stop at the first non-Reentry candle; that must be the alert candle.
        for (int i = 0; i < MaxWaitCandles; i++)
        {
            if (!GetPrevCandle(candleTf1, out candleTf1))
            {
                ExtraText = $"insufficient TF1 history for lookback ({i} candles checked)";
                return false;
            }


            // Checklist google
            // file:///D:/Shares/Marius/Documents/Crypto/BbMa/Grok/Poging%201/Google%20-%20Fact%20sheet.htm

            //// BB width filter
            //if (!candleTf1.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
            //{
            //    ExtraText = $"bb.width too small {candleTf1.CandleData!.BollingerBandsPercentage:N2}";
            //    return false;
            //}

            // file:///D:/Shares/Marius/Documents/Crypto/BbMa/Grok/Poging%201/Google%20-%20Fact%20sheet.htm


            // --------------------------
            // 3 Lowest timeframe (LTF)
            // 3.2

            // Grok: https://grok.com/share/c2hhcmQtNA_acadb1c2-54a4-4451-9864-c0f40e74c87b
            // Extreme buy (=extreme) of Re-entry op LTF(MA5/ 10 raakt / komt uit Lower BB + bull candle).
            // Prijs herstelt van Lower BB of Mid BB support.
            // Confluence met HTF/ITF codes, bijv.REM/REE/RRE:

            // https://grok.com/share/c2hhcmQtNA_ef9fd129-8c4a-4e87-9073-0678e0ccacf8
            // Re-entry Buy: Prijs maakt een pullback/correctie naar de Lower BB, Mid BB of MA5/10 zone.
            // Prijs herstelt en sluit terug boven de MA5/10 of Mid BB.
            // Re-entry vindt plaats in de "Zone of Fire" (gebied rond MA5/10 + Mid BB).

            BbmaState state1 = BbmaStateLong(candleTf1);
            // Still in Reentry — keep walking back to find the alert that preceded it
            if (state1 == BbmaState.Reentry)
                continue;

            //if (!(state1 == BbmaState.Extreme || state1 == BbmaState.MagicExtreme))
            //{
            //    ExtraText = $"TF1 ({Interval.Name}) not in reentry state ({TfStateCode(state1)})";
            //    //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}

            //// 3.1 Is there a CSM Buy? (Candle closes above bb.upper)
            //if (!CheckCsmLong(Interval, candleTf1))
            //{
            //    ExtraText = "No CSM present on TF1";
            //    //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}


            // --------------------------
            // 2 Middle timeframe (MTF)
            var result2 = IndicatorDataList.CalculateIndicatorsForInterval(
                Symbol, Interval, candleTf1.Candle.OpenTime, period2);
            if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
            {
                ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name})";
                GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
                return false;
            }


            //// 2.1 Is er een MHV Buy? (Prijs kan niet meer onder de Lower BB sluiten).
            //if (DetectMlv(result2.higherInterval.Interval, candleTf1) != BbmaState.ValidMLV)
            //{
            //    ExtraText = "No MLV/MHV present on TF2";
            //    //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}

            // Grok: https://grok.com/share/c2hhcmQtNA_acadb1c2-54a4-4451-9864-c0f40e74c87b
            // Extreme (E): MA5/10 komt uit Lower BB + candle reversal (sluit weer in BB of MA5/10).
            // OF MHV / MLV (Market Has/Low Volume) na extreme.

            // 2.2 Is er een Extreme Buy zichtbaar? (MA 5 Low steekt buiten de Lower BB).
            BbmaState state2 = BbmaStateShort(result2.candle);
            if (state2 != BbmaState.Extreme)
            {
                ExtraText = $"TF2 ({result2.higherInterval.Interval.Name}) not an extreme ({TfStateCode(state2)})";
                //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
                return false;
            }


            // 2.3 Sluit de prijs onder de Mid BB? (Bevestiging van kracht).
            //if (result2.candle.Candle.Close > (decimal)result2.candle.CandleData.Sma20!.Value)
            //{
            //    ExtraText = $"TF2 ({result2.higherInterval.Interval.Name}) not above sma20 ({TfStateCode(state2)})";
            //    //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}


            // --------------------------
            // 1 Highest timeframe (HTF)
            var result3 = IndicatorDataList.CalculateIndicatorsForInterval(
                Symbol, Interval, candleTf1.Candle.OpenTime, period3);
            if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
            {
                ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name})";
                GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
                return false;
            }

            //// 1.1 Zit de prijs boven de EMA 50? (Trendfilter)
            //// Trend filter on TF3: EMA50 above mid-BB (SMA20) = bearish bias
            //double ema50Tf3 = result3.candle.CandleData!.Ema50!.Value;
            //double midBbTf3 = result3.candle.CandleData!.Sma20!.Value;
            //if (ema50Tf3 <= result3.candle!.CandleData!.Sma20!.Value || midBbTf3 <= result3.candle!.CandleData!.Sma20!.Value)
            //{
            //    ExtraText = $"TF3 EMA50 ({ema50Tf3:N6}) not above mid-BB — bullish bias on HTF, no Short";
            //    GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}

            // 1.2 Is er een Re-entry Buy zone? (Prijs raakt de MA 5/10 LOW aan).
            BbmaState state3 = BbmaStateShort(result3.candle, allowWickDetection: false);
            if (state3 != BbmaState.Reentry)
            {
                ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not in Reentry state ({TfStateCode(state3)}{TfStateCode(state2)}{TfStateCode(state1)})";
                GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
                return false;
            }

            // 1.3 Is de Mid BB stijgend of vlak? (Niet scherp omlaag).
            // This might be a problem codewise?
            //if (!GetPrevCandle(result3.higherInterval.Interval, result3.candle, out MyData? prevCandle))
            //{
            //    ExtraText = $"Error TF3 get prevcandle";
            //    GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}
            //if (midBbTf3 <= prevCandle!.CandleData!.Sma20!.Value)
            //{
            //    ExtraText = $"Error TF3 going up ({TfStateCode(state3)}{TfStateCode(state2)}{TfStateCode(state1)})";
            //    GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}

            //if (!CheckCsmShort(result3.higherInterval.Interval, result3.candle))
            //{
            //    ExtraText = "No CSM present on TF3";
            //    //GlobalData.AddTextToLogTab($"BBMA2 {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            //    return false;
            //}


            // --------------------------
            // Check...
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
        }

        ExtraText = $"no valid alert found within {MaxWaitCandles} candle lookback";
        return false;
    }
}
#endif
