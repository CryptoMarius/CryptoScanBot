using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<long> GetCandlesForInterval(IDisposable clientBase, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, long minFetch, long maxFetch)
    {
        // Remarks:
        // The maximum is 1000 candles per GetKlinesAsync call.
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        MexcRestClient client;
        if (clientBase is MexcRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BybitRestClient");


        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        int limit = Api.ExchangeOptions.CandleLimit;
        long minTime = minFetch;
        DateTime minDate = CandleTools.GetUnixDate(minTime);
        long maxTime = minTime + (limit - 1) * interval.Duration;
        DateTime maxDate = CandleTools.GetUnixDate(maxTime);

    Again:
        var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)exchangeInterval, startTime: minDate, endTime: maxDate, limit: limit);
        if (!result.Success)
        {
            // This is based on the kucoin error number,, does Mexc have an error for overloading the exchange as wel?
            // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429: Too Many Requests
            if (result.Error?.Code == 429) // not sure if this error exists on Mexc? Copied?
            {
                GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                Thread.Sleep(15000);
                //continue;
                goto Again;
            }
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
            foreach (var kline in result.Data)
            {
                if (symbolInterval.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                {
                    long unix = CandleTools.GetUnixTime(kline.OpenTime, 60);
                    if (unix + symbolInterval.Interval.Duration > maxFetch) // future candle?
                        continue;
                }

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.QuoteVolume, false);

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
                // New coins dont have history, we appearently asking for a period with no activity, skip that period
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


        int count = result.Data.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + minDate.ToLocalTime() + " .. " + CandleTools.GetUnixDate(fetchedUpTo).ToLocalTime();
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return fetchedUpTo;
    }

}