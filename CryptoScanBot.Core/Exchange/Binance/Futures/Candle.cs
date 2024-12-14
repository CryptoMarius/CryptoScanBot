using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.ExtensionMethods;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

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
        BinanceRestClient client;
        if (clientBase is BinanceRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BinanceRestClient");


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

        var result = await client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)exchangeInterval, startTime: minDate, endTime: maxDate, limit: limit);
        if (!result.Success)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return minFetch;
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


        int count = result.Data.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + minDate.ToLocalTime() + " .. " + CandleTools.GetUnixDate(fetchedUpTo).ToLocalTime();
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return fetchedUpTo;
    }

}
