﻿using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalMacdTestLong : SignalCreateBase
{
    public SignalMacdTestLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.MacdTest;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Rsi == null)
           || (candle.CandleData.StochSignal == null)
           || (candle.CandleData.StochOscillator == null)
           || (candle.CandleData.MacdTestHistogram == null)
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return $"MacdLt.Histogram={CandleLast.CandleData.MacdTestHistogram}";
    }


    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        if (CandleLast.CandleData.Rsi < 50)
            return false;

        // Is er een macd crossing op de Macd 34/144
        if (CandleLast.CandleData.MacdTestHistogram < 0)
            return false;
        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
            return false;
        if (candlePrev.CandleData.MacdTestHistogram > 0)
            return false;

        //if ((decimal)candlePrev.CandleData.PSar.Value > CandleLast.Close)
          //  return false;


        // Er sprake was van een 1.5 percentage drop in de "rode" periode
        // (op de lagere intervallen wil 1 candle wel eens storen)
        int count = 0;
        bool oversold = true;
        decimal minValue = decimal.MaxValue;
        decimal maxValue = decimal.MinValue;
        while (candlePrev.CandleData.MacdTestHistogram <= 0)
        {
            count++;
            decimal value = candlePrev.Low;
            if (value < minValue)
                minValue = value;
            value = candlePrev.High;
            if (value > maxValue)
                maxValue = value;

            //if (candlePrev.IsStochOversold() || candlePrev.IsRsiOversold())
              //  oversold = true;

            if (!GetPrevCandle(candlePrev, out candlePrev))
                return false;
        }

        if (count < 20 || !oversold)
            return false;

        //if (CandleLast.CandleData.SlopeEma20 < 0)
        //    return false;


        decimal perc = 100m * (maxValue / minValue - 1);
        if (perc <= 2.0m)
            return false;

        ExtraText = perc.ToString("N2") + "SlopeEma " +CandleLast.CandleData.SlopeEma50?.ToString("N7") + " rsi " + CandleLast.CandleData.Rsi?.ToString("N2");
        return true;
    }


}

