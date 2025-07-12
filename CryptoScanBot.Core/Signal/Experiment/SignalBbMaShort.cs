#if StrategyBbma
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

public class SignalBbMaShort : SignalCreateBase
{
    public SignalBbMaShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.BbMa;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Wma05High == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    private bool PrepareHigherInterval(CryptoIntervalPeriod higher, out CryptoSymbolInterval higherInterval, out CryptoCandle? candle)
    {
        higherInterval = Symbol.GetSymbolInterval(higher);
        long candleOpenTime = IntervalTools.StartOfIntervalCandle2(CandleLast.OpenTime, Interval.Duration, higherInterval.Interval.Duration);
        if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out candle))
            return false;

        if (candle.CandleData == null)
        {
            List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
            if (history == null)
                return false;
            CandleIndicatorData.CalculateIndicators(Symbol, higherInterval.Interval, history);
        }

        return true;
    }


    private bool IsExtreme(CryptoSymbolInterval symbolInterval, CryptoCandle candle, int backward)
    {
        if (candle.CandleData!.Wma05High <= candle.CandleData.BollingerBandsUpperBand)
            return false;

        // go back x extra candle(s)?
        if (backward > 0)
        {
            //if (!symbolInterval.CandleList.TryGetValue(candle.OpenTime - symbolInterval.Interval.Duration, out candle))
            //    return false;

            //if (!IndicatorsOkay(candle))
            //    return false;
        }

        return true;
    }


    public override bool IsSignal()
    {
        CryptoIntervalPeriod higherIntervalPeriod = CryptoIntervalPeriod.interval1m;
        // BBMA codes interval correlation (we do not support the week interval)
        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval5m)
            higherIntervalPeriod = CryptoIntervalPeriod.interval15m;
        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval15m)
            higherIntervalPeriod = CryptoIntervalPeriod.interval1h;
        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval1h)
            higherIntervalPeriod = CryptoIntervalPeriod.interval4h;
        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval4h)
            higherIntervalPeriod = CryptoIntervalPeriod.interval1d;
        //if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval1d)
        //    higherIntervalPeriod = CryptoIntervalPeriod.interval1w;

        if (higherIntervalPeriod == CryptoIntervalPeriod.interval1m)
        {
            ExtraText = $"not accepted interval {Interval.Name}";
            return false;
        }


        if (!IsExtreme(SymbolInterval, CandleLast, 0))
        {
            ExtraText = $"no extreme on interval {Interval.Name}";
            return false;
        }

        // For now just focus on the 2 extremes (the second situation)

        //for example : https://www.forexfactory.com/thread/724759-bbma-strategy-by-oma-ally?page=3
        //H4 : ReEntry H1: ReEntry M15 : Extreme
        //H4 : ReEntry H1: Extreme M15 : Extreme ***
        //H4 : ReEntry H1: Extreme M15 : MHV

        if (!PrepareHigherInterval(higherIntervalPeriod, out CryptoSymbolInterval higherInterval, out CryptoCandle? candle))
            return false;

        if (!IsExtreme(higherInterval, candle!, 1))
        {
            ExtraText = $"no extreme on interval {higherInterval.Interval.Name}";
            return false;
        }


        ExtraText = "";
        return true;
    }

}
#endif