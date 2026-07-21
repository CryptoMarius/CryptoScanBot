using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Storsi.Signal;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class StoRsiLong : StoRsiBase
{
    public override bool AdditionalChecks(MyData data, out string response)
    {
        var settings = StorsiPlugin.Settings;

        if (settings.CheckBollingerBandsCondition)
        {
            if (!InLowerPartOfBollingerBands(3, 5.0m, false))
            {
                response = "not in lower part of bb";
                return false;
            }
        }

        if (settings.SkipFirstSignal)
        {
            if (HadStorsiInThelastXCandles(SignalSide, 1, 3) == null)
            {
                response = "skip first storsi";
                return false;
            }
        }

        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = StorsiPlugin.Settings;

        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (!CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        if (!CandleLast.RsiOversold(settings.AddRsiAmount))
        {
            ExtraText = "rsi not oversold";
            return false;
        }

        return true;
    }

}
