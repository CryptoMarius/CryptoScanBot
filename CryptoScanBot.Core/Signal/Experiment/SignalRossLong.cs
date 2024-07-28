using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

public class SignalRossLong : SignalCreateBase
{
    public SignalRossLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Ross;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // ********************************************************************
        // BB width is within certain percentages
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }



        if (CandleLast.CandleData!.Bbr > 0)
            return false;


        // Needs to be oversold in the last 5 candles
        if (WasRsiOversoldInTheLast(5))
            return false;


        // calculate avg volume over the last x candles
        int count = 50;
        decimal sumVolume = 0;
        CryptoCandle? candle = CandleLast;
        while (count > 0)
        {
            count--;
            sumVolume += candle!.Volume;
            if (!GetPrevCandle(candle, out candle))
                return false;
        }
        decimal avgVolume = sumVolume / 50;


       
        // need 3 candles with > 5x avg volume on green candles
        count = 2;
        candle = CandleLast;
        while (count > 0)
        {
            count--;
            if (candle!.Volume < 3m * avgVolume)
                return false;
            if (!GetPrevCandle(candle, out candle))
                return false;
        }


        ExtraText = $"{avgVolume:N2}%";
        return true;
    }

}
