using System;

namespace CryptoSbmScanner
{
    public class SignalPriceCrossedMa20 : SignalBase
    {
        public SignalPriceCrossedMa20(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            SignalMode = SignalMode.modeLong;
            SignalStrategy = SignalStrategy.strategyPriceCrossedMa20;
        }

        private bool IndicatorCandleOkay(CryptoCandle candle)
        {
            if ((candle == null)
               || (candle.CandleData.Rsi == null) || (!candle.CandleData.Rsi.Rsi.HasValue)
               || (candle.CandleData.Sma50.Sma == null) || (!candle.CandleData.Sma50.Sma.HasValue)
               || (candle.CandleData.Sma200.Sma == null) || (!candle.CandleData.Sma200.Sma.HasValue)
               || (candle.CandleData.BollingerBands == null) || (!candle.CandleData.BollingerBands.Sma.HasValue)
               )
                return false;

            return true;
        }


        public override bool IsSignal()
        {
            ExtraText = "";

            // De breedte van de bb is ten minste 1.5%
            if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
            {
                ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage.ToString("N2");
                return false;
            }

            CryptoCandle prevCandle;
            if (!Candles.TryGetValue(CandleLast.OpenTime - 1 * Interval.Duration, out prevCandle))
            {
                ExtraText = "geen prev candle! " + CandleLast.DateLocal.ToString();
                return false;
            }

            if (CandleLast.CandleData.Rsi.Rsi.Value <= 50)
                return false;

            if (CandleLast.CandleData.Stoch.Oscillator.Value <= 50)
                return false;

            // Kruising van de candle
            if (prevCandle.Close > (decimal)prevCandle.CandleData.BollingerBands.Sma.Value)
                return false;

            if (CandleLast.Close < (decimal)CandleLast.CandleData.BollingerBands.Sma.Value)
                return false;

            if (CandleLast.CandleData.Sma50.Sma.Value > CandleLast.CandleData.Sma200.Sma.Value)
                return false;


            decimal tickPercentage = 100m * Symbol.PriceTickSize / (decimal)Symbol.LastPrice;
            if (tickPercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
                return false;

            return true;
        }


        public override bool AllowStepIn(CryptoSignal signal)
        {
            return true;
        }


        public override bool GiveUp(CryptoSignal signal)
        {
            ExtraText = "";

            // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
            //if ((CandleLast.OpenTime - signal.EventTime) / Interval.Duration > GlobalData.Settings.Bot.SignalRemoveAfterCandles)
            //{
            //    ExtraText = "Ophouden na " + GlobalData.Settings.Bot.SignalRemoveAfterCandles.ToString() + " candles";
            //    return true;
            //}


            CryptoCandle CandlePrev1;
            if (!Candles.TryGetValue(CandleLast.OpenTime - 1 * Interval.Duration, out CandlePrev1))
            {
                ExtraText = "No prev1";
                return true;
            }

            // Als het alsnog negatief wordt (als het nog in de queue zit & limiet op aantal slots) dan stoppen
            //if ((CandlePrev1.CandleData.Slope20 >= 0) && (CandleLast.CandleData.Slope20 < 0))
            //{
            //    ExtraText = string.Format("SMA20.Slope20(-1) {0:N8}", CandleLast.CandleData.Slope20);
            //    return true;
            //}


            return false;
        }


    }
}
