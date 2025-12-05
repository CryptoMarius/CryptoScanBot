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

        // De ma lijnen en psar goed staan
        if (!CandleLast!.IsSbmConditionsOverbought(true))
        {
            ExtraText = "no sbm conditions";
            return false;
        }

        if (!this.HasBollingerBandsIncreased(GlobalData.Settings.Signal.Sbm.Sbm3CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm3CandlesBbRecoveryPercentage))
            return false;

        return true;
    }
}
