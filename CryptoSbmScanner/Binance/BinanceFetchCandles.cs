using Binance.Net;
using Binance.Net.Enums;

using System;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using System.Threading.Tasks;
using CryptoExchange.Net.Objects;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Interfaces;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;

namespace CryptoSbmScanner
{
    /// <summary>
    /// Fetch the candles from Binance
    /// </summary>
    public class BinanceFetchCandles
    {
        // Prevent multiple sessions
        static private SemaphoreSlim Semaphore = new SemaphoreSlim(1);


        private KlineInterval GetBinanceInterval(CryptoInterval interval)
        {
            KlineInterval binanceInterval;
            switch (interval.IntervalPeriod)
            {
                case CryptoIntervalPeriod.interval1m:
                    binanceInterval = KlineInterval.OneMinute;
                    break;
                case CryptoIntervalPeriod.interval5m:
                    binanceInterval = KlineInterval.FiveMinutes;
                    break;
                case CryptoIntervalPeriod.interval15m:
                    binanceInterval = KlineInterval.FifteenMinutes;
                    break;
                case CryptoIntervalPeriod.interval30m:
                    binanceInterval = KlineInterval.ThirtyMinutes;
                    break;
                case CryptoIntervalPeriod.interval1h:
                    binanceInterval = KlineInterval.OneHour;
                    break;
                case CryptoIntervalPeriod.interval2h:
                    binanceInterval = KlineInterval.TwoHour;
                    break;
                case CryptoIntervalPeriod.interval4h:
                    binanceInterval = KlineInterval.FourHour;
                    break;
                case CryptoIntervalPeriod.interval6h:
                    binanceInterval = KlineInterval.SixHour;
                    break;
                case CryptoIntervalPeriod.interval12h:
                    binanceInterval = KlineInterval.TwelveHour;
                    break;
                case CryptoIntervalPeriod.interval1d:
                    binanceInterval = KlineInterval.OneDay;
                    break;
                //case CryptoIntervalPeriod.interval3d:
                //  binanceInterval = KlineInterval.ThreeDay;
                //break;
                //case IntervalPeriod.interval1w:
                //    binanceInterval = KlineInterval.OneWeek;
                //    break;
                default:
                    // Ten teken dat het ondersteund wordt
                    binanceInterval = KlineInterval.OneMonth;
                    break;
            }
            return binanceInterval;
        }

        private async Task<long> GetCandlesForInterval(BinanceClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval)
        {
            KlineInterval binanceInterval = GetBinanceInterval(interval);
            if (binanceInterval >= KlineInterval.OneMonth)
                return 0;

            //BinanceWeights.WaitForFairBinanceWeight(5, "klines"); // *5x ivm API weight waarschuwingen

            // The maximum is 1000 candles
            WebCallResult<IEnumerable<IBinanceKline>> result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, binanceInterval, CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized), null, 1000);
            if (!result.Success)
            {
                // Might do something better than this
                GlobalData.AddTextToLogTab(String.Format("{0} {1} error getting klines {2}", symbol.Name, interval.Name, result.Error));
                return 0;
            }

            // Be carefull not going over boundaries (we stop early at 700..800 while the limit is actually 1200)
            int? weight = result.ResponseHeaders.UsedWeight();
            if (weight > 700)
            {
                GlobalData.AddTextToLogTab(string.Format("Binance delay needed for weight: {0}", weight));
                if (weight > 800)
                    await Task.Delay(10000);
                if (weight > 900)
                    await Task.Delay(10000);
                if (weight > 1000)
                    await Task.Delay(15000);
                if (weight > 1100)
                    await Task.Delay(15000);
            }

            // Might have problems with no internet etc.
            if ((result == null) || (result.Data == null) || (result.Data.Count() == 0))
            {
                GlobalData.AddTextToLogTab(String.Format("{0} {1} ophalen vanaf {2} geen candles ontvangen", symbol.Name, interval.Name, CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToString()));
                return 0;
            }

            // Remember
            long startFetchDate = (long)symbolInterval.LastCandleSynchronized;

            Monitor.Enter(symbol.CandleList);
            try
            {
                // Combine the candles, caulutaing the ones from other interval's
                foreach (BinanceSpotKline kline in result.Data)
                {
                    // Quoted = volume * price (expressed in usdt/eth/btc etc), base is coins
                    CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, interval, kline.OpenTime, 
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume);

                    // For the next GetCandles() session
                    symbolInterval.LastCandleSynchronized = candle.OpenTime;
                }
            }
            finally
            {
                Monitor.Exit(symbol.CandleList);
            }


            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
            string s = symbol.Name + " " + interval.Name + " ophalen vanaf " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " UTC tot " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime() + " UTC";
            GlobalData.AddTextToLogTab(s + " opgehaald: " + result.Data.Count() + " totaal: " + candles.Count.ToString());
            return result.Data.Count();
        }


        private async Task FetchCandlesInternal(BinanceClient client, CryptoSymbol symbol, long fetchEndUnix)
        {
            DateTime[] fetchFrom = new DateTime[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];

            DateTime utcNow = DateTime.UtcNow;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
                fetchFrom[(int)interval.IntervalPeriod] = utcNow;

            // Determine the (maximum) startdate (without knowing what we already have)
            // If Binance does not have this interval than make the lower timeframe a bit bigger
            // so we can calculate the candles
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, utcNow);

                CryptoInterval loopInterval = interval;
                while (true)
                {
                    DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                    if (fetchFrom[(int)loopInterval.IntervalPeriod] > startFetchUnixDate)
                        fetchFrom[(int)loopInterval.IntervalPeriod] = startFetchUnixDate;

                    // Is this timeframe supported?
                    if (GetBinanceInterval(loopInterval) != KlineInterval.OneMonth)
                        break;
                    else
                        loopInterval = loopInterval.ConstructFrom;
                }
            }

            // Debug
            //foreach (CryptoInterval interval in GlobalData.IntervalList)
              //  GlobalData.AddTextToLogTab("Fetching " + symbol.Name + " " + interval.Name + " " + fetchFrom[(int)interval.IntervalPeriod].ToLocalTime());


            // Correct the start date with what we already have
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                
                DateTime startFetchUnixDate = fetchFrom[(int)interval.IntervalPeriod];
                long startFetchUnix = CandleTools.GetUnixTime(startFetchUnixDate, 60);  //debug

                if ((symbolInterval.LastCandleSynchronized == null) || (startFetchUnix > symbolInterval.LastCandleSynchronized))
                    symbolInterval.LastCandleSynchronized = startFetchUnix;

                //DateTime LastCandleSynchronizedDate = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);  //debug
            }


            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

                while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
                {
                    long lastDate = (long)symbolInterval.LastCandleSynchronized;
                    DateTime symbolfetchCandleDebug = CandleTools.GetUnixDate(lastDate);  //debug

                    DateTime debug = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);  //debug
                    if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                        break;

                    long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval);
                    // Nothing more? (we have coins stopping, beaware for endless loops)
                    if ((symbolInterval.LastCandleSynchronized == lastDate) || (candleCount == 0))
                        break;
                }


                // Try to calculate new candles from what we have collected
                Monitor.Enter(symbol.CandleList);
                try
                {
                    foreach (CryptoInterval intervalCalc in GlobalData.IntervalList)
                    {
                        if (intervalCalc.ConstructFrom != null) // de 1m overslagen
                        {
                            if (intervalCalc.IntervalPeriod > interval.IntervalPeriod)
                            {
                                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(intervalCalc.ConstructFrom.IntervalPeriod);
                                SortedList<long, CryptoCandle> candlesInterval = symbolPeriod.CandleList;
                                if (candlesInterval.Values.Count > 0)
                                {
                                    long unixFirst = candlesInterval.Values.First().OpenTime;
                                    unixFirst = unixFirst - (unixFirst % intervalCalc.Duration);
                                    //DateTime dateFirstDebug = CandleTools.GetUnixDate(unixFirst);

                                    long unixLast = candlesInterval.Values.Last().OpenTime;
                                    unixLast = unixLast - (unixLast % intervalCalc.Duration);
                                    //DateTime dateLastDebug = CandleTools.GetUnixDate(unixLast);

                                    long unixLoop = unixFirst;
                                    //DateTime dateLoopDebug = CandleTools.GetUnixDate(unixLoop);

                                    // Bulk calculatie
                                    while (unixLoop <= unixLast)
                                    {
                                        CandleTools.CalculateCandleForInterval(intervalCalc, intervalCalc.ConstructFrom, symbol, unixLoop);
                                        unixLoop += intervalCalc.Duration;
                                        //dateLoopDebug = CandleTools.GetUnixDate(unixLoop); //ter debug want een unix date is onleesbaar
                                    }
                                    CandleTools.UpdateCandleFetched(symbol, intervalCalc);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(symbol.CandleList);
                }

            }
        }

        public async Task FetchCandlesAsync(long fetchEndUnix, Queue<CryptoSymbol> queue)
        {
            try
            {
                // Reuse the socket in this thread, because:
                // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
                using (BinanceClient client = new BinanceClient())
                {
                    while (true)
                    {
                        CryptoSymbol symbol;

                        Monitor.Enter(queue);
                        try
                        {
                            if (queue.Count > 0)
                                symbol = queue.Dequeue();
                            else
                                break;
                        }
                        finally
                        {
                            Monitor.Exit(queue);
                        }

                        await FetchCandlesInternal(client, symbol, fetchEndUnix);
                    }
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
            }
        }


        public async Task ExecuteAsync()
        {
            GlobalData.AddTextToLogTab("Fetching historical candles");

            CryptoExchange exchange = null;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                try
                {
                    await Semaphore.WaitAsync();
                    try
                    {
                        GlobalData.SetCandleTimerEnable(false);
                        GlobalData.AddTextToLogTab("");
                        GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                        if (GlobalData.ApplicationStatus != ApplicationStatus.AppStatusPrepare)
                        {
                            BinanceFetchSymbols fetchSymbols = new BinanceFetchSymbols();
                            await Task.Run(async () => { await fetchSymbols.ExecuteAsync(); }); // NO await, this one has to be parallel
                        }
                        GlobalData.AddTextToLogTab("Aantal symbols = " + exchange.SymbolListName.Values.Count.ToString());


                        Queue<CryptoSymbol> queue = new Queue<CryptoSymbol>();
                        foreach (var symbol in exchange.SymbolListName.Values)
                        {
                            if (CandleTools.MatchingQuote(symbol) && (symbol.Status == 1) && (!symbol.IsBarometerSymbol()))
                            {
                                //if (symbol.Name.Equals("LEVERBUSD"))
                                    queue.Enqueue(symbol);
                            }
                        }


                        // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                        // De datum en tijd tot na het activeren van beide streams (overlap)
                        DateTime fetchEndUnixDate = DateTimeOffset.UtcNow.UtcDateTime;
                        long fetchEndUnix = CandleTools.GetUnixTime(fetchEndUnixDate, 60);


                        // En dan door x tasks de queue leeg laten trekken
                        List<Task> taskList = new List<Task>();
                        while (taskList.Count < 5)
                        {
                            Task task = Task.Run(async () => { await FetchCandlesAsync(fetchEndUnix, queue); });
                            taskList.Add(task);
                        }
                        Task t = Task.WhenAll(taskList);
                        t.Wait();

                        GlobalData.AddTextToLogTab("Candles ophalen klaar", true);
                    }
                    finally
                    {
                        Semaphore.Release();

                        // Enabled analysing
                        GlobalData.SetCandleTimerEnable(false);
                        GlobalData.SetCandleTimerEnable(GlobalData.Settings.General.GetCandleInterval > 0);
                    }
                }
                catch (Exception error)
                {
                    GlobalData.Logger.Error(error);
                    GlobalData.AddTextToLogTab("error get prices " + error.ToString() + "\r\n");
                }
            }

            // Assume we now can run
            GlobalData.ApplicationStatus = ApplicationStatus.AppStatusRunning;

        }


    }
}
