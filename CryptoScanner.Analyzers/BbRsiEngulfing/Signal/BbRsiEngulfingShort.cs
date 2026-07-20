using CryptoScanner.Analyzers.Stobb;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.BbRsiEngulfing.Signal;

public class BbRsiEngulfingShort : SignalCreateBase
{


    public override bool IndicatorsOkay(MyData data)
    {
        if ((data == null)
           || data.Candle.OpenTime == 0
           || (data.CandleData == null)
            || (data.CandleData.Rsi == null)
            || (data.CandleData.BollingerBandsUpperBand == null)
            )
            return false;

        return true;
    }



    public override bool IsSignal()
    {
        ExtraText = "";

        // BB width must be at least 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(StobbPlugin.Settings.BBMinPercentage, 0)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        if (!GetPrevCandle(CandleLast!, out MyData? prev))
            return false;


        // Prev above BB
        //if (!prev!.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        if (prev!.Candle.Close <= (decimal)prev!.CandleData!.BollingerBandsUpperBand!)
        {
            ExtraText = "not above bb.upper";
            return false;
        }

        // Rsi overbought
        if (!prev!.RsiOverbought(4))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        // Candle last closes below the low of the previous
        if (CandleLast.Candle.Close >= prev!.Candle.Low)
        {
            ExtraText = "not engulfing";
            return false;
        }

        if (HadStorsiInThelastXCandles(SignalSide, 0, 25, 4) == null && HadStobbInThelastXCandles(SignalSide, 0, 25, StobbPlugin.Settings.UseLowHigh) == null)
        {
            ExtraText = "no previous storsi/stobb found";
            return false;
        }

        return true;
    }

}
