using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Momentum;

public class SignalSbm1Short : SignalSbmBaseShort
{
    public SignalSbm1Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


    public bool HadStobbInThelastXCandles(int candleCount)
    {
        // Was price close to the edge of the bb?
        CryptoCandle? last = CandleLast;
        while (candleCount > 0)
        {
            if (last == null)
                return false;
            // Closes or opens above the bb & stochastic overbought situation 
            if (last!.IsAboveBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh) && last.StochOverbought()) 
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
            ExtraText = "no stob in the last x candles";
            return false;
        }

        return true;
    }


}
