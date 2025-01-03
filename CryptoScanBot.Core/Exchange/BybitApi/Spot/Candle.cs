﻿using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.BybitApi.Spot;

// https://api.bybit.com/v5/market/kline?category=spot&end=1727265780000&interval=3&limit=1000&start=1727265720000&symbol=BTCUSDT
//{"retCode":0,"retMsg":"OK","result":{"category":"spot","symbol":"BTCUSDT","list":[
//["1727265780000","63793.89","63804.34","63766.76","63783.56","65.081143","4151231.31551695"],
//["1727265600000","63843.6","63847.81","63793.88","63793.89","32.339743","2063972.41347612"]]},
//"retExtInfo":{ },"time":1733503221768}

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<long> GetCandlesForInterval(IDisposable clientBase, CryptoSymbol symbol, CryptoInterval interval, long minFetch, long maxFetch)
    {
        // Remarks:
        // The maximum is 1000 candles per GetKlinesAsync call.
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        BybitRestClient client;
        if (clientBase is BybitRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BybitRestClient");

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        long minTime = minFetch;
        DateTime minDate = CandleTools.GetUnixDate(minTime);
        long maxTime = minTime + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;
        DateTime maxDate = CandleTools.GetUnixDate(maxTime);

        var result = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, symbol.Name, (KlineInterval)exchangeInterval, startTime: minDate, endTime: maxDate, limit: 1000);
        if (!result.Success)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return minFetch;
        }


        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(minFetch)} no candles received");
            return minFetch;
        }


        long fetchedUpTo = long.MinValue;
        await symbol.CandleLock.WaitAsync();
        try
        {
            foreach (var kline in result.Data.List)
            {
                if (symbolInterval.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                {
                    long unix = CandleTools.GetUnixTime(kline.StartTime, 60);
                    if (unix + symbolInterval.Interval.Duration > maxFetch) // future candle?
                        continue;
                }

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.StartTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.QuoteVolume);

                // remember the newest candle
                if (candle.OpenTime > fetchedUpTo)
                    fetchedUpTo = candle.OpenTime;
                //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);
            }

            // For the next session
            if (fetchedUpTo > long.MinValue)
            {
                fetchedUpTo += interval.Duration;
            }
            else
            {
                // New coins dont have History, we appearently asking for a period with no activity, skip that period
                if (maxTime > maxFetch)
                    fetchedUpTo = maxFetch;
                else
                    fetchedUpTo = maxTime;
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }


        int count = result.Data.List.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + minDate.ToLocalTime() + " .. " + CandleTools.GetUnixDate(fetchedUpTo).ToLocalTime();
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return fetchedUpTo;
    }

}
