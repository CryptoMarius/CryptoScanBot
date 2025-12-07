using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;


public class SignalSbm3Short : SignalSbmBaseShort
{
    public SignalSbm3Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
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

        if (!this.HasBollingerBandsIncreased(GlobalData.Settings.Signal.Sbm.Sbm3CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm3CandlesBbRecoveryPercentage))
            return false;

        return true;
    }
}
