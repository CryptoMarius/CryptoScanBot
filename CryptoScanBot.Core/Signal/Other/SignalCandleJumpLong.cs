﻿using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Other;

public class SignalCandleJumpLong : SignalCreateBase
{
    public SignalCandleJumpLong(CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(account, symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Jump;
    }


    public override bool IsSignal()
    {
        // Een waarde die plotseling ~X% hoger of lager ligt dan de vorige candle

        ExtraText = "";

        // We gaan van rechts naar links
        int candleCount = GlobalData.Settings.Signal.Jump.CandlesLookbackCount;
        if (candleCount > 0)
        {
            // Wat is het laagste en hoogste punt in de laatste x candles
            long minDate = CandleLast.OpenTime;
            decimal minValue = decimal.MaxValue;
            long maxDate = CandleLast.OpenTime;
            decimal maxValue = decimal.MinValue;

            CryptoCandle? candle = CandleLast;
            while (candleCount > 0)
            {
                decimal value = candle.GetLowValue(GlobalData.Settings.Signal.Jump.UseLowHighCalculation);
                if (value < minValue)
                {
                    minValue = value;
                    minDate = candle.OpenTime;
                }

                value = candle.GetHighValue(GlobalData.Settings.Signal.Jump.UseLowHighCalculation);
                if (value > maxValue)
                {
                    maxValue = value;
                    maxDate = candle.OpenTime;
                }

                // 1 candle verder naar links
                if (!Candles.TryGetValue(candle.OpenTime - Interval.Duration, out candle))
                    return false;

                candleCount--;
            }


            // Is het gestegen ? (maar pas op, het kan alweer gedaald zijn)
            if (minDate < maxDate)
            {
                decimal perc = 100m * (maxValue / minValue - 1);
                if (perc >= GlobalData.Settings.Signal.Jump.CandlePercentage)
                {
                    ExtraText = "+" + perc.ToString("N2") + "%";
                    return true;
                }
            }
        }

        return false;
    }

}
