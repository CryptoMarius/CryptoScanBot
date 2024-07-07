using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

// Based on a video from Ross Cameron
// https://www.youtube.com/watch?v=4Pc_von1wS4
// The idea is that with high volume (5*avg volume) the interest is higher and that will continue (not alway's true)
// Btw: I found volume based trading very interesting and this is just a little experiment in that corner..
// Sofar long signals (I did not check 100%) have at least 1% profit (from the scanner's monitoring system)

public class SignalRoss2Short : SignalCreateBase
{
    public SignalRoss2Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Ross2;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Vwap == null
           )
            return false;

        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // ********************************************************************
        // BB width is within certain percentages
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Sbm.BBMinPercentage, GlobalData.Settings.Signal.Sbm.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        //// ********************************************************************
        //// macd histogram positief
        //if (CandleLast.CandleData!.MacdHistogram > 0)
        //{
        //    ExtraText = $"macd histogram not positive {CandleLast.CandleData.MacdHistogram}";
        //    return false;
        //}


        // ********************************************************************
        // price crossed the vwap downwards

        if (!GetPrevCandle(CandleLast, out CryptoCandle? prevCandle))
                return false;

        if (prevCandle!.Close < (decimal)prevCandle.CandleData!.Vwap!)
            return false;

        if (CandleLast.Close > (decimal)CandleLast.CandleData!.Vwap!)
            return false;

        
        ExtraText = $"{CandleLast.CandleData.Vwap:N4}";
        return true;
    }

}
