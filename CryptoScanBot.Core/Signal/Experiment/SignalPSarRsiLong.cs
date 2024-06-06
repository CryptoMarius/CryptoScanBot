using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

#if EXTRASTRATEGIESPSARRSI
public class SignalPSarRsiLong : SignalCreateBase
{
    public SignalPSarRsiLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        ReplaceSignal = false;
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.PSarRsi;
    }

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
            || candle.CandleData?.PSar == null
            || candle.CandleData?.Rsi == null
            || candle.CandleData?.Sma200 == null
            )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        //if ((double?)CandleLast?.Close >= CandleLast?.CandleData?.Sma200)
        //    return false;

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = "bb.width te klein " + CandleLast?.CandleData?.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        if (!GetPrevCandle(CandleLast, out CryptoCandle? prevCandle))
            return false;
        if (!GetPrevCandle(prevCandle, out CryptoCandle? prevCandle2))
            return false;


        // crossing psar (1 candle extra)
        if (prevCandle2?.CandleData?.PSar <= (double?)prevCandle2?.Close)
            return false;
        if (CandleLast?.CandleData?.PSar >= (double?)CandleLast?.Close)
            return false;

        // crossing rsi (1 candle extra)
        if (prevCandle2?.CandleData?.Rsi > GlobalData.Settings.General.RsiValueOversold)
            return false;
        if (CandleLast?.CandleData?.Rsi < GlobalData.Settings.General.RsiValueOversold)
            return false;

        // Macd pink (recovering)
        if (prevCandle?.CandleData?.MacdHistogram >= 0)
            return false;
        if (CandleLast?.CandleData?.MacdHistogram >= 0)
            return false;
        if (CandleLast?.CandleData?.MacdHistogram <= prevCandle?.CandleData?.MacdHistogram)
            return false;

        // quick recovery from oversold area?
        if (!WasRsiOversoldInTheLast(10))
            return false;
        if (HadStobbInThelastXCandles(CryptoTradeSide.Long, 1, 10) == null)
            return false;

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