using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalSbm1Long : SignalSbmBaseLong
{
    public SignalSbm1Long(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
    }


    public bool HadStobbInThelastXCandles(int candleCount)
    {
        // Was price close to the edge of the bb?
        CryptoCandle? last = CandleLast;
        while (candleCount-- > 0)
        {
            // Closes or opens below the bb & stochastic oversold situation 
            if (last!.IsBelowBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh) && last!.StochOversold())
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
        }

        return false;
    }



    public override bool IsSignal()
    {
        if (!base.IsSignal())
            return false;

        if (!HadStobbInThelastXCandles(GlobalData.Settings.Signal.Sbm.Sbm1CandlesLookbackCount))
        {
            ExtraText = "no stob in the last x candles";
            return false;
        }

        return true;
    }


}
