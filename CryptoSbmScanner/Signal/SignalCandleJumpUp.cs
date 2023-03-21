using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalCandleJumpUp : SignalBase
{
    public SignalCandleJumpUp(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeInfo;
        SignalStrategy = SignalStrategy.strategyCandlesJumpUp;
    }


    public override bool IsSignal()
    {
        ExtraText = "";


        // Een waarde die plotseling ~X% hoger of lager ligt dan de vorige candle


        //if (CandleLast.Close > CandleLast.Open)
        //{
        //    decimal perc = Math.Abs(100m * ((CandleLast.Close / CandleLast.Open) - 1));
        //    if (perc >= GlobalData.Settings.Signal.AnalysisCandleJumpPercentage)
        //    {
        //        ExtraText = perc.ToString("N2") + "%";
        //        return true;
        //    }
        //}

        // We gaan van rechts naar links
        int candleCount = GlobalData.Settings.Signal.JumpCandlesLookbackCount;
        if (candleCount > 0)
        {
            decimal value = decimal.MaxValue;
            CryptoCandle candle = CandleLast;
            while (candleCount > 0)
            {
                if (GlobalData.Settings.Signal.JumpUseLowHighCalculation)
                {
                    value = Math.Min(value, candle.Low);
                }
                else
                {
                    value = Math.Min(value, candle.Open);
                    value = Math.Min(value, candle.Close);
                }

                // 1 candle verder naar links
                if (!Candles.TryGetValue(candle.OpenTime - Interval.Duration, out candle))
                    return false;

                candleCount--;
            }

            decimal last = CandleLast.Close;
            //decimal last = Math.Max(CandleLast.Open, CandleLast.Close);
            if (value < last)
            {
                decimal perc = Math.Abs(100m * (last / value - 1));
                if (perc >= GlobalData.Settings.Signal.AnalysisCandleJumpPercentage)
                {
                    ExtraText = perc.ToString("N2") + "%";
                    return true;
                }
            }
        }
        return false;
    }

}
