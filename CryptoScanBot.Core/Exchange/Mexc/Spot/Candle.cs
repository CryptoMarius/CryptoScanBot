using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

/// <summary>
/// Fetch candles from the exchange
/// </summary>
public class Candle
{
#if MEXCDEBUG
    private static int tickerIndex = 0;
#endif

    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);


    private static async Task<long> GetCandlesForInterval(MexcRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, long fetchEndUnix)
    {
        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            return 0;

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";


        // Fetch candles, sorting is not guaranteed (its even recersed on bybit)
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        while (true) // only for error
        {
            int exchangeLimit = 500;
            DateTime dateEnd = dateStart.AddSeconds(exchangeLimit * interval.Duration); // To create a valid date period
            var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)exchangeInterval, startTime: dateStart, endTime: dateEnd, limit: exchangeLimit);
            //GlobalData.AddTextToLogTab($"Debug: {symbol.Name} {interval.Name} volume={symbol.Volume} start={dateStart} end={dateEnd} url={result.RequestUrl}");
            if (!result.Success)
            {
                // This is based on the kucoin error number,, does Mexc have an error for overloading the exchange as wel?
                // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429: Too Many Requests
                if (result.Error?.Code == 429) // not sure if this error exists on Mexc? Copied?
                {
                    GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                    Thread.Sleep(10000); // We just retry after a delay
                    continue;
                }
                // Might do something better than this
                GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
                return 0;
            }


            // Might have problems with no internet etc.
            if (result == null || result.Data == null)
            {
                GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no data received");
                return 0;
            }

            // Remember for reporting
            long? startFetchDate = symbolInterval.LastCandleSynchronized;

            //Monitor.Enter(symbol.CandleList);
            await symbol.CandleLock.WaitAsync();
            try
            {
                if (result.Data.Any())
                {
                    long last = long.MinValue;
                    foreach (var kline in result.Data)
                    {
                        // Add candle & overwriting all previous data
                        CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                            kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.QuoteVolume, false);

                        //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);
                        // Remember the last candle
                        // We assume we have fetched all candles up to this point in time
                        // (FYI: Sorting is not guaranteed (its even recersed on bybit))
                        if (candle.OpenTime > last)
                            last = candle.OpenTime;
                    }
                    symbolInterval.LastCandleSynchronized = last + interval.Duration; // For the next session && the + saves retrieving 1 candle)
                }
                else
                {
                    // Assume there are no candles available in the requested date period ()
                    GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
                    symbolInterval.LastCandleSynchronized = CandleTools.GetUnixTime(dateEnd, interval.Duration) + interval.Duration;
                }


#if MEXCDEBUG
                // Debug, wat je al niet moet doen voor een exchange...
                Interlocked.Increment(ref tickerIndex);
                long unix = CandleTools.GetUnixTime(DateTime.UtcNow, 0);
                string filename = $@"{GlobalData.GetBaseDir()}\{Api.ExchangeOptions.ExchangeName}\Candles-{symbol.Name}-{interval.Name}-{unix}-#{tickerIndex}.json";
                string text = System.Text.Json.JsonSerializer.Serialize(result, GlobalData.JsonSerializerIndented);
                File.WriteAllText(filename, text);
#endif
            }
            finally
            {
                //Monitor.Exit(symbol.CandleList);
                symbol.CandleLock.Release();
            }


            GlobalData.AddTextToLogTab($"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from UTC {CandleTools.GetUnixDate(startFetchDate).ToLocalTime()} .. " +
                $"{CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime()} UTC received: {result.Data.Count()} total {symbolInterval.CandleList.Count}");
            return result.Data.Count();
        }
    }


    private static async Task FetchCandlesInternal(MexcRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            bool intervalSupported = Interval.GetExchangeInterval(interval.IntervalPeriod) != null;
            if (intervalSupported)
            {
                // Fetch the candles
                while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
                {
                    if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                        break;

                    long lastDate = (long)symbolInterval.LastCandleSynchronized;
                    //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                    //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());


                    // Nothing more? (we have coins stopping, beaware for endless loops)
                    long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval, fetchEndUnix);

                    if (candleCount == 0 && symbolInterval.LastCandleSynchronized > fetchEndUnix)
                        symbolInterval.LastCandleSynchronized = fetchEndUnix; // reset
                    CandleTools.UpdateCandleFetched(symbol, interval);

                    if (candleCount == 0 && symbolInterval.LastCandleSynchronized == lastDate)
                        break;
                }
            }


            //Monitor.Enter(symbol.CandleList);
            await symbol.CandleLock.WaitAsync();
            try
            {
                // Add missing candles (the only place we know it can be done safely)
                CandleTools.BulkAddMissingCandles(symbol, interval);

                // Bulk calculate the higher interval candles
                if (i + 1 < GlobalData.IntervalList.Count)
                {
                    CryptoInterval targetInterval = GlobalData.IntervalList[i + 1];
                    CryptoInterval sourceInterval = targetInterval.ConstructFrom!;
                    CandleTools.BulkCalculateCandles(symbol, sourceInterval, targetInterval, fetchEndUnix);
                }
            }
            finally
            {
                //Monitor.Exit(symbol.CandleList);
                symbol.CandleLock.Release();
            }
        }


        // Adjust the administration for the not supported interval's
        foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
        {
            bool intervalSupported = Interval.GetExchangeInterval(symbolInterval.IntervalPeriod) != null;
            if (!intervalSupported && symbolInterval.CandleList.Count > 0)
            {
                CryptoCandle candle = symbolInterval.CandleList.Values.Last();
                symbolInterval.LastCandleSynchronized = candle.OpenTime + symbolInterval.Interval.Duration;
            }
        }

        // Remove the candles we needed because of the not supported intervals & bulk calculation
        await CandleTools.CleanCandleDataAsync(symbol, null);
    }


    public static async Task FetchCandlesAsync(long fetchEndUnix, Queue<CryptoSymbol> queue)
    {
        try
        {
            // Reuse the socket in this thread, because:
            // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
            using MexcRestClient client = new();

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

                if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                {
                    Interval.DetermineFetchStartDate(symbol, fetchEndUnix);
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


    public static async Task GetCandlesForSymbolAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData!.FetchCandles)
            return;

        //GlobalData.AddTextToLogTab($"Fetching historical candles {symbol.Name}");

        using MexcRestClient client = new();
        await FetchCandlesInternal(client, symbol, fetchEndUnix);

        //GlobalData.AddTextToLogTab("Candles ophalen klaar", true);
    }


    public static async Task GetCandlesForAllSymbolsAsync()
    {
        //GlobalData.AddTextToLogTab("Fetching historical candles");

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Fetching {exchange.Name} information");
            try
            {
                await Semaphore.WaitAsync();
                try
                {
                    GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                        await Task.Run(Symbol.ExecuteAsync);

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"Aantal symbols={exchange.SymbolListName.Values.Count}");


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol())
                            continue;

                        if (symbol.QuoteData!.FetchCandles)
                        {
                            if (symbol.QuoteData.MinimalVolume == 0 || (symbol.QuoteData.MinimalVolume > 0 && symbol.Volume > 0.1m * symbol.QuoteData.MinimalVolume))
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