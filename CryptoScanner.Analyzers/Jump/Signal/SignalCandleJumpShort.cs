using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Jump.Signal;

public class SignalCandleJumpShort : SignalCreateBase
{
    public override bool IsSignal()
    {
        // Een waarde die plotseling ~X% hoger of lager ligt dan de vorige candle

        ExtraText = "";
        var settings = JumpPlugin.Settings;

        // We gaan van rechts naar links
        int candleCount = settings.CandlesLookbackCount;
        if (candleCount > 0)
        {
            // Wat is het laagste en hoogste punt in de laatste x candles
            CandleTime minDate = CandleLast!.Candle.OpenTime;
            decimal minValue = decimal.MaxValue;
            CandleTime maxDate = CandleLast.Candle.OpenTime;
            decimal maxValue = decimal.MinValue;

            MyData? candle = CandleLast;
            while (candleCount > 0)
            {
                decimal value = candle!.Candle.GetLowValue(settings.UseLowHighCalculation);
                if (value < minValue)
                {
                    minValue = value;
                    minDate = candle!.Candle.OpenTime;
                }

                value = candle!.Candle.GetHighValue(settings.UseLowHighCalculation);
                if (value > maxValue)
                {
                    maxValue = value;
                    maxDate = candle!.Candle.OpenTime;
                }

                if (!GetPrevCandle(candle, out candle))
                    return false;

                candleCount--;
            }


            // Is het gedaald? (maar pas op, het kan alweer gestegen zijn)
            if (minDate > maxDate)
            {
                // A drop is measured from its starting point, which is the max (the min came later),
                // so 4% means the same thing on the long side and on the short side. Using
                // maxValue / minValue - 1 would be the rise back from the min and is the larger number,
                // which made the short fire below the configured percentage.
                decimal perc = 100m * (1 - minValue / maxValue);
                if (perc >= settings.CandlePercentage)
                {
                    ExtraText = "-" + perc.ToString("N2") + "%";
                    return true;
                }
            }
        }

        return false;
    }

}