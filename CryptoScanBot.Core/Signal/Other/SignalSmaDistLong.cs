using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;


public class SignalSmaDistLong : SignalCreateBase
{
    public SignalSmaDistLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Sma20 == null
           )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (CandleLast!.Close > (decimal)CandleLast.CandleData!.Sma20!)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle? candlePrev))
            return false;

        decimal dist1 = Math.Abs(100 * (CandleLast!.Close - (decimal)CandleLast.CandleData!.Sma20!) / (decimal)CandleLast.CandleData!.Sma20!);
        decimal dist2 = Math.Abs(100 * (candlePrev!.Close - (decimal)candlePrev.CandleData!.Sma20!) / (decimal)candlePrev.CandleData!.Sma20!);

        if (dist1 < dist2 || dist1 < 2.50m)
        {
            ExtraText = "";
            return false;
        }

        ExtraText = $"{dist1:N2} {dist2:N2}";
        return true;
    }

}
