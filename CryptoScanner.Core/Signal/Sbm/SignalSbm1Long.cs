using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Sbm;

public class SignalSbm1Long : SignalSbmBase
{

    // TODO: Stoch cross over %K/%D (in AllowStepIn)

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast!.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
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

        if (!this.HadStobbInThelastXCandlesOversold(GlobalData.Settings.Signal.Sbm.Sbm1CandlesLookbackCount))
        {
            ExtraText = "no stob in the last x candles";
            return false;
        }

        if (!this.IsMacdRecoveryOversold(GlobalData.Settings.Signal.Sbm.CandlesForMacdRecovery))
        {
            ExtraText = "no macd recovery";
            return false;
        }

        return true;
    }
}
