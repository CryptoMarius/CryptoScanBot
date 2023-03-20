using System.Collections.Generic;

namespace CryptoSbmScanner
{

    // Het draait allemaal om de status van het algoritme
    // (het algoritme zet die status zelf alsmede delay enz.):
    // -None, candle aanbieden voor signaal detectie
    // -WarmingUp (voor de indicators)
    // -Delaying: Een (optionele) delay
    // -TryStepIn: Na een OK van het algoritme om in te stappen

    public class SignalBase
    {
        protected CryptoExchange Exchange;
        protected CryptoSymbol Symbol;
        protected CryptoSymbolInterval SymbolInterval;
        protected CryptoInterval Interval;
        protected CryptoQuoteData QuoteData;
        protected SortedList<long, CryptoCandle> Candles;

        public SignalMode SignalMode;
        public SignalStrategy SignalStrategy;
        public CryptoCandle CandleLast = null;
        public string ExtraText = "";
        public bool ReplaceSignal = false;

        public SignalBase(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) 
        {
            this.Symbol = symbol;
            this.Exchange = symbol.Exchange;
            this.Interval = interval;
            this.QuoteData = symbol.QuoteData;
            this.CandleLast = candle;

            this.SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
            this.Candles = SymbolInterval.CandleList;
        }

        /// <summary>
        /// Zijn de indicatoren aanwezig
        /// </summary>
        public virtual bool IndicatorsOkay(CryptoCandle candle)
        {
            return true;
        }


        /// <summary>
        /// Is het een signaal?
        /// </summary>
        public virtual bool IsSignal()
        {
            return false;
        }

        public virtual string DisplayText()
        {
            string stos = string.Format("stoch={0:N2} signal={1:N2}", CandleLast.CandleData.Stoch.Oscillator.Value, CandleLast.CandleData.Stoch.Signal.Value);
            return stos;
        }

        
        /// <summary>
        /// Ophouden met positie nemen
        /// </summary>
        public virtual bool GiveUp(CryptoSignal signal)
        {
            ExtraText = "";
            return false;
        }


        /// <summary>
        /// Extra controles nadat we het accepteren
        /// </summary>
        public virtual bool AllowStepIn(CryptoSignal signal)
        {
            return false;
        }

        /// <summary>
        /// Is de RSI oversold geweest in de laatste x candles
        /// </summary>
        public bool wasRsiOversoldInTheLast(int candleCount = 30)
        {
            // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
            long time = CandleLast.OpenTime;
            while (candleCount >= 0)
            {
                CryptoCandle candle;
                if (Candles.TryGetValue(time, out candle))
                {
                    if (IndicatorsOkay(candle) && candle.IsRsiOversold())
                       return true;
                }
                candleCount--;
                time -= Interval.Duration;
            }
            return false;
        }


    }
}
