using BloFin.Net.Clients;
using BloFin.Net.Enums;

using CryptoExchange.Net.Objects.Errors;
using CryptoExchange.Net.SharedApis;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;


namespace CryptoScanBot.Core.Exchange.BloFin.Futures;

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
        BloFinRestClient client;
        if (clientBase is BloFinRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BloFinRestClient");
        var api = client.FuturesApi;

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

    Again:
        string symbolName = api.FormatSymbol(symbol.Base, symbol.Quote, TradingMode.PerpetualLinear);
        var result = await api.ExchangeData.GetKlinesAsync(symbolName, (KlineInterval)exchangeInterval, 
            startTime: minDate, endTime: maxDate, limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success)
        {
            if (result.Error?.Code == 429) // not sure if this error exists on Mexc? Copied?
            {
                GlobalData.AddTextToLogTab($"{prefix} delay needed because of rate limits");
                Thread.Sleep(15000);
                //continue;
                goto Again;
            }
            if (result.Error?.ErrorType == ErrorType.RateLimitRequest)
            {
                GlobalData.AddTextToLogTab($"{prefix} delay needed because of rate limits");
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
#if DEBUG
            SaveCandleInfo(result, $"candles {symbol.Base}-{symbol.Quote} {interval.Name} no data.json");
#endif
            return minFetch;
        }


        long fetchedUpTo = long.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
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
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.Volume, kline.QuoteVolume);

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
            symbol.Data.CandleLock.Release();
        }


        int count = result.Data.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = symbol.Exchange.Name + " " + symbol.Name + " " + interval.Name + " fetch from " + minDate.ToLocalTime() + " .. " + CandleTools.GetUnixDate(fetchedUpTo).ToLocalTime();
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return fetchedUpTo;
    }

}
