using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Sbm.Signal;

public class SignalSbm3Long : SignalSbmBase
{

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast!.CheckBollingerBandsWidth(SbmPlugin.Settings.BBMinPercentage, SbmPlugin.Settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Check ma lines
        if (!CandleLast!.IsSbmConditionsOversold())
        {
            ExtraText = "no sbm conditions";
            return false;
        }

        // Check psar below sma20
        if (!CandleLast!.IsSbmConditionsPSarOversold())
        {
            ExtraText = "psar not below sma20";
            return false;
        }

        if (!this.IsBollingerBandsIncreased(SbmPlugin.Settings.Sbm3CandlesLookbackCount, SbmPlugin.Settings.Sbm3CandlesBbRecoveryPercentage))
            return false;

        if (!this.IsMacdRecoveryOversold(SbmPlugin.Settings.CandlesForMacdRecovery))
        {
            ExtraText = "no macd recovery";
            return false;
        }

        return true;
    }
}