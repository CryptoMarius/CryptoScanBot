using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class SignalStoRsiMultiLong : SignalSbmBaseLong
{
    public SignalStoRsiMultiLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.StoRsiMulti;
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Rsi == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // disable sbm conditions
        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = "bb.width too small " + CandleLast.CandleData!.BollingerBandsPercentage?.ToString("N2");
            return false;
        }
        long unixDate = CandleLast.OpenTime;

        //if (!CandleLast.IsStochOversold(GlobalData.Settings.Signal.StoRsi.AddStochAmount))
        //{
        //    ExtraText = "stoch not oversold";
        //    return false;
        //}

        //if (!CandleLast.IsRsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
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
            long candleOpenTime = IntervalTools.StartOfIntervalCandle2(unixDate, Interval.Duration, higherInterval.Interval.Duration);
            if (!higherInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle? candle))
                return false;

            if (candle.CandleData == null)
            {
                List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, higherInterval.Interval, candleOpenTime, out string _);
                if (history == null)
                    return false;
                CandleIndicatorData.CalculateIndicators(history);
            }

            if (IndicatorsOkay(candle!) && candle.IsStochOversold(GlobalData.Settings.Signal.StoRsi.AddStochAmount) && candle.IsRsiOversold(GlobalData.Settings.Signal.StoRsi.AddRsiAmount))
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

            if (intervalPeriod == CryptoIntervalPeriod.interval1d)
                return false;
            intervalPeriod++;
        }


        //// close date shouw be in the lower part of the bb
        //if (!IsInLowerPartOfBollingerBands(1, 10.0m))
        //    return false;

        ExtraText = "";
        return false;
    }


}
