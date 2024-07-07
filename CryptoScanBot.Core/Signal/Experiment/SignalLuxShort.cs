﻿using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal.Momentum;

namespace CryptoScanBot.Core.Signal.Experiment;

public class SignalFluxShort : SignalSbmBaseShort
{
    public SignalFluxShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Short;
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
        if (CandleLast.CandleData!.Rsi < GlobalData.Settings.General.RsiValueOverbought)
        {
            ExtraText = $"rsi {CandleLast.CandleData.Rsi} not below {GlobalData.Settings.General.RsiValueOverbought}";
            return false;
        }


        //if (!GetPrevCandle(CandleLast, out CryptoCandle? prevCandle))
        //    return false;

        //// ********************************************************************
        //// Need some sign of recovery (weak)
        //if (CandleLast.CandleData.Rsi > prevCandle!.CandleData!.Rsi)
        //{
        //    ExtraText = $"rsi {CandleLast.CandleData.Rsi} still increasing";
        //    return false;
        //}

        //// ********************************************************************
        //// Need some sign of recovery (weak)
        //if (CandleLast.CandleData.StochSignal > prevCandle.CandleData.StochSignal)
        //{
        //    ExtraText = $"stoch {CandleLast.CandleData.Rsi} still increasing";
        //    return false;
        //}

        // ********************************************************************
        // the distance between ema5 and close needs to be as big as possible (weak)
        // Begins to looks like the SBM method with distances, actually kind of interesting..
        // ==> rubber bands methology & recovery signs
        decimal percentage = 100 * (decimal)CandleLast.CandleData.Ema9! / CandleLast.Close;
        if (percentage < 101m)
        {
            ExtraText = $"distance close and ema9 not ok {percentage:N2}";
            return false;
        }
        percentage -= 100;

        // stupid lux?
        //// ********************************************************************
        //// Flux
        //LuxIndicator.Calculate(Symbol, out int _, out int luxOverBought, CryptoIntervalPeriod.interval5m, CandleLast.OpenTime + Interval.Duration);
        //if (luxOverBought < 100)
        //{
        //    ExtraText = $"flux overbought {luxOverBought} below 100";
        //    return false;
        //}

        ExtraText = $"{percentage:N2}%";
        return true;
    }
}
