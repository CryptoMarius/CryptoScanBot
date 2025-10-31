using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalMacdLong : SignalCreateBase
{
    public SignalMacdLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.MacdHistogram == null
           )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Is there a macd crossing
        if (CandleLast.CandleData!.MacdHistogram < 0)
            return false;
        if (!GetPrevCandle(CandleLast, out CryptoCandle? candlePrev))
            return false;
        if (candlePrev!.CandleData!.MacdHistogram > 0)
            return false;

        if (HadStorsiInThelastXCandles(SignalSide, 0, 6, 4) == null)
            return false;

        return true;
    }


}

