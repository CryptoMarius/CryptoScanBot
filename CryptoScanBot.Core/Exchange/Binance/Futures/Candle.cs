﻿using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.ExtensionMethods;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures;

using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

/// <summary>
/// Fetch candles from the exchange
/// </summary>
public class Candle
{
    // Prevent multiple sessions
    private static readonly SemaphoreSlim Semaphore = new(1);


    private static async Task<long> GetCandlesForInterval(BinanceRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, long fetchEndUnix)
    {
        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            return 0;

        //BinanceWeights.WaitForFairBinanceWeight(5, "klines"); // *5x ivm API weight waarschuwingen
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval.Name}";

        // The maximum is 1000 candles
        // Attention: The order can be from new to old and it can contain in progress candles..
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        WebCallResult<IEnumerable<IBinanceKline>> result = await client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)exchangeInterval, dateStart, null, 1000);
        if (!result.Success)
        {
            // Might do something better than this
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return 0;
        }

        // Be carefull not going over boundaries (we stop early at 700..800 while the limit is actually 1200)
        int? weight = result.ResponseHeaders.UsedWeight();
        if (weight > 700)
        {
            GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: {weight} (because of rate limits)");
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
        if (result == null || result.Data == null || !result.Data.Any())
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
            return 0;
        }

        // Remember
        long? startFetchDate = symbolInterval.LastCandleSynchronized;

        Monitor.Enter(symbol.CandleList);
        try
        {
            long last = long.MinValue;
            foreach (var kline in result.Data)
            {
                // The exchange can deliver future candles, suprises, skip!
                // Might have build problems if we exclude them, but voila...
                // (just make sure we alway's include the 1m)
                // We can exclude future candles with parameters, better then quick fix...!
                if (symbolInterval.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                {
                    long unix = CandleTools.GetUnixTime(kline.OpenTime, 60);
                    if (unix + symbolInterval.Interval.Duration > fetchEndUnix)
                        continue;
                }

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.QuoteVolume, false);
                //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);

                // Onthoud de laatste candle, t/m die datum is alles binnen gehaald.
                // NB: De candle volgorde is niet gegarandeerd (op bybit zelfs omgedraaid)
                if (candle.OpenTime > last)
                    last = candle.OpenTime;
            }

            // For the next session
            if (last > long.MinValue)
            {
                symbolInterval.LastCandleSynchronized = last + interval.Duration; // new (saves 1 candle)
                // Alternatief (maar als er gaten in de candles zijn geeft dit problemen, endless loops)
                //CandleTools.UpdateCandleFetched(symbol, interval);
            }

            //SaveInformation(symbol, result.Data.List);
        }
        finally
        {
            Monitor.Exit(symbol.CandleList);
        }


        int count = result.Data.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " .. " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime();
        GlobalData.AddTextToLogTab(s + " received: " + count + " total: " + candles.Count.ToString());
        return count;
    }


    private static async Task FetchCandlesInternal(BinanceRestClient client, CryptoSymbol symbol, long fetchEndUnix)
    {
        //GlobalData.AddTextToLogTab($"{symbol.Name} FetchCandlesInternal");

        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            bool intervalSupported = Interval.GetExchangeInterval(interval.IntervalPeriod) != null;
            //GlobalData.AddTextToLogTab($"{symbol.Name} FetchCandlesInternal {interval.Name} supported={intervalSupported}");
            if (intervalSupported)
            {
                // Fetch the candles
                while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
                {
                    long lastDate = (long)symbolInterval.LastCandleSynchronized;
                    //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                    //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());

                    if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                        break;

                    // Nothing more? (we have coins stopping, beaware for endless loops)
                    long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval, fetchEndUnix);
                    CandleTools.UpdateCandleFetched(symbol, interval);
                    if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                        break;
                }
            }


            Monitor.Enter(symbol.CandleList);
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
                Monitor.Exit(symbol.CandleList);
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
        CandleTools.CleanCandleData(symbol, null);
    }


    public static async Task FetchCandlesAsync(long fetchEndUnix, Queue<CryptoSymbol> queue)
    {
        try
        {
            // Reuse the socket in this thread, because:
            // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
            using BinanceRestClient client = new();

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

                // Did we switch exchange (it takes a long time)
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
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        GlobalData.AddTextToLogTab($"Fetching historical candles {symbol.Name}");

        using BinanceRestClient client = new();
        await FetchCandlesInternal(client, symbol, fetchEndUnix);

        GlobalData.AddTextToLogTab("Candles ophalen klaar");
    }


    public static async Task GetCandlesForAllSymbolsAsync()
    {
        GlobalData.AddTextToLogTab("Fetching historical candles");

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
                    while (taskList.Count < 1)
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
