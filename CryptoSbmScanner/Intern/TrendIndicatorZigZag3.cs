using CryptoExchange.Net.CommonObjects;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;

namespace CryptoSbmScanner
{
    // https://ctrader.com/algos/indicators/show/157

    public class TrendIndicatorZigZag3
    {
        private CryptoSymbol Symbol;
        private CryptoInterval Interval;
        private SortedList<long, CryptoCandle> CandleList;

        public TrendIndicatorZigZag3(CryptoSymbol symbol, CryptoInterval interval, SortedList<long, CryptoCandle> candleList)
        {
            this.Symbol = symbol;
            this.Interval = interval;
            this.CandleList = candleList;
        }


        static public List<CryptoCandle> CalculateHistory(SortedList<long, CryptoCandle> candleSticks, int maxCandles)
        {
            //Transporteer de candles naar de Stock list
            //Jammer dat we met tussen-array's moeten werken
            List<CryptoCandle> history = new List<CryptoCandle>();
            Monitor.Enter(candleSticks);
            try
            {
                //Vanwege performance nemen we een gedeelte van de candles
                for (int i = candleSticks.Values.Count - 1; i >= 0; i--)
                {
                    CryptoCandle candle = candleSticks.Values[i];

                    //In omgekeerde volgorde in de lijst zetten
                    if (history.Count == 0)
                        history.Add(candle);
                    else
                        history.Insert(0, candle);

                    maxCandles--;
                    if (maxCandles == 0)
                        break;
                }
            }
            finally
            {
                Monitor.Exit(candleSticks);
            }
            return history;
        }

        public CryptoTrendIndicator Calculate()
        {
            List<CryptoCandle> history = CalculateHistory(CandleList, 21);
            List<EmaResult> emaList8 = (List<EmaResult>)Indicator.GetEma(history, 8);
            List<EmaResult> emaList21 = (List<EmaResult>)Indicator.GetEma(history, 21);

            double ema8 = emaList8[emaList8.Count - 1].Ema.Value;
            double ema21 = emaList21[emaList21.Count - 1].Ema.Value;
            if (ema8 > ema21)
                return CryptoTrendIndicator.trendBullish;
            else if (ema8 < ema21)
                return CryptoTrendIndicator.trendBearish;
            else
                return CryptoTrendIndicator.trendSideways;
        }
    }
}
