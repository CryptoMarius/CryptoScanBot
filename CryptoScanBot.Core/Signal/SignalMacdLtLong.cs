using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalMacdLtLong : SignalCreateBase
{
    public SignalMacdLtLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.MacdLt;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.MacdLtHistogram == null
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return $"MacdLt.Histogram={CandleLast.CandleData.MacdLtHistogram}";
    }


    public override bool IsSignal()
    {
        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Is er een macd crossing op de Macd 34/144
        if (CandleLast.CandleData.MacdLtHistogram < 0)
            return false;
        if (!GetPrevCandle(CandleLast, out CryptoCandle candlePrev))
            return false;
        if (candlePrev.CandleData.MacdLtHistogram > 0)
            return false;


        // Er sprake was van een 1.5 percentage drop in de "rode" periode
        // (op de lagere intervallen wil 1 candle wel eens storen)
        decimal minValue = decimal.MaxValue;
        decimal maxValue = decimal.MinValue;
        while (candlePrev.CandleData.MacdLtHistogram <= 0)
        {
            decimal value = candlePrev.Low;
            if (value < minValue)
                minValue = value;
            value = candlePrev.High;
            if (value > maxValue)
                maxValue = value;
            if (!GetPrevCandle(candlePrev, out candlePrev))
                return false;
        }
        decimal perc = 100m * (maxValue / minValue - 1);
        if (perc <= 2.5m)
            return false;


        ExtraText = perc.ToString("N2");
        return true;
    }


}

