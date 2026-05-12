using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Jump;

public class SignalCandleJumpShort : SignalCreateBase
{


    public override bool IsSignal()
    {
        // Een waarde die plotseling ~X% hoger of lager ligt dan de vorige candle

        ExtraText = "";

        // We gaan van rechts naar links
        int candleCount = GlobalData.Settings.Signal.Jump.CandlesLookbackCount;
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
                decimal value = candle!.Candle.GetLowValue(GlobalData.Settings.Signal.Jump.UseLowHighCalculation);
                if (value < minValue)
                {
                    minValue = value;
                    minDate = candle!.Candle.OpenTime;
                }

                value = candle!.Candle.GetHighValue(GlobalData.Settings.Signal.Jump.UseLowHighCalculation);
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
                decimal perc = 100m * (maxValue / minValue - 1);
                if (perc >= GlobalData.Settings.Signal.Jump.CandlePercentage)
                {
                    ExtraText = "-" + perc.ToString("N2") + "%";
                    return true;
                }
            }
        }

        return false;
    }

}