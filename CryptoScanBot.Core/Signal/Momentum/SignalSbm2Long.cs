using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalSbm2Long : SignalSbmBaseLong
{
    public SignalSbm2Long(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
    }


    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!InLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm2BbPercentage))
        {
            ExtraText = "no low price in the last x candles";
            return false;
        }

        return true;
    }


}
