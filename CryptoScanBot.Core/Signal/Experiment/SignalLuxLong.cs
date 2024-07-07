using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

namespace CryptoScanBot.Core.Signal.Experiment;

/// <summary>
/// Signal if the coin is oversold and the distance between price and EMA5 is at a peak
/// </summary>
public class SignalLuxLong : SignalSbmBaseLong
{
    public SignalLuxLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Lux;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Rsi == null
           || candle.CandleData.Ema9 == null
           || candle.CandleData.StochSignal == null
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


        // ********************************************************************
        // Rsi
        if (CandleLast.CandleData!.Rsi > GlobalData.Settings.General.RsiValueOversold)
        {
            ExtraText = $"rsi {CandleLast.CandleData.Rsi} not above {GlobalData.Settings.General.RsiValueOversold}";
            return false;
        }


        //if (!GetPrevCandle(CandleLast, out CryptoCandle? prevCandle))
        //    return false;

        //// ********************************************************************
        //// Need some sign of recovery (weak)
        //if (CandleLast.CandleData.Rsi < prevCandle!.CandleData!.Rsi)
        //{
        //    ExtraText = $"rsi {CandleLast.CandleData.Rsi} still decreasing";
        //    return false;
        //}

        //// ********************************************************************
        //// Need some sign of recovery (weak)
        //if (CandleLast.CandleData.StochSignal < prevCandle.CandleData.StochSignal)
        //{
        //    ExtraText = $"stoch {CandleLast.CandleData.Rsi} still decreasing";
        //    return false;
        //}

        // ********************************************************************
        // the distance between ema9 and close needs to be as big as possible (weak)
        // Begins to looks like the SBM method with distances, actually kind of interesting..
        // ==> rubber bands methology & recovery signs
        decimal percentage = 100 * (decimal)CandleLast.CandleData.Ema9! / CandleLast.Close;
        if (percentage < 101.0m)
        {
            ExtraText = $"distance close and ema9 not ok {percentage:N2}";
            return false;
        }

        percentage -= 100;

        // stupid lux?
        //// ********************************************************************
        //// Lux Algo indicator (but can be a long time on 100%)
        //LuxIndicator.Calculate(Symbol, out int luxOverSold, out int _, CryptoIntervalPeriod.interval5m, CandleLast.OpenTime + Interval.Duration);
        //if (luxOverSold < 100)
        //{
        //    ExtraText = $"flux oversold {luxOverSold} below 100";
        //    return false;
        //}


        ExtraText = $"{percentage:N2}%";
        return true;
    }

}
