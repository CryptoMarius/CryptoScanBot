using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiShort : SignalSbmBase
{


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Rsi == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        if (GlobalData.Settings.Signal.StoRsi.OnlyIfLux5m)
        {
            if (CandleLast.CandleData!.Lux5mValue < 50)
            {
                response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%)";
                return false;
            }
        }

        // Check above/below STOBB BB bands
        if (GlobalData.Settings.Signal.StoRsi.CheckBollingerBandsCondition)
        {
            //if (!CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseHighLow))
            if (!InUpperPartOfBollingerBands(3, 5.0m))
            {
                response = "not in upper part of bb";
                return false;
            }
        }

        if (GlobalData.Settings.Signal.StoRsi.SkipFirstSignal)
        {
            if (HadStorsiInThelastXCandles(SignalSide, 1, 3) == null)
            {
                response = "skip first storsi";
                return false;
            }
        }

        // disable sbm conditions (inheritance)
        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (!CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!CandleLast.RsiOverbought(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        ExtraText = "";
        return true;
    }

}
