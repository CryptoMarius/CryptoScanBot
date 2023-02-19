using System;

namespace CryptoSbmScanner
{
    public class SignalCandleJumpDown : SignalBase
    {
        public SignalCandleJumpDown(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            SignalMode = SignalMode.modeInfo;
            SignalStrategy = SignalStrategy.strategyCandlesJumpDown;
        }


        public override bool IndicatorsOkay()
        {
            return true;
        }


        public override bool IsSignal()
        {
            ExtraText = "";

            // Een waarde die plotseling ~X% hoger of lager ligt dan de vorige candle
            //if (CandleLast.Open > CandleLast.Close)
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
                decimal value = decimal.MinValue;
                CryptoCandle candle = CandleLast;
                while (candleCount > 0)
                {
                    if (GlobalData.Settings.Signal.JumpUseLowHighCalculation)
                    {
                        value = Math.Max(value, candle.High);
                    }
                    else
                    {
                        value = Math.Max(value, candle.Open);
                        value = Math.Max(value, candle.Close);
                    }

                    // 1 candle verder naar links
                    if (!Candles.TryGetValue(candle.OpenTime - Interval.Duration, out candle))
                        return false;

                    candleCount--;
                }

                //decimal last = Math.Min(CandleLast.Open, CandleLast.Close);
                decimal last = CandleLast.Close;
                if (value > last)
                {
                    decimal perc = Math.Abs(100m * ((last / value) - 1));
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
}