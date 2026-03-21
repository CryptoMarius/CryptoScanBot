using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

/// <summary>
/// Stochastic Directional Short strategy.
///
/// The idea: the 1h stochastic was recently overbought and is now falling toward oversold
/// (but not there yet). This tells us the 1h direction is down. The current (lower) interval
/// stochastic is also falling, confirming a good short entry moment.
///
/// Designed to run on the 5m interval.
/// </summary>
public class SignalStochDirShort : SignalSbmBaseShort
{

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
            || data.CandleData == null
            || data.CandleData.StochSignal == null
            || data.CandleData.StochOscillator == null)
            return false;

        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // This strategy is meant for intervals below 1h
        if (Interval.IntervalPeriod >= CryptoIntervalPeriod.interval1h)
            return false;

        // Step 1: Get the 1h candle and verify its direction is down
        var result = IndicatorDataList.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, CryptoIntervalPeriod.interval1h);
        if (!result.success)
            return false;

        MyData hourData = result.candle!;

        // 1h must have had a substantial overbought period (raised threshold = more extreme/prolonged peak)
        double stochSurface1h = this.StochOverboughtSurface(result.higherInterval, hourData, 30, GlobalData.Settings.General.SettingsStoch.Overbought);
        if (stochSurface1h < 15)
        {
            ExtraText = $"1h not overbought enough ({stochSurface1h:N1})";
            return false;
        }

        // 1h stoch must have exited the overbought zone
        if (hourData.CandleData?.StochOscillator >= GlobalData.Settings.General.SettingsStoch.Overbought)
        {
            ExtraText = "1h stoch still overbought";
            return false;
        }

        // 1h stoch must not yet have reached oversold (direction not complete)
        if (hourData.CandleData?.StochOscillator <= GlobalData.Settings.General.SettingsStoch.Oversold)
        {
            ExtraText = "1h stoch already oversold";
            return false;
        }

        // 1h stoch must have traveled at least 15 points down from the overbought boundary,
        // ensuring a real move has taken place and not just a tiny pullback
        const double MinStochTravel = 15.0;
        double stoch1h = hourData.CandleData!.StochOscillator!.Value;
        double stochTraveled = GlobalData.Settings.General.SettingsStoch.Overbought - stoch1h;
        if (stochTraveled < MinStochTravel)
        {
            ExtraText = $"1h stoch barely moved ({stochTraveled:N1} < {MinStochTravel})";
            return false;
        }

        // 1h BB width: the market must have been volatile enough on the 1h
        if (hourData.CandleData.BollingerBandsPercentage == null
            || !hourData.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"1h bb.width too small ({hourData.CandleData.BollingerBandsPercentage:N2})";
            return false;
        }

        // 1h stoch must be falling
        if (!this.StochDecreasingInTheLast(result.higherInterval, hourData, 3, 1))
        {
            ExtraText = "1h stoch not falling";
            return false;
        }

        // Step 2: Current interval (5m) stoch must also be falling
        if (!this.StochDecreasingInTheLast(SymbolInterval, CandleLast, 2, 999))
        {
            ExtraText = "stoch not falling";
            return false;
        }

        double stoch5m = CandleLast.CandleData!.StochOscillator!.Value;
        ExtraText = $"5m:{stoch5m:N1} 1h:{stoch1h:N1} trvl:{stochTraveled:N1} surf:{stochSurface1h:N1} bb:{hourData.CandleData.BollingerBandsPercentage:N2}";

        return true;
    }
}
