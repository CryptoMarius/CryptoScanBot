using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Clients;
using Kucoin.Net.Enums;

using ApiAlias = CryptoScanBot.Core.Exchange.Kucoin.Futures;

namespace ExchangeTest.Exchange.Kucoin.Futures;

/// <summary>
/// Fetch the candles from Binance
/// </summary>
public class Candles
{
    const string ExchangeName = "Kucoin Futures";

#if KUCOINDEBUG
    private static int tickerIndex = 0;
#endif
    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    private static FuturesKlineInterval? GetExchangeInterval(CryptoInterval interval)
    {
        return interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => FuturesKlineInterval.OneMinute,
            //CryptoIntervalPeriod.interval3m => FuturesKlineInterval.ThreeMinutes,
            CryptoIntervalPeriod.interval5m => FuturesKlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => FuturesKlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => FuturesKlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => FuturesKlineInterval.OneHour,
            CryptoIntervalPeriod.interval2h => FuturesKlineInterval.TwoHours,
            CryptoIntervalPeriod.interval4h => FuturesKlineInterval.FourHours,
            //CryptoIntervalPeriod.interval6h => FuturesKlineInterval.SixHours,
            CryptoIntervalPeriod.interval8h => FuturesKlineInterval.EightHours,
            CryptoIntervalPeriod.interval12h => FuturesKlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => FuturesKlineInterval.OneDay,
            _ => null,
        };
    }

    private static async Task<long> GetCandlesForInterval(KucoinRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval)
    {
        FuturesKlineInterval? exchangeInterval = GetExchangeInterval(interval);
        if (exchangeInterval == null)
            return 0;

        //KucoinWeights.WaitForFairWeight(1);
        string prefix = $"{ExchangeName} {symbol.Name} {interval.Name}";

        // bewust 5 candles terug (omdat de API qua klines raar doet, hebben we in ieder geval 1 te pakken)
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized - 10 * interval.Duration);
        while (true)
        {
            //DateTime dateEnd = dateStart.AddSeconds(1000 * interval.Duration); // , dateEnd

            // We get the klines in ascending order with a limit of 200 klines per call
            var result = await client.FuturesApi.ExchangeData.GetKlinesAsync(symbol.Name, (FuturesKlineInterval)exchangeInterval, dateStart);
            //GlobalData.AddTextToLogTab($"Debug: {symbol.Name} {interval.Name} volume={symbol.Volume} start={dateStart} end={dateEnd} url={result.RequestUrl}");
            if (!result.Success)
            {
                // Simply retry (or at least that seems to be the advise)
                // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429000: Too Many Requests
                if (result.Error?.Code == 429000)
                {
                    GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                    Thread.Sleep(15000);
                    continue;
                }

                // Might do something better than this
                GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
                return 0;
            }


            // Might have problems with no internet etc.
            if (result == null || result.Data == null || !result.Data.Any())
            {
                GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
                return 0;
            }

            // Remember
            long? startFetchDate = symbolInterval.LastCandleSynchronized;

            //Monitor.Enter(symbol.CandleList);
            await symbol.CandleLock.WaitAsync();
            try
            {
                long last = long.MinValue;
                // Combine candles, calculating other interval's
                foreach (var kline in result.Data)
                {
                    // Quoted = volume * price (expressed in usdt/eth/btc etc), base is coins
                    CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 0, kline.Volume, false);

                    // Onthoud de laatste aangeleverde candle, t/m die datum is ten minste alles binnen gehaald
                    if (candle.OpenTime > last)
                        last = candle.OpenTime;
                }

                // Voor de volgende GetCandlesForInterval() sessie
                if (last > long.MinValue)
                {
                    symbolInterval.LastCandleSynchronized = last;
                    // Alternatief (maar als er gaten in de candles zijn geeft dit problemen, endless loops)
                    //CandleTools.UpdateCandleFetched(symbol, interval);
                }

#if KUCOINDEBUG
                // Debug, wat je al niet moet doen voor een exchange...
                tickerIndex++;
                long unix = CandleTools.GetUnixTime(DateTime.UtcNow, 0);
                string filename = GlobalData.GetBaseDir() + $@"\Kucoin\Candles-{symbol.Name}-{interval.Name}-{unix}-#{tickerIndex}.json";
                string text = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true});
                File.WriteAllText(filename, text);
#endif
            }
            finally
            {
                //Monitor.Exit(symbol.CandleList);
                symbol.CandleLock.Release();
            }


            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
            string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " .. " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime();
            GlobalData.AddTextToLogTab(s + " fetched: " + result.Data.Count() + " total: " + candles.Count.ToString());
            return result.Data.Count();
        }
    }


    private static async Task FetchCandlesInternal(KucoinRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            // Fetch the candles
            while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
            {
                long lastDate = (long)symbolInterval.LastCandleSynchronized;
                //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());


                if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                    break;

                // Nothing more? (we have coins stopping, be aware for endless loops)
                long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval);
                if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                    break;
            }

            //Monitor.Enter(symbol.CandleList);
            await symbol.CandleLock.WaitAsync();
            try
            {
                // Fill missing candles (the only place we know fore it can be done)
                // We hebben de candles opgevraagd van x tot y, dat betekend dat we alle candles hebben,
                // eventueel ontbrekende candles in deze reeks mogen we opvullen met een "zero" candle
                if (symbolInterval.CandleList.Count != 0)
                {
                    CryptoCandle stickOld = symbolInterval.CandleList.Values.First();
                    //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
                    long unixTime = stickOld.OpenTime;
                    while (unixTime < symbolInterval.LastCandleSynchronized)
                    {
                        if (!symbolInterval.CandleList.TryGetValue(unixTime, out CryptoCandle? candle))
                        {
                            candle = new()
                            {
                                OpenTime = unixTime,
                                Open = stickOld.Close,
                                High = stickOld.Close,
                                Low = stickOld.Close,
                                Close = stickOld.Close,
#if SUPPORTBASEVOLUME
                                BaseVolume = 0,
#endif
                                Volume = 0
                            };
                            symbolInterval.CandleList.Add(candle.OpenTime, candle);
                            //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Added missing candle " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
                        }
                        stickOld = candle;
                        unixTime += interval.Duration;
                    }
                }

                // Calculate higher interval candles from the lower interval (if available)
                for (int j = i + 1; j < GlobalData.IntervalList.Count; j++)
                {
                    CryptoInterval intervalCalc = GlobalData.IntervalList[j];
                    if (intervalCalc.IntervalPeriod > interval.IntervalPeriod)
                    {
                        // Naar het lagere tijd interval om de eerste en laatste candle te achterhalen
                        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(intervalCalc.ConstructFrom!.IntervalPeriod);
                        SortedList<long, CryptoCandle> candlesLowerInterval = symbolPeriod.CandleList;
                        if (candlesLowerInterval.Values.Any())
                        {
                            long unixFirst = candlesLowerInterval.Values.First().OpenTime;
                            unixFirst -= unixFirst % intervalCalc.Duration; // too much?
                            //DateTime dateFirstDebug = CandleTools.GetUnixDate(unixFirst);

                            long unixLast = candlesLowerInterval.Values.Last().OpenTime;
                            unixLast -= unixLast % intervalCalc.Duration; // too much? ++ ?
                            //DateTime dateLastDebug = CandleTools.GetUnixDate(unixLast);

                            // Bulk calculation (shared code with the 1m stream)
                            long unixLoop = unixFirst;
                            while (unixLoop <= unixLast)
                            {
                                CandleTools.CalculateCandleForInterval(symbol, intervalCalc.ConstructFrom, intervalCalc, unixLoop);
                                unixLoop += intervalCalc.Duration;
                            }
                            CandleTools.UpdateCandleFetched(symbol, intervalCalc);
                        }
                    }
                }

            }
            finally
            {
                //Monitor.Exit(symbol.CandleList);
                symbol.CandleLock.Release();
            }
        }
    }

    public static async Task FetchCandlesAsync(long fetchEndUnix, Queue<CryptoSymbol> queue)
    {
        try
        {
            // Reuse the socket in this thread, because:
            // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
            using KucoinRestClient client = new();

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

                // Er is niet geswicthed van exchange (omdat het ophalen zo lang duurt)
                if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                {
                    CandleTools.DetermineFetchStartDate(symbol, fetchEndUnix);
                    await FetchCandlesInternal(client, symbol, fetchEndUnix);
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
        }
    }


    public static async Task ExecuteAsync(string symbolName)
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out CryptoScanBot.Core.Model.CryptoExchange? exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(string.Format("Fetching {0} information", exchange.Name));
            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    //if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                    //    await Task.Run(GetSymbols.ExecuteAsync);

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"Aantal symbols={exchange.SymbolListName.Values.Count}");


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        // limit the amount....
                        if (!symbol.Name.Equals(symbolName))
                            continue;

                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol())
                            continue;

                        if (symbol.QuoteData.FetchCandles)
                        {
                            if (symbol.QuoteData.MinimalVolume > 0 && symbol.Volume > 0.1m * symbol.QuoteData.MinimalVolume)
                                //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                                queue.Enqueue(symbol);
                        }
                    }


                    // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                    // De datum en tijd tot na het activeren van beide streams (overlap)
                    DateTime fetchEndUnixDate = DateTimeOffset.UtcNow.UtcDateTime;
                    long fetchEndUnix = CandleTools.GetUnixTime(fetchEndUnixDate, 60);


                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () => { await FetchCandlesAsync(fetchEndUnix, queue); });
                        taskList.Add(task);
                    }
                    await Task.WhenAll(taskList).ConfigureAwait(false);

                    GlobalData.AddTextToLogTab("Candles ophalen klaar");
                }
                finally
                {
                    // Enabled analysing
                    GlobalData.SetCandleTimerEnable(true);

                    Semaphore.Release();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("error get prices " + error.ToString());
            }
        }
    }


}
