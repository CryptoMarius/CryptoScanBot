using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Storsi;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiLong : SignalStoRsiBase
{
    public override bool AdditionalChecks(MyData data, out string response)
    {
        if (GlobalData.Settings.Signal.StoRsi.OnlyIfLux5m)
        {
            int needed = GlobalData.Settings.Signal.StoRsi.Lux5mPercentage;
            if (CandleLast.CandleData!.Lux5mValue > -needed)
            {
                response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%, need <= -{needed}%)";
                return false;
            }
        }

        if (GlobalData.Settings.Signal.StoRsi.CheckBollingerBandsCondition)
        {
            if (!InLowerPartOfBollingerBands(3, 5.0m))
            {
                response = "not in lower part of bb";
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

        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = GlobalData.Settings.Signal.StoRsi;

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

        // ********************************************************************
        // Dont trade against the trend (only check current interval)
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        return true;
    }

}
