using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection.Metadata;

namespace CryptoSbmScanner
{
    public class CandleIndicatorData
    {
        //public EmaResult Ema8 { get; set; }
        //public EmaResult Ema21 { get; set; }
        // Simple Moving Average
        public SmaResult Sma50 { get; set; }
        //public SmaResult Sma100 { get; set; }
        public SmaResult Sma200  { get; set; }
        public RsiResult Rsi { get; set; }
        public MacdResult Macd { get; set; }
        public StochResult Stoch { get; set; }
        public double PSar { get; set; }
#if DEBUG
        public float PSarDave { get; set; }
        public float PSarJason { get; set; }
        public float PSarTulip { get; set; }   
#endif
        public BollingerBandsResult BollingerBands { get; set; }
        public float BollingerBandsPercentage { get; set; }

        // 200 voor SMA200, //135 voor de MACD 114; // 64; //30 is genoeg voor de laatste waarde in de StochRSI (28) en wat voor wat extra's 
        // (Dave heeft het verhoogd naar 64, later voor de RSI nog eens naar de 114, en voor de Macd naar 135, SMA naar 200, balen!!)
        // Voor de SMA lookback willen we 60 sma200's erin, dus 200 + 60 !!
        private const int maxCandles = 260;


        /// <summary>
        /// Make a list of candles up to nextCandleOpenTime with at least 260 candles.
        /// (target: ma200 for the last 60 minutes, but also the other indicators)
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="interval"></param>
        /// <param name="nextCandleOpenTime"></param>
        /// <param name="errorstr"></param>
        /// <returns></returns>
        static public List<CryptoCandle> CalculateCandles(CryptoSymbol symbol, CryptoInterval interval, long nextCandleOpenTime, out string errorstr)
        {
            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> intervalCandles = symbolPeriod.CandleList;
            if (intervalCandles.Count < maxCandles)
            {
                errorstr = string.Format("{0} Not enough candles available for interval {1} count={2} requested={3}", symbol.Name, interval.Name, intervalCandles.Count, maxCandles);
                return null;
            }

            CryptoCandle firstCandle = intervalCandles.Values.First();
            long firstTime = firstCandle.OpenTime;


            SortedList<long, CryptoCandle> candlesForHistory = new SortedList<long, CryptoCandle>();

            // Een kleine selectie van candles overnemen.
            // Mijn vermoeden is we de 1e keer geen candle hebben (omdat we de tijd van de volgende candle krijgen)
            // MAAR, vanuit een backtest is deze candle er wel, dus hier moet ik waarschijnlijk iets aan doen....
            // Probleem, als er niet genoeg candles zijn krijgen we een endless loop! ;-)
            long candleLoop = nextCandleOpenTime - (nextCandleOpenTime % interval.Duration); //Geen verandering als het goed is
            //DateTime candleLoopDebug = CandleTools.GetUnixDate(candleLoop); //debug
            while (candlesForHistory.Count < maxCandles)
            {
                CryptoCandle candle;
                if (intervalCandles.TryGetValue(candleLoop, out candle))
                    candlesForHistory.Add(candle.OpenTime, candle);
                else
                {
                    // De laatste candle is niet altijd aanwezig (een kwestie van timing?)
                    if (nextCandleOpenTime != candleLoop) 
                    {
                        // In de hoop dat dit het automatisch zou kunnen fixen?
                        //symbolPeriod.CandleFetched.IsChanged = true;
                        if (symbolPeriod.LastCandleSynchronized.Value > candleLoop - interval.Duration)
                            symbolPeriod.LastCandleSynchronized = candleLoop - interval.Duration;
                        GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Missing candle information " + CandleTools.GetUnixDate(candleLoop).ToLocalTime());
                        GlobalData.ConnectionWasRestored(""); // A quick fix (dont like it)?
                    }
                }
                candleLoop -= interval.Duration;
                //candleLoopDebug = CandleTools.GetUnixDate(candleLoop);

                if (candleLoop < firstTime)
                    break;
            }

            //Mhh, blijkbaar waren er gewoon niet goed candles
            if (candlesForHistory.Count < maxCandles)
            {
                errorstr = string.Format("{0} Not enough candles available for interval {1} count={2} requested={3}", symbol.Name, interval.Name, candlesForHistory.Count, maxCandles);
                return null;
            }

            // Fill in missing candles (repeating the last candle.close) up to nextCandleOpenTime
            // We assume nothing has happened in that period (flat candles with no orders)
            CandleTools.AddMissingSticks(candlesForHistory, nextCandleOpenTime, interval);


            // Convert the list to a input kind the stupid indicators are using
            errorstr = "";
            List<CryptoCandle> history = candlesForHistory.Values.ToList();
            return history;
        }



        //private void DumpCandles()
        //{
        //    GlobalData.AddTextToLogTab(Symbol.Name + " error data?");
        //    try
        //    {
        //        foreach (CryptoCandle quotte in Candles.Values)
        //        {
        //            GlobalData.AddTextToLogTab(
        //                Symbol.Name + " " +
        //                quotte.DateLocal.ToString() + ", " +
        //                quotte.Open.ToString() + ", " +
        //                quotte.High.ToString() + ", " +
        //                quotte.Low.ToString() + ", " +
        //                quotte.Close.ToString() + " " +
        //                "sma200=" + quotte.Low.ToString() + ", " +

        //                );
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        GlobalData.Logger.Error(error);
        //    }
        //}


        /// <summary>
        /// Calculate all the indicators we want to have an fill in the last 60 candles
        /// </summary>
        public static void CalculateIndicators(List<CryptoCandle> history)
        {
            // Overslaan indien het gevuld is (meerdere aanroepen)
            CryptoCandle candle = history.Last();
            if (candle.CandleData != null)
                return;
            //List<EmaResult> emaList8 = (List<EmaResult>)Indicator.GetEma(history, 8);
            //List<EmaResult> emaList21 = (List<EmaResult>)Indicator.GetEma(history, 21);

            List<SmaResult> smaList50 = (List<SmaResult>)Indicator.GetSma(history, 50);
            //List<SmaResult> smaList100 = (List<SmaResult>)Indicator.GetSma(history, 100);
            List<SmaResult> smaList200 = (List<SmaResult>)Indicator.GetSma(history, 200);

            List<RsiResult> rsiList = (List<RsiResult>)Indicator.GetRsi(history);
            List<MacdResult> macdList = (List<MacdResult>)Indicator.GetMacd(history);

            // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
            List<StochResult> stochList = (List<StochResult>)Indicator.GetStoch(history, 14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...
#if DEBUG
            List<ParabolicSarResult> psarListDave = (List<ParabolicSarResult>)Indicator.GetParabolicSar(history);
            List<ParabolicSarResult> psarListJason = (List<ParabolicSarResult>)Indicators.CalcParabolicSarJasonLam(history);
            List<ParabolicSarResult> psarListTulip = (List<ParabolicSarResult>)Indicators.CalcParabolicSarTulip(history);
            List<SmaResult> smaList20 = (List<SmaResult>)Indicator.GetSma(history, 20);
#endif
            List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)Indicator.GetBollingerBands(history);

            // Because Skender.Psar has different results  we use the old ta-lib (I dont like that)
            var inLow = history.Select(x => Convert.ToDouble(x.Low)).ToArray();
            var inHigh = history.Select(x => Convert.ToDouble(x.High)).ToArray();
            //var inOpen = history.Select(x => Convert.ToDouble(x.Open)).ToArray();
            //var inClose = history.Select(x => Convert.ToDouble(x.Close)).ToArray();

            int startIdx = 0;
            int endIdx = history.Count - 1;
            int outNbElement;
            TicTacTec.TA.Library.Core.RetCode retCode;

            int outBegIdxPSar;
            double[] pSarOutput = new double[history.Count];
            retCode = TicTacTec.TA.Library.Core.Sar(startIdx, endIdx, inHigh, inLow, 0.02, 0.2, 
                out outBegIdxPSar, out outNbElement, pSarOutput);

            // We might do everything via ta-lib, but its a tricky library (only use it for psar)
            //int outBegIdxBb;
            //double[] bboutputUpper = new double[history.Count];
            //double[] bboutputMiddle = new double[history.Count];
            //double[] bboutputLower = new double[history.Count];
            //retCode = TicTacTec.TA.Library.Core.Bbands(startIdx, endIdx, inClose, 20, 2.0, 2.0, TicTacTec.TA.Library.Core.MAType.Sma,
            //    out outBegIdxBb, out outNbElement, bboutputUpper, bboutputMiddle, bboutputLower);
            //output will be SMA values


            // Fill the last 60 candles with the indicator data
            int iteration = 0;
            for (int index = history.Count - 1; index >= 0; index--)
            {
                // Maximaal 60 records aanvullen
                iteration++;
                if (iteration > 61)
                    break;


                candle = history[index];

                CandleIndicatorData candleData = new CandleIndicatorData();
                candle.CandleData = candleData;
                try
                {
                    candleData.Sma50 = smaList50[index];
                    candleData.Sma200 = smaList200[index];
                    candleData.Rsi = rsiList[index];
                    candleData.Macd = macdList[index];
                    candleData.Stoch = stochList[index];
                    candleData.BollingerBands = bollingerBandsList[index];
                    candleData.BollingerBandsPercentage = 100 * (float)((candleData.BollingerBands.UpperBand / candleData.BollingerBands.LowerBand) - 1);

                    if (index >= outBegIdxPSar)
                        candleData.PSar = pSarOutput[index - outBegIdxPSar];
#if DEBUG
                    if (psarListDave[index].Sar != null)
                        candleData.PSarDave = (float)psarListDave[index].Sar.Value;
                    candleData.PSarJason = (float)psarListJason[index].Sar;
                    candleData.PSarTulip = (float)psarListTulip[index].Sar;
#endif

                }
                catch (Exception error)
                {
                    // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                    GlobalData.Logger.Error(error);
                    GlobalData.AddTextToLogTab("");
                    GlobalData.AddTextToLogTab("error indicators");
                    GlobalData.AddTextToLogTab(error.ToString());
                    GlobalData.AddTextToLogTab("");
                    GlobalData.AddTextToLogTab(history.ToString());
                    throw;
                }

            }
        }

        // Extended with 1 day + 9 hours because of the 24 hour market climate (or barometer).  (we show ~6 hours of that in the display)
        public static long InitialCandleCountFetch = (24 * 60 * 60) + (9 * 60 * 60);

        public static long GetCandleFetchStart(CryptoSymbol symbol, CryptoInterval interval, DateTime utcNow)
        {
            long startFetchUnix;

            // Since the market climate is also a coin we must make an exception, it needs more candles because of the 24h bm calculation
            if (symbol.IsBarometerSymbol())
            {
                startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - (1440 * interval.Duration);
            }
            else
            {
                // Bepaal de start datum (we willen ook niet "teveel" data uit de database of bestanden ophalen)
                // Voor de 1m tenminste 2 dagen en anders tenminste 215 candles zodat de indicators het doen
                // (die 2 dagen is nodig voor de (volume) berekening van de 24h barometer, de 215 vanwege de indicators)
                // Als we de volume berekening achterwege laten dan zijn het er 24*60=1440
                if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                    startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - InitialCandleCountFetch;
                else
                    // de 0 was eerst een 10 en bedoeld om meldingen met terugwerkende kracht te berekenen bij de start
                    startFetchUnix = CandleTools.GetUnixTime(utcNow, 60) - ((0 + maxCandles) * interval.Duration); 
                startFetchUnix = startFetchUnix - (startFetchUnix % interval.Duration);

                // Lets extend that with 1 extra candle just in case...
                startFetchUnix -= interval.Duration;
            }
            DateTime symbolfetchCandleDebug = CandleTools.GetUnixDate(startFetchUnix);  //debug
            return startFetchUnix;

        }

    }
}
