using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalSbm2Long : SignalSbmBaseLong
{
    public SignalSbm2Long(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Sbm2;
    }


    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!IsInLowerPartOfBollingerBands(GlobalData.Settings.Signal.Sbm.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm2BbPercentage))
        {
            ExtraText = "geen lage prijs in de laatste x candles";
            return false;
        }

        return true;
    }


}
