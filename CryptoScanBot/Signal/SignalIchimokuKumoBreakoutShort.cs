using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Skender.Stock.Indicators;

namespace CryptoScanBot.Signal;

#if EXTRASTRATEGIES

public class IchimokuKumoBreakoutShort: SignalCreateBase
{
    public IchimokuKumoBreakoutShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.IchimokuKumoBreakout;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
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


        // De breedte van de bb is ten minste X%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // 52!
        // De 1m candle is nu definitief, doe een herberekening van de relevante intervallen
        List<CryptoCandle> quotes = CandleIndicatorData.CalculateCandles(Symbol, SymbolInterval.Interval, CandleLast.OpenTime, out string _);
        if (quotes == null)
        {
            //GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
            //symbolInterval.Signal = null;
            return false;
        }


        // Geen idee of dit werkt ...
        int tenkanPeriods = 6;
        int kijunPeriods = 26;
        int senkouBPeriods = 52;
        IEnumerable<IchimokuResult> results = quotes.GetIchimoku(tenkanPeriods, kijunPeriods, senkouBPeriods);
        if (results == null || !results.Any())
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
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


        // 1: De voorlaatste candle moet onder de bovenste cloud lijn zitten
        IchimokuResult last = results.Last();
        if (candlePrev.Close < last.SenkouSpanA)
            return false;
        if (candlePrev.Close < last.SenkouSpanB)
            return false;

        // 1: De laatste candle moet boven de bovenste cloud lijn zitten
        if (CandleLast.Close >= last.SenkouSpanA)
            return false;
        if (CandleLast.Close >= last.SenkouSpanB)
            return false;

        // 2: Price sluit boven de Kijun Sen
        if (CandleLast.Close >= last.KijunSen)
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