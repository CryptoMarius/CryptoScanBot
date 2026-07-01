using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG

public class SignalIchimokuKumoBreakoutShort : SignalCreateBase
{

    public override bool IndicatorsOkay(MyData candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Rsi == null)
           //|| (candle.CandleData.Ema20 == null)
           )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";


        // BB width filter: only enforce minimum; skip the Stobb maximum (5%) because Kumo Breakout
        // is a momentum strategy that fires after a breakout — typically at higher volatility.
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 0))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // 52!
        // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
        List<IQuote>? quotes = IndicatorEngine.CollectCandles(Symbol, SymbolInterval.Interval, CandleLast.Candle.OpenTime, out string _);
        if (quotes == null)
        {
            //GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
            //symbolInterval.Signal = null;
            return false;
        }


        // Standard Ichimoku periods per the strategy definition (tenkan = 9, not 6)
        int tenkanPeriods = 9;
        int kijunPeriods = 26;
        int senkouBPeriods = 52;
        IEnumerable<IchimokuResult> results = quotes.ToIchimoku(tenkanPeriods, kijunPeriods, senkouBPeriods);
        if (results == null || !results.Any())
            return false;

        if (!GetPrevCandle(CandleLast, out MyData? candlePrev))
            return false;

        // https://www.whselfinvest.com/nl-be/trading-platform/gratis-trading-strategie/tradingsysteem/18-ichimoku-kbo
        // De zone tussen de Senkou Span A en Senkou Span B noemt men Kumo(wolk). De wolk is rood als de trend
        // neerwaarts is (Senkou Span A ligt onder de Senkou Span B). De wolk is groen als de trend opwaarts
        // is (Senkou Span A ligt boven de Senkou Span B).

        // van telegram:
        //Sure! Dit zijn een aantal factoren die  als confirmatie bijvoorbeeld bij een Bullish Kumo Breakout gebruikt worden:
        //1) Breekt uit en sluit boven de Kumo
        //2) Price sluit boven de Kijun Sen
        //3) Een positieve(groene) Kumo future
        //4) Chikou span boven prijs

        // Senkou Span A/B are projected 26 periods forward; the cloud values that align with the
        // current candle sit at index (count - 1 - kijunPeriods), not at results.Last().
        List<IchimokuResult> resultList = results.ToList();
        int cloudIndex = resultList.Count - 1 - kijunPeriods;
        if (cloudIndex < 0)
            return false;
        IchimokuResult cloud = resultList[cloudIndex];
        if (cloud.SenkouSpanA == null || cloud.SenkouSpanB == null || cloud.KijunSen == null)
            return false;

        // Bottom of cloud = min(SenkouSpanA, SenkouSpanB) — works for both green and red clouds
        decimal cloudBottom = Math.Min((decimal)cloud.SenkouSpanA, (decimal)cloud.SenkouSpanB);

        // 1: Previous candle must be above the cloud bottom (not yet broken out downward)
        if (candlePrev!.Candle.Close < cloudBottom)
            return false;

        // 1: Current candle must close below the cloud bottom (the breakout)
        if (CandleLast.Candle.Close >= cloudBottom)
            return false;

        // 2: Price closes below Kijun Sen
        if (CandleLast.Candle.Close >= (decimal)cloud.KijunSen)
            return false;



        //if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out CryptoCandle prevCandle))
        //{
        //    ExtraText = "geen prev candle! " + CandleLast.DateLocal.ToString();
        //    return false;
        //}

        //// De vorige candle mag de ema niet gekruist hebben
        //if ((prevCandle.Open > (decimal)prevCandle.CandleData.Ema20) || (prevCandle.Close > (decimal)CandleLast.CandleData.Ema20))
        //    return false;

        //// De laatste candle moet de ema opwaarts kruisen
        //if ((CandleLast.Open > (decimal)CandleLast.CandleData.Ema20) || (CandleLast.Close < (decimal)CandleLast.CandleData.Ema20))
        //    return false;

        // voorlopig even false!
        return true;
    }

}

#endif