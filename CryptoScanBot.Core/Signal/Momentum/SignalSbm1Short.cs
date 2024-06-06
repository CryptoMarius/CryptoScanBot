using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalSbm1Short : SignalSbmBaseShort
{
    public SignalSbm1Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
        SignalStrategy = CryptoSignalStrategy.Sbm1;
    }


    public bool HadStobbInThelastXCandles(int candleCount)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        CryptoCandle? last = CandleLast;
        while (candleCount > 0)
        {
            // Er een candle onder de bb opent of sluit & een overbought situatie (beide moeten onder de 20 zitten)
            if (last!.IsAboveBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh) && last.IsStochOverbought())
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }



    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!HadStobbInThelastXCandles(GlobalData.Settings.Signal.Sbm.Sbm1CandlesLookbackCount))
        {
            ExtraText = "geen stob in de laatste x candles";
            return false;
        }

        return true;
    }


}
