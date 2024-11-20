﻿using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.BybitApi.Futures;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    private static async Task<long> GetCandlesForInterval(BybitRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, long fetchEndUnix)
    {
        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            return 0;

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        // The maximum is 1000 candles
        // Attention: The order can be from new to old and it can contain in progress candles..
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        var result = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Linear, symbol.Name, (KlineInterval)exchangeInterval, startTime: dateStart, null, limit: 1000);
        if (!result.Success)
        {
            // Might do something better than this
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return 0;
        }


        // Might have problems with no internet etc.
        if (result == null || result.Data == null || !result.Data.List.Any())
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
            return 0;
        }

        // Remember
        long? startFetchDate = symbolInterval.LastCandleSynchronized;

        await symbol.CandleLock.WaitAsync();
        try
        {
            long last = long.MinValue;
            foreach (var kline in result.Data.List)
            {
                // The exchange can deliver future candles, suprises, skip!
                // Might have build problems if we exclude them, but voila...
                // (just make sure we alway's include the 1m)
                // We can exclude future candles with parameters, better then quick fix...!
                if (symbolInterval.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                {
                    long unix = CandleTools.GetUnixTime(kline.StartTime, 60);
                    if (unix + symbolInterval.Interval.Duration > fetchEndUnix)
                        continue;
                }

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.StartTime,
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
            symbol.CandleLock.Release();
        }


        int count = result.Data.List.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + CandleTools.GetUnixDate(startFetchDate).ToLocalTime() + " .. " + CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime();
        GlobalData.AddTextToLogTab(s + " received: " + count + " total: " + candles.Count.ToString());
        return count;
    }


    public async Task GetCandlesForIntervalAsync(IDisposable? clientBase, CryptoSymbol symbol, CryptoInterval interval, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        BybitRestClient client;
        if (clientBase is BybitRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BybitRestClient");


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
                CandleTools.UpdateCandleFetched(symbol, interval);
                if (symbolInterval.LastCandleSynchronized == lastDate || candleCount == 0)
                    break;
            }
        }


        await symbol.CandleLock.WaitAsync();
        try
        {
            // Add missing candles (the only place we know it can be done safely)
            CandleTools.BulkAddMissingCandles(symbol, interval);

            // Bulk calculate the higher interval candles
            if (interval.IntervalPeriod < Enum.GetValues(typeof(CryptoIntervalPeriod)).Cast<CryptoIntervalPeriod>().Last())
            {
                CryptoInterval targetInterval = GlobalData.IntervalListPeriod[interval.IntervalPeriod + 1];
                CryptoInterval sourceInterval = targetInterval.ConstructFrom!;
                CandleTools.BulkCalculateCandles(symbol, sourceInterval, targetInterval, fetchEndUnix);
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }


        // Adjust the administration for the not supported interval's
        if (!intervalSupported && symbolInterval.CandleList.Count > 0)
        {
            CryptoCandle candle = symbolInterval.CandleList.Values.Last();
            symbolInterval.LastCandleSynchronized = candle.OpenTime + symbolInterval.Interval.Duration;
        }
    }


    public async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        using BybitRestClient client = new();
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            await GetCandlesForIntervalAsync(client, symbol, interval, fetchEndUnix);
        }


        // Remove the candles we needed because of the not supported intervals & bulk calculation
        await CandleTools.CleanCandleDataAsync(symbol, null);
    }
}
