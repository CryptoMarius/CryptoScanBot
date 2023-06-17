using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalCandleJumpUp : SignalCreateBase
{
    public SignalCandleJumpUp(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = CryptoTradeDirection.Short;
        SignalStrategy = SignalStrategy.Jump;
    }


    public override bool IsSignal()
    {
        // Een waarde die plotseling ~X% hoger of lager ligt dan de vorige candle

        ExtraText = "";

        // We gaan van rechts naar links
        int candleCount = GlobalData.Settings.Signal.JumpCandlesLookbackCount;
        if (candleCount > 0)
        {
            // Wat is het laagste en hoogste punt in de laatste x candles
            long minDate = CandleLast.OpenTime;
            decimal minValue = decimal.MaxValue;
            long maxDate = CandleLast.OpenTime;
            decimal maxValue = decimal.MinValue;

            CryptoCandle candle = CandleLast;
            while (candleCount > 0)
            {
                decimal value = candle.GetLowest(GlobalData.Settings.Signal.JumpUseLowHighCalculation);
                if (value < minValue)
                {
                    minValue = value;
                    minDate = candle.OpenTime;
                }

                value = candle.GetHighest(GlobalData.Settings.Signal.JumpUseLowHighCalculation);
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
                if (perc >= GlobalData.Settings.Signal.AnalysisCandleJumpPercentage)
                {
                    ExtraText = "+"+ perc.ToString("N2") + "%";
                    return true;
                }
            }
        }

        return false;
    }
}
