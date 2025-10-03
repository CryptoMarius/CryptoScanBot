using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

#if DEBUG
public class SignalBbMaLong : SignalCreateBase
{
    public SignalBbMaLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.BbMa;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Wma05Low == null
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
            List<CryptoCandle>? history = CandleIndicatorData.CollectCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
            if (history == null)
                return false;
            CandleIndicatorData.CalculateIndicators(Symbol, higherInterval.Interval, history);
        }

        return true;
    }


    private bool IsExtreme(CryptoSymbolInterval symbolInterval, CryptoCandle candle, int backward)
    {
        decimal ema50 = (decimal)candle.CandleData!.Ema50!;
        decimal wma05Low = (decimal)candle.CandleData!.Wma05Low!;
        decimal wma10Low = (decimal)candle.CandleData!.Wma10Low!;
        decimal bbLower = (decimal)candle.CandleData!.BollingerBandsLowerBand!.Value;

        // Extreme Type A: LWMA 5 high/low closes above/below BB
        bool extremeTypeA = wma05Low < bbLower;
        // Extreme Type B: Bullish/bearish candle rejects BB
        bool extremeTypeB = candle.Low <= bbLower && candle.Close > bbLower && candle.Close > candle.Open;
        // Magic Extreme: LWMA 5 + LWMA 10 outside BB
        bool magicExtreme = extremeTypeA && wma10Low < bbLower && candle.Close > candle.Open;
        // Advance Extreme: Price rejects EMA 50 (wick rejection)
        bool advanceExtreme = candle.Low <= ema50 && candle.Close > ema50 && candle.Close > candle.Open;


        bool mlvPotential = extremeTypeA || extremeTypeB || advanceExtreme || magicExtreme;

        // go back x extra candle(s)?
        //if (backward > 0)
        //{
        //if (!symbolInterval.CandleList.TryGetValue(candle.OpenTime - symbolInterval.Interval.Duration, out candle))
        //    return false;

        //if (!IndicatorsOkay(candle))
        //    return false;
        //}

        return mlvPotential;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

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
            return false;

        // For now just focus on the 2 extremes (the second situation)

        //for example : https://www.forexfactory.com/thread/724759-bbma-strategy-by-oma-ally?page=3
        //H4 : ReEntry H1: ReEntry M15 : Extreme
        //H4 : ReEntry H1: Extreme M15 : Extreme ***
        //H4 : ReEntry H1: Extreme M15 : MHV

        if (!PrepareHigherInterval(higherIntervalPeriod, out CryptoSymbolInterval higherInterval, out CryptoCandle? candle))
            return false;

        if (!IsExtreme(higherInterval, candle!, 1))
            return false;


        return true;
    }

}
#endif