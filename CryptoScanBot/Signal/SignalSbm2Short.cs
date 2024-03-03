using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Signal;

public class SignalSbm2Short : SignalSbmBaseShort
{
    public SignalSbm2Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Sbm2;
    }


    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!IsInUpperPartOfBollingerBands(GlobalData.Settings.Signal.Sbm.Sbm2CandlesLookbackCount, GlobalData.Settings.Signal.Sbm.Sbm2BbPercentage))
        {
            ExtraText = "geen hoge prijs in de laatste x candles";
            return false;
        }

        return true;
    }


}
