using System;
using System.Linq;

namespace CryptoSbmScanner
{
    public class SignalStobbOversold : SignalBase
    {
        public SignalStobbOversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
        {
            ReplaceSignal = true;
            SignalMode = SignalMode.modeLong;
            SignalStrategy = SignalStrategy.strategyStobbOversold;
        }


        private bool IndicatorCandleOkay(CryptoCandle candle)
        {
            if ((candle == null)
               || (candle.CandleData.Stoch == null)
               || (candle.CandleData.Stoch.Signal == null)
               || (candle.CandleData.Stoch.Oscillator == null)
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


        public override string DisplayText()
        {
            return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
                CandleLast.CandleData.Stoch.Oscillator.Value,
                CandleLast.CandleData.Stoch.Signal.Value
            );
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

            // Er een candle onder de bb opent of sluit
            if (!CandleLast.IsBelowBollingerBands())
            {
                ExtraText = "niet beneden de bb";
                return false;
            }

            // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
            if (!CandleLast.IsStochOversold())
            {
                ExtraText = "stoch niet oversold";
                return false;
            }


            // Platte candles of candles zonder volume
            // EXTRA (anders krijg je toch vreemde momenten met platte candles)
            //CryptoSignal Signal = new CryptoSignal();
            //SignalCreate.CalculateAdditionalAlarmProperties(Signal, Candles.Values.ToList(), 30, CandleLast.OpenTime);
            //if (GlobalData.Settings.Signal.CandlesWithZeroVolume > 0 && Signal.CandlesWithZeroVolume >= GlobalData.Settings.Signal.CandlesWithZeroVolume)
            //    return false;
            //if (GlobalData.Settings.Signal.CandlesWithFlatPrice > 0 && Signal.CandlesWithFlatPrice >= GlobalData.Settings.Signal.CandlesWithFlatPrice)
            //    return false;
            //if (GlobalData.Settings.Signal.AboveBollingerBandsSma > 0 && Signal.AboveBollingerBandsSma < GlobalData.Settings.Signal.AboveBollingerBandsSma)
            //    return false;
            //if (GlobalData.Settings.Signal.AboveBollingerBandsUpper > 0 && Signal.AboveBollingerBandsUpper < GlobalData.Settings.Signal.AboveBollingerBandsUpper)
            //    return false;


            if (GlobalData.Settings.Signal.StobIncludeRsi && !CandleLast.IsRsiOversold())
            {
                ExtraText = "rsi niet oversold";
                return false;
            }

            if (GlobalData.Settings.Signal.StobIncludeSoftSbm && !CandleLast.IsSbmConditionsOversold(false))
            {
                ExtraText = "geen soft sbm condities";
                return false;
            }

            return true;
        }


        public override bool AllowStepIn(CryptoSignal signal)
        {

            if (CandleLast.CandleData.Stoch.Oscillator.Value < CandleLast.CandleData.Stoch.Signal.Value)
            {
                ExtraText = "Stoch Oscillator onder stoch signal";
                return false;
            }


            // Breedte bb in 1.5% t/m 5.0%
            if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.AnalysisBBMinPercentage, GlobalData.Settings.Signal.AnalysisBBMaxPercentage))
            {
                ExtraText = "De BB is te smal of te breed";
                return false;
            }


            // Er een candle onder de bb opent of sluit
            if (CandleLast.IsBelowBollingerBands())
            {
                ExtraText = "prijs beneden bb";
                return false;
            }

            // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
            if (CandleLast.IsStochOversold())
            {
                ExtraText = "stoch nog steeds oversold";
                return false;
            }



            // Onderstaand komt uit de monitor thread (oid)
            // Het is geheel afgestemd op de oude STOB (hardcoded)


            // Stochastic: Omdat ik ze wel eens door elkaar haal:
            // %K = Oscilator berekend over een lookback periode van 14 candles
            // %D = signal, het gemiddelde van de laatste 3 %K waarden

            // Oscillator  %K Oscillator over prior N lookback periods (de snelle "gele" lijn)
            // Signal  %D Simple moving average of Oscillator (de langzame "rode" lijn)


            // De %D en %k moeten elkaar gekruist hebben. Dus %K(snel) > %D(traag), geeft goede instapmomenten (voor zover ik dat gecontroleerd heb)
            if ((decimal)CandleLast.CandleData.Stoch.Oscillator.Value <= (decimal)CandleLast.CandleData.Stoch.Signal.Value)
            {
                string text = "Monitor " + Symbol.Name + " " + signal.Interval.Name + " signal from=" + signal.OpenDate.ToLocalTime() + " price=" + Symbol.LastPrice;
                ExtraText = text + " below(k < d) " + CandleLast.CandleData.Stoch.Oscillator.Value.ToString("N5") + "   " + CandleLast.CandleData.Stoch.Signal.Value.ToString("N5");
                return false;
            }

            // %K Oscillator (geel, de "snelle"), was 14, maar ik stap dan te vroeg in?
            //decimal value = GlobalData.Settings.Bot.StepInStochValue;
            //if ((decimal)CandleLast.CandleData.Stoch.Oscillator.Value < value)
            //{
            //    string text = "Monitor " + Symbol.Name + " " + signal.Interval.Name + " signal from=" + signal.OpenDate.ToLocalTime() + " price=" + Symbol.LastPrice;
            //    ExtraText = string.Format("{0}  stoch to low ({1} < {2})", text, CandleLast.CandleData.Stoch.Oscillator.Value.ToString("N5"), value);
            //    return false;
            //}

            ExtraText = "Alles lijkt goed";
            return true;
        }



        public override bool GiveUp(CryptoSignal signal)
        {
            if (CandleLast.CandleData.Stoch.Oscillator.Value >= 80)
            {
                ExtraText = "give up(stoch.osc > 80)";
                //AppendLine(string.Format("{0} give up (stoch.osc > 80) {1}", text, dcaPrice.ToString0());
                return true;
            }


            if (Math.Min(CandleLast.Open, CandleLast.Close) >= (decimal)CandleLast.CandleData.BollingerBands.Sma.Value)
            {
                //reason = string.Format("{0} give up (pricewise.body > bb) {1}", text, dcaPrice.ToString0());
                ExtraText = "give up (pricewise.body > bb)";
                return true;
            }


            ////// Geen negatieve barometer

            //// Even een quick fix voor de barometer
            //CryptoQuoteData quoteData;
            //if (!GlobalData.Settings.QuoteCoins.TryGetValue(signal.Symbol.Quote, out quoteData))
            //{
            //    ExtraText = "Barometers not yet calculated";
            //    return true;
            //}

            //// We gaan ervan uit dat alles in 1x wordt berekend
            //BarometerData barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
            //if (!barometerData.PriceBarometer.HasValue)
            //{
            //    ExtraText = "Barometers not yet calculated";
            //    return true;
            //}


            //barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
            //if (barometerData.PriceBarometer <= GlobalData.Settings.Bot.Barometer01hBotMinimal)
            //{
            //    ExtraText = string.Format("Barometer15m {0} below {1}", barometerData.PriceBarometer?.ToString0("N2"), GlobalData.Settings.Bot.Barometer01hBotMinimal.ToString0("N2"));
            //    return true;
            //}



            // Langer dan 60 candles willen we niet wachten
            if ((CandleLast.OpenTime - signal.EventTime) / Interval.Duration > 20)
            {
                ExtraText = "Ophouden na 20 candles";
                return true;
            }


            ExtraText = "";
            return false;
        }


    }
}
