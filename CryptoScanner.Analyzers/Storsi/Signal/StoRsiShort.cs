using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Storsi.Signal;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class StoRsiShort : StoRsiBase
{
    public override bool AdditionalChecks(MyData data, out string response)
    {
        var settings = StorsiPlugin.Settings;
        if (settings.OnlyIfLux5m)
        {
            int needed = settings.Lux5mPercentage;
            if (CandleLast.CandleData!.Lux5mValue < needed)
            {
                response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%, need >= {needed}%)";
                return false;
            }
        }

        if (settings.CheckBollingerBandsCondition)
        {
            if (!InUpperPartOfBollingerBands(3, 5.0m))
            {
                response = "not in upper part of bb";
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

        if (!CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        if (!CandleLast.RsiOverbought(settings.AddRsiAmount))
        {
            ExtraText = "rsi not overbought";
            return false;
        }

        // ********************************************************************
        // Dont trade against the trend (only check current interval)
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        // Optional zone-rejection confirmation (DLZ / FVG / SMC). OR over enabled types.
        if (!CheckEnabledZoneRejections(out string zoneInfo))
        {
            ExtraText = zoneInfo;
            return false;
        }
        if (zoneInfo.Length > 0)
            ExtraText = $"storsi+{zoneInfo}";

        return true;
    }

}
