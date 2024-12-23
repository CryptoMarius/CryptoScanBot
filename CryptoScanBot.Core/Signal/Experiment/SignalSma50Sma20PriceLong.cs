using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalSma50Sma20PriceLong : SignalCreateBase
{
    public SignalSma50Sma20PriceLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.SignalSma50Sma20Price;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
            || (candle.CandleData.Sma20 == null)
            || (candle.CandleData.Sma50 == null)
            || (candle.CandleData.BollingerBandsLowerBand == null)
            )
            return false;

        return true;
    }


    private static bool IsInLowerPartOfBollingerBands(CryptoCandle candle, decimal percentage)
    {
        decimal? value = (decimal?)candle?.CandleData?.BollingerBandsLowerBand;
        value += (decimal?)candle?.CandleData?.BollingerBandsDeviation * percentage / 100m;
        if (candle?.Close <= value)
            return true;
        return false;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100)) //GlobalData.Settings.Signal.AnalysisBBMaxPercentage
        {
            ExtraText = "bb.width to small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }


        if (CandleLast.CandleData!.Sma50 >= CandleLast.CandleData.Sma20)
            return false;

        if (!IsInLowerPartOfBollingerBands(1, 7.5m))
            return false;


        // Is it near the bb in a higher timeframe?
        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval8h)
        {
            CryptoSymbolInterval higherInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod + 3);
            long candleOpenTime = IntervalTools.StartOfIntervalCandle2(CandleLast.OpenTime, Interval.Duration, higherInterval.Interval.Duration);
            if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle? candle))
                return false;

            if (candle.CandleData == null)
            {
                List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
                if (history == null)
                    return false;
                CandleIndicatorData.CalculateIndicators(history);
            }

            if (!IndicatorsOkay(candle))
                return false;
            if (!IsInLowerPartOfBollingerBands(candle, 30m))
                return false;
        }

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        // Langer dan 3 candles willen we niet wachten
        if (CandleLast.OpenTime - signal.EventTime > 3 * Interval.Duration)
        {
            ExtraText = "Ophouden na 3 candles";
            return true;
        }

        ExtraText = "";
        return false;
    }

}
