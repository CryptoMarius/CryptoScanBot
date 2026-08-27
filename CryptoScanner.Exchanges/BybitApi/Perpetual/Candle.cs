using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoExchange.Net.Objects.Errors;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitApi.Perpetual;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
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
        var api = client.V5Api;

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        // Should the exchange refuse a request anyway ("Too many visits"), then waiting out the
        // window and asking again is the only sensible answer. Giving up returns the same fetchFrom
        // to the caller, which stops the loop over this symbol and interval and leaves a hole in the
        // history until the next refresh cycle. Same shape as BitMart and BloFin already use.
        //
        // Our own LimitRate keeps this process to 200 requests per 20 seconds, twelve times below
        // the 600 per 5 seconds Bybit documents per IP, and the busiest five seconds of the night of
        // 19/20-08-2026 held 54 requests. The three rejections that night all landed during the
        // history backfill of a freshly listed symbol (METUSDT, MAGMAUSDT), where every response is
        // the maximum of 1000 candles - so the trigger is the weight of the responses, not the
        // number of requests, and lowering the request rate would not have prevented them.
        int attempt = 0;
    Again:
        var result = await api.ExchangeData.GetKlinesAsync(Category.Linear, symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime(), limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success)
        {
            if (await RetryAfterRateLimitAsync(result.Error, prefix, ++attempt))
                goto Again;
            GlobalData.AddErrorToLogTab($"{prefix} fetch from {fetchFrom.ToLocalTime()} error getting klines {result.Error}");
            return (false, 0, fetchFrom);
        }

        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToLocalTime()} no candles received");
            return (false, 0, fetchFrom);
        }

        CandleTime fetchedUpTo = CandleTime.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var kline in result.Data.List)
            {
                if (CheckFutureCandleReceived(kline.StartTime, symbol, interval, kline.ClosePrice))
                    continue;

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.StartTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume);

                // remember the newest candle
                if (candle.OpenTime > fetchedUpTo)
                    fetchedUpTo = candle.OpenTime;
                //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.ExchangeSymbol + " " + interval.ExchangeSymbol + " " + candle.DateLocal);
            }

            // For the next session
            if (fetchedUpTo > CandleTime.MinValue)
            {
                fetchedUpTo += interval.Duration;
            }
            else
            {
                // New coins dont have History, we appearently asking for a period with no activity, skip that period
                CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
                if (maxTime > currentTime)
                    fetchedUpTo = currentTime;
                else
                    fetchedUpTo = maxTime;
            }
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }


        int count = result.Data.List.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {fetchFrom.ToLocalTime()} .. {fetchedUpTo.ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }

}
