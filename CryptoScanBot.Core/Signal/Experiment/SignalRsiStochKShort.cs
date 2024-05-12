using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

#if EXTRASTRATEGIESPSARRSI
public class SignalRsiStochKShort : SignalCreateBase
{
    public SignalRsiStochKShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        ReplaceSignal = false;
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.RsiStochK;
    }

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
            || candle.CandleData?.Rsi == null
            || candle.CandleData?.StochOscillator == null
            )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // The width of bollingband is acceptable to make a profit (not to small)
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = "bb.width te klein " + CandleLast?.CandleData?.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;
        if (!GetPrevCandle(prevCandle, out CryptoCandle prevCandle2))
            return false;


        // below overbought
        if (CandleLast?.CandleData?.Rsi >= GlobalData.Settings.General.RsiValueOverbought)
            return false;

        // crossing stoch.k/rsi
        if (prevCandle2?.CandleData?.Rsi < prevCandle?.CandleData?.StochOscillator)
            return false;
        if (CandleLast?.CandleData?.Rsi > CandleLast?.CandleData?.StochOscillator)
            return false;

        // quick recovery from overbought area?
        if (!WasRsiOverboughtInTheLast(10))
            return false;
        if (HadStobbInThelastXCandles(CryptoTradeSide.Short, 1, 10) == null)
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