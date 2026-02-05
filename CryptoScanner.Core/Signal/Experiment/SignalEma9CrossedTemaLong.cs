#if DEBUG
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

public class SignalEma9CrossedTemaLong : SignalCreateBase
{
    public SignalEma9CrossedTemaLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Ema9 == null)
           || (candle.CandleData.Tema == null)
           )
            return false;

        return true;
    }


    private bool HasCrossedProjection()
    {
        CryptoCandle? lastCandle = CandleLast;
        if (GetPrevCandle(lastCandle, out CryptoCandle? prevCandle) &&
            prevCandle!.CandleData!.Ema9 > prevCandle.CandleData.Tema &&
            lastCandle.CandleData!.Ema9 > lastCandle.CandleData.Tema)
        {
            // Both still above the tema, is the price projection under the tema?

            // previous
            decimal ema9prev = (decimal)prevCandle!.CandleData!.Ema9!;
            decimal tema5prev = (decimal)prevCandle!.CandleData!.Tema!;

            // current
            decimal ema9last = (decimal)lastCandle!.CandleData!.Ema9!;
            decimal tema5last = (decimal)lastCandle!.CandleData!.Tema!;

            // projection
            decimal ema9proj = ema9last + (ema9last - ema9prev);
            decimal tema5proj = tema5last + (tema5last - tema5prev);

            if (ema9proj < tema5proj)
            {
                // plus check percentage before crossing, need to be > 0.5%?
                if (!GetPrevCandle(prevCandle, out prevCandle))
                    return false;
                decimal distance = Math.Abs((decimal)prevCandle!.CandleData!.Ema9! - (decimal)prevCandle.CandleData.Tema!);
                decimal distancePerc = distance / (decimal)prevCandle.CandleData.Tema! * 100;
                if (distancePerc > 0.5m)
                {
                    ExtraText = $"{distancePerc:N2} (projection)";
                    return true;
                }
            }
        }

        return false;
    }


    private bool HasCrossedNow()
    {
        CryptoCandle? lastCandle = CandleLast;
        if (GetPrevCandle(lastCandle, out CryptoCandle? prevCandle) &&
            prevCandle!.CandleData!.Ema9 > prevCandle.CandleData.Tema &&
            lastCandle.CandleData!.Ema9 < lastCandle.CandleData.Tema)
        {
            // check percentage before crossing, need to be > 0.5%?
            if (!GetPrevCandle(prevCandle, out prevCandle))
                return false;
            decimal distance = Math.Abs((decimal)prevCandle!.CandleData!.Ema9! - (decimal)prevCandle.CandleData.Tema!);
            decimal distancePerc = distance / (decimal)prevCandle.CandleData.Tema! * 100;
            if (distancePerc < 0.5m)
                return false;

            ExtraText = $"{distancePerc:N2}";
            return true;
        }

        return false;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        return HasCrossedNow(); // || HasCrossedProjection();
    }

}

#endif
