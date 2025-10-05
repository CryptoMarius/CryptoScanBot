using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

#if DEBUG
public class SignalBbMaLong : SignalCreateBase
{
    public SignalBbMaLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Ema50 == null
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
        // go back x extra candle(s)?
        while (backward-- > 0)
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
            bool advanceExtreme = false; //candle.Low <= ema50 && candle.Close > ema50 && candle.Close > candle.Open;

            if (extremeTypeA || extremeTypeB || advanceExtreme || magicExtreme)
                return true;

            if (!GetPrevCandle(candle, out CryptoCandle? prev))
                return false;
            candle = prev!;
        }

        return false;
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
        if (Interval.IntervalPeriod == CryptoIntervalPeriod.interval1d)
            higherIntervalPeriod = CryptoIntervalPeriod.interval1w;

        if (higherIntervalPeriod == CryptoIntervalPeriod.interval1m)
        {
            ExtraText = $"not accepted interval {Interval.Name}";
            return false;
        }


        if (!IsExtreme(SymbolInterval, CandleLast, 2))
            return false;

        // For now just focus on the 2 extremes (the second situation)

        // REE,   1h Reentry 15m Extreme, 5m Extreme
        // REM,   1h Reentry 15m Extreme, 5m Momentum?

        if (!PrepareHigherInterval(higherIntervalPeriod, out CryptoSymbolInterval higherInterval, out CryptoCandle? candle))
            return false;

        if (!IsExtreme(higherInterval, candle!, 2))
            return false;


        return true;
    }

}
#endif