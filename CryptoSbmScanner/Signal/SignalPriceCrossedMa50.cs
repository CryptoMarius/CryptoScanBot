using System;

namespace CryptoSbmScanner
{
    public class SignalPriceCrossedMa50 : SignalBase
    {
        public SignalPriceCrossedMa50(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            SignalMode = SignalMode.modeLong;
            SignalStrategy = SignalStrategy.strategyPriceCrossedMa50;
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


        public override bool IndicatorsOkay()
        {
            if (!IndicatorCandleOkay(CandleLast))
                return false;
            return true;
        }


        public override bool IsSignal()
        {
            ExtraText = "";

            // De breedte van de bb is ten minste 1.5%
            if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.AnalysisBBMinPercentage, GlobalData.Settings.Signal.AnalysisBBMaxPercentage))
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
            if (prevCandle.Close > (decimal)prevCandle.CandleData.Sma50.Sma.Value)
                return false;

            if (CandleLast.Close < (decimal)CandleLast.CandleData.Sma50.Sma.Value)
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
            return false;
        }


    }
}
