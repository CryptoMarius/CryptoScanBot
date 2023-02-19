using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace CryptoSbmScanner
{
    public class CryptoZigZagResult
    {
        public string PointType { get; set; } // indicates a specific point and type e.g. H or L
        public decimal Value { get; set; }
        public CryptoCandle Candle { get; set; }
    }

    public class TrendIndicator
    {
        public List<CryptoZigZagResult> zigZagList = new List<CryptoZigZagResult>();


        private List<CryptoCandle> CalculateHistory(SortedList<long, CryptoCandle> candleSticks, int maxCandles)
        {
            // TODO: Deze routine is gedupliceerd, optimaliseren in de TradeTools of iets dergelijks?

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

                    // In omgekeerde volgorde in de lijst zetten
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


        /// <summary>
        /// ZigZag afkomstig uit de cAlgo wereld
        /// </summary>
        public CryptoTrendIndicator CalculateTrend(CryptoSymbol symbol, CryptoInterval interval)
        {
            CryptoTrendIndicator trend = CryptoTrendIndicator.trendSideways;

            SortedList<long, CryptoCandle> candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;

            // TODO: Uitzoeken hoe veel candles deze zigzag nu eigenlijk nodig heeft. 20.000+ candles lijkt me nogal overdreven
            //List<CryptoCandle> history = candles.Values.ToList(); // Nee, allemaal - CalculateHistory(candles, 600);
            List<CryptoCandle> history = CalculateHistory(candles, 1000); // Toch ingekort
            if (history.Count == 0)
            {
                //Signal.Reaction = string.Format("not enough quotes for {0} trend", interval.Name);
                return trend;
            }

            //GlobalData.AddTextToLogTab("");
            //GlobalData.AddTextToLogTab("");
            //GlobalData.AddTextToLogTab("ZigZagTest2 cAlgo");
            //GlobalData.AddTextToLogTab("");
            TrendIndicatorZigZag zigZagTest2 = new TrendIndicatorZigZag();

            // Naarmate het het interval hoger is moet ook de depth hoger zijn.
            // Eigenlijk is de trend voor lage intervallen niet heel betrouwbaar.
            if (interval.IntervalPeriod >= CryptoIntervalPeriod.interval4h)
                zigZagTest2.Depth = 8;
            else
                zigZagTest2.Depth = 5;

            foreach (CryptoCandle candle in history)
                zigZagTest2.OnProcess(candle, true);

            zigZagList.Clear();


            //GlobalData.AddTextToLogTab("");
            //GlobalData.AddTextToLogTab("ZigZag points:");
            // De lows en highs in 1 lijst zetten voor interpretatie verderop
            // Deze indicator zet de candles net andersom (voila)
            for (int x = zigZagTest2.Candles.Count - 1; x >= 0; x--)
            {
                CryptoCandle candle = zigZagTest2.Candles[x];

                if (zigZagTest2._highBuffer[x] != 0)
                {
                    //candle.CandleData.ZigZag = rsi;
                    //s = string.Format("date={0} H {1:N8} rsi={2:N8}", candle.Date.ToLocalTime(), zigZagTest2._highBuffer[x], candle.CandleData.Rsi.Rsi);
                    //GlobalData.AddTextToLogTab(s);

                    CryptoZigZagResult zigZagResult = new CryptoZigZagResult();
                    zigZagResult.Candle = candle;
                    zigZagResult.PointType = "H";
                    zigZagResult.Value = zigZagTest2._highBuffer[x];
                    zigZagList.Add(zigZagResult);
                }

                if (zigZagTest2._lowBuffer[x] != 0)
                {
                    //s = string.Format("date={0} L {1:N8} rsi={2:N8}", candle.Date.ToLocalTime(), zigZagTest2._lowBuffer[x], candle.CandleData.Rsi.Rsi);
                    //GlobalData.AddTextToLogTab(s);
                    CryptoZigZagResult zigZagResult = new CryptoZigZagResult();
                    zigZagResult.Candle = candle;
                    zigZagResult.PointType = "L";
                    zigZagResult.Value = zigZagTest2._lowBuffer[x];
                    zigZagList.Add(zigZagResult);
                }
            }




            // Niewe bepaling (experiment)
            // NB: Discussie over de laatste waarde (lijkt een extra L/H die niet correct is? actuele marktprijs?)?
            int count = 0;
            decimal? low = null;
            decimal? high = null;
            for (int i = 0; i < zigZagList.Count; i++)
            {
                CryptoZigZagResult zigZagResult = zigZagList[i];

                decimal? value, previous;
                switch (zigZagResult.PointType)
                {
                    // NB: De inner switch code voor de L en H zijn hetzelfde
                    case "H":
                        previous = high;
                        value = zigZagResult.Value;
                        switch (trend)
                        {
                            case CryptoTrendIndicator.trendBearish: // bearish
                                if (value > previous)
                                {
                                    // de nieuwe is hoger dan de vorige, dan kan het geen bearish trend meer zijn
                                    // Het kan eventueel nog een false breakout zijn (houden we nu geen rekening mee)
                                    trend = CryptoTrendIndicator.trendBullish;
                                    count = 1;
                                }
                                else
                                    count++;
                                break;
                            case CryptoTrendIndicator.trendSideways: // er is nog geen trend gedetecteerd (c.q. sideway's)
                                if ((previous != null) && (value > previous))
                                {
                                    // de nieuwe is hoger dan de vorige, laten we aannemen dat het bullish wordt
                                    trend = CryptoTrendIndicator.trendBullish;
                                    count = 1;
                                }
                                else if ((previous != null) && (value < previous))
                                {
                                    // de nieuwe is lager dan de vorige, laten we aannemen dat het bearish wordt
                                    trend = CryptoTrendIndicator.trendBearish;
                                    count = 1;
                                }
                                break;
                            case CryptoTrendIndicator.trendBullish: // bullish
                                if (value <= previous)
                                {
                                    // de nieuwe is lager dan de vorige, dan kan het geen bullish trend meer zijn
                                    trend = CryptoTrendIndicator.trendBearish;
                                    count = 1;
                                }
                                else
                                    count++;
                                break;

                        }
                        high = value;
                        break;
                    case "L":
                        previous = low;
                        value = zigZagResult.Value;
                        switch (trend)
                        {
                            case CryptoTrendIndicator.trendBearish: // bearish
                                if (value > previous)
                                {
                                    // de nieuwe is hoger dan de vorige, dan kan het geen bearish trend meer zijn
                                    trend = CryptoTrendIndicator.trendBullish;
                                    count = 1;
                                }
                                else
                                    count++;
                                break;
                            case CryptoTrendIndicator.trendSideways: // er is nog geen trend gedetecteerd (c.q. sideway's)
                                if ((previous != null) && (value > previous))
                                {
                                    // de nieuwe is hoger dan de vorige, laten we aannemen dat het bullish wordt
                                    trend = CryptoTrendIndicator.trendBullish;
                                    count = 1;
                                }
                                else if ((previous != null) && (value < previous))
                                {
                                    // de nieuwe is lager dan de vorige, laten we aannemen dat het bearish wordt
                                    trend = CryptoTrendIndicator.trendBearish;
                                    count = 1;
                                }
                                break;
                            case CryptoTrendIndicator.trendBullish: // bullish
                                if (value <= previous)
                                {
                                    // de nieuwe is lager dan de vorige, dan kan het geen bullish trend meer zijn
                                    trend = CryptoTrendIndicator.trendBearish;
                                    count = 1;
                                }
                                else
                                    count++;
                                break;

                        }
                        low = value;
                        break;
                }

            }


            if (count < 3)
                trend = CryptoTrendIndicator.trendSideways; // trendBearish;

            // De zigzag heeft soms problemen met de laatste piek of dal, daarom het volgende ter correctie:
            // Als de allerlaatste candle alweer onder of boven de l/h staat is de trend niet meer geldig
            CryptoCandle candleLast = history.Last();
            if (trend == CryptoTrendIndicator.trendBullish)
            {
                if (candleLast.Close < low)
                    trend = CryptoTrendIndicator.trendSideways; // trendBearish;
            }
            if (trend == CryptoTrendIndicator.trendBearish)
            {
                if (candleLast.Close > high)
                    trend = CryptoTrendIndicator.trendSideways; // trendBullish;
            }



            //GlobalData.AddTextToLogTab("");
            //GlobalData.AddTextToLogTab("Trend(2):");
            //string s = "";
            //if (trend == CryptoTrend.trendBullish)
            //    s = string.Format("{0} {1}, candles={2}, count={3}, trend=bullish, ", symbol.Name, interval.IntervalPeriod, candles.Count, count);
            //else if (trend == CryptoTrend.trendBearish)
            //    s = string.Format("{0} {1}, candles={2}, count={3}, trend=bearish, ", symbol.Name, interval.IntervalPeriod, candles.Count, count);
            //else
            //    s = string.Format("{0} {1}, candles={2}, count={3}, trend=sideway's?, ", symbol.Name, interval.IntervalPeriod, candles.Count, count);
            //GlobalData.AddTextToLogTab(s);

            //if (trend == CryptoTrend.trendBullish)
            //    procentje += (int)interval.IntervalPeriod * y;
            //else if (trend == CryptoTrend.trendBearish)
            //    procentje -= (int)interval.IntervalPeriod * y;
            return trend;
        }


        //decimal xyz = 100 * (decimal)procentje / sum;
        //GlobalData.AddTextToLogTab(xyz.ToString("N2"));
    }
}
