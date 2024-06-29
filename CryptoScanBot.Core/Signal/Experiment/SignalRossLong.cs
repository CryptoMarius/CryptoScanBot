using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Experiment;

// Based on a video from Ross Cameron
// https://www.youtube.com/watch?v=4Pc_von1wS4
// The idea is that with high volume (5*avg volume) the interest is higher and that will continue (not alway's true)
// Btw: I found volume based trading very interesting and this is just a little experiment in that corner..
// Sofar long signals (I did not check 100%) have at least 1% profit (from the scanner's monitoring system)

public class SignalRossLong : SignalCreateBase
{
    public SignalRossLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Ross;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        response = "";

        //// ********************************************************************
        //// Price must be below BB.Low
        //if (CandleLast.Close >= (decimal)CandleLast.CandleData.BollingerBandsLowerBand)
        //{
        //    ExtraText = "price above bb.low";
        //    return false;
        //}

        //// ********************************************************************
        //// percentage ema5 and close at least 1%
        //decimal percentage = 100 * (decimal)CandleLast.CandleData.Ema5 / CandleLast.Close;
        //if (percentage < 100.25m)
        //{
        //    response = $"distance close and ema5 not ok {percentage:N2}";
        //    return false;
        //}


        //// ********************************************************************
        //// MA lines without psar
        //if (!CandleLast.IsSbmConditionsOversold(false))
        //{
        //    response = "geen sbm condities";
        //    return false;
        //}

        //// ********************************************************************
        //if (!IsRsiIncreasingInTheLast(3, 1))
        //{
        //    response = string.Format("RSI niet oplopend in de laatste 3,1");
        //    return false;
        //}


        return true;
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

        // ********************************************************************
        // macd histogram positief
        if (CandleLast.CandleData.MacdHistogram < 0)
        {
            ExtraText = $"macd histogram not positive {CandleLast.CandleData.MacdHistogram}";
            return false;
        }


        // ********************************************************************
        // price > vwap
        if (CandleLast.Close < (decimal)CandleLast.CandleData.Vwap!)
        {
            ExtraText = $"price below vwap {CandleLast.CandleData.Vwap}";
            return false;
        }


        // calculate avg volume over the last 500 candles
        int count = 500;
        decimal sumVolume = 0;
        //decimal sumMacd = 0;
        CryptoCandle? candle = CandleLast;
        while (count > 0)
        {
            count--;
            sumVolume += candle!.Volume;
            //if (candle!.CandleData != null) // we only have around 60 macd values...
                //sumMacd += (decimal)candle!.CandleData!.MacdHistogram!;
            if (!GetPrevCandle(candle, out candle))
                return false;
        }
        decimal avgVolume = sumVolume / 500;
        //decimal avgMacd = sumMacd/ 500;


        // need 5 candles with > 5x avg volume
        // // deleted=and 4 candles with > 5x macd "volume"
        count = 3;
        candle = CandleLast;
        while (count > 0)
        {
            count--;
            if (candle!.Volume < 4.5m * avgVolume)
                return false;
            //if ((decimal)candle!.CandleData!.MacdHistogram! < 5 * avgMacd)
              //  return false;

            if (candle.CandleData!.MacdHistogram < 0)
                return false;

            if (!GetPrevCandle(candle, out candle))
                return false;
        }



        ExtraText = $"{avgVolume:N2}%";
        return true;
    }

}
