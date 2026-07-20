using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Sbm.Signal;

public class SignalSbm1Short : SignalSbmBase
{

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(SbmPlugin.Settings.BBMinPercentage, SbmPlugin.Settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Check ma lines
        if (!CandleLast!.IsSbmConditionsOverbought())
        {
            ExtraText = "no sbm conditions";
            return false;
        }

        // Check psar above sma20
        if (!CandleLast!.IsSbmConditionsPSarOverbought())
        {
            ExtraText = "psar not above sma20";
            return false;
        }

        if (!this.IsStobbInThelastXCandlesOverbought(SbmPlugin.Settings.Sbm1CandlesLookbackCount, SbmPlugin.Settings.UseLowHigh))
        {
            ExtraText = "no stob in the last x candles";
            return false;
        }

        if (!this.IsMacdRecoveryOverbought(SbmPlugin.Settings.CandlesForMacdRecovery))
        {
            ExtraText = "no macd recovery";
            return false;
        }

        return true;
    }
}
