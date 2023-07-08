using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

#if EXTRASTRATEGIES
public class SignalSlopeEma50TurningNegative : SignalCreateBase
{
    public SignalSlopeEma50TurningNegative(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = CryptoOrderSide.Buy;
        SignalStrategy = CryptoSignalStrategy.SlopeEma50;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Ema20 == null)
           || (candle.CandleData.Ema50 == null)
           || (candle.CandleData.Ema200 == null)
           || (candle.CandleData.SlopeEma20 == null)
           || (candle.CandleData.SlopeEma50 == null)
           )
            return false;

        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        if (CandleLast.CandleData.SlopeEma50 > 0)
            return false;

        if (CandleLast.CandleData.Ema50 < CandleLast.CandleData.Ema200)
            return false;


        if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out CryptoCandle prevCandle))
        {
            ExtraText = "geen prev candle! " + CandleLast.DateLocal.ToString();
            return false;
        }
        if (!IndicatorsOkay(prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeEma50 < 0)
            return false;

        if (!BarometersOkay())
        {
            ExtraText = "barometer te laag";
            return false;
        }

        return true;
    }

}
#endif