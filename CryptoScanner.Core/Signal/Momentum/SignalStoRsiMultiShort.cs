using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiMultiLong : SignalSbmBaseLong
{
    public SignalStoRsiMultiLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Rsi == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    public override bool AdditionalChecks(MyData data, out string response)
    {
        // disable sbm conditions
        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }
        CandleTime unixDate = CandleLast.Candle.OpenTime;

        //if (!CandleLast.StochOversold(0))
        //{
        //    ExtraText = "stoch not oversold";
        //    return false;
        //}

        //if (!CandleLast.RsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
        //{
        //    ExtraText = "rsi not oversold";
        //    return false;
        //}


        // Is it a signal valid over 4 intervals (multistorsi)
        int okay = 4;
        ExtraText = "";
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = 6; count > 0; count--)
        {
            CryptoSymbolInterval higherInterval = Symbol.GetSymbolInterval(intervalPeriod);
            CandleTime candleOpenTime = IntervalTools.StartOfIntervalCandle2(unixDate, Interval.Duration, higherInterval.Interval.Duration);
            if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle _))
                return false;

            // Calculate indicators if needed
            IndicatorDataList.PrepareIndicators(Symbol, higherInterval.Interval, candleOpenTime, out _);
            if (!IndicatorDataList.TryGetCandle(higherInterval.Interval, candleOpenTime, out MyData? candle))
                return false;


            if (IndicatorsOkay(candle!) && candle!.StochOversold() && candle!.RsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
            {
                if (ExtraText != "")
                    ExtraText += ',';
                ExtraText += higherInterval.Interval.Name;

                okay--;
                if (okay == 0)
                    return true;
            }
            else
            {
                // first interval needs to be a signal
                if (count == 6)
                    return false;
            }

            //if (okay < count) return false;

            if (intervalPeriod == CryptoIntervalPeriod.interval1w)
                return false;
            intervalPeriod++;
        }


        //// close date shouw be in the lower part of the bb
        //if (!InLowerPartOfBollingerBands(1, 10.0m))
        //    return false;

        ExtraText = "";
        return false;
    }


}
