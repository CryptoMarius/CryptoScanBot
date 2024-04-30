using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

#if EXTRASTRATEGIESFISHER
public class SignalFisherTransformLong : SignalCreateBase
{
    public SignalFisherTransformLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        ReplaceSignal = false;
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Fisher;
    }

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
            || candle.CandleData?.FisherValue == null
            || candle.CandleData?.FisherTrigger == null
            )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // Too many results
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval3m)
            return false;

        //if (CandleLast?.CandleData?.Sma200 >= (double)CandleLast?.Close)
        //    return false;

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width te klein " + CandleLast?.CandleData?.BollingerBandsPercentage?.ToString("N2");
            return false;
        }


        if (CandleLast?.CandleData?.FisherTrigger >= CandleLast?.CandleData?.FisherValue)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle?.CandleData?.FisherTrigger <= prevCandle?.CandleData?.FisherValue)
            return false;

        // detect false signals?

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // Langer dan 3 candles willen we niet wachten
        if (CandleLast?.OpenTime - signal.EventTime > 3 * Interval.Duration)
        {
            ExtraText = "Ophouden na 3 candles";
            return true;
        }

        ExtraText = "";
        return false;
    }

}
#endif