using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

public class SignalBbRsiEngulfingShort : SignalCreateBase
{
    public SignalBbRsiEngulfingShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
            || (candle.CandleData.Rsi == null)
            || (candle.CandleData.BollingerBandsLowerBand == null)
            )
            return false;

        return true;
    }



    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        if (!GetPrevCandle(CandleLast!, out CryptoCandle? prev))
            return false;


        // Prev above BB
        //if (!prev!.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        if (prev!.Close <= (decimal)prev!.CandleData!.BollingerBandsUpperBand!)
        {
            ExtraText = "not below bb.upper";
            return false;
        }

        // Rsi oversold
        if (!prev!.RsiOverbought(4))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        // Candle last closes above the high of the previous
        if (CandleLast.Close >= prev!.Low)
        {
            ExtraText = "not engulfing";
            return false;
        }

        if (HadStorsiInThelastXCandles(SignalSide, 0, 25, 4) == null && HadStobbInThelastXCandles(SignalSide, 0, 25) == null)
        {
            ExtraText = "no previous storsi/stobb found";
            return false;
        }

        return true;
    }

}
