using CryptoExchange.Net.Objects.Errors;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanner.Core.Exchange.Mexc.Spot;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        // Remarks:
        // The maximum is 500 candles per GetKlinesAsync call (the documentation says 1000, but the
        // exchange returns 500 even when a higher limit is asked for - hence CandleLimit=500).
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        MexcRestClient client;
        if (clientBase is MexcRestClient client1)
            client = client1;
        else
            throw new Exception("Expected MexcRestClient");
        var api = client.SpotApi;

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");
        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        int attempt = 0;
    Again:
        var result = await api.ExchangeData.GetKlinesAsync(symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime(), limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success)
        {
            // Mexc answers with 429 when the weight of an endpoint is exceeded, and with 418 once
            // it decided to ban the address. A ban is lifted after ten minutes, so waiting is the
            // only sensible response - see LimitRate for the weights we book.
            if (await RetryAfterRateLimitAsync(result.Error, prefix, ++attempt))
                goto Again;
            GlobalData.AddErrorToLogTab($"{prefix} error getting klines {result.Error}");
            return (false, 0, fetchFrom);
        }


        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToDateTime()} no candles received");
#if DEBUG
            SaveCandleInfo(result, $"candles {symbol.Base}-{symbol.Quote} {interval.Name} no data.json");
#endif
            return (false, 0, fetchFrom);
        }


        CandleTime fetchedUpTo = CandleTime.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var kline in result.Data)
            {
                if (CheckFutureCandleReceived(kline.OpenTime, symbol, interval, kline.ClosePrice))
                    continue;

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.QuoteVolume);

                // remember the newest candle
                if (candle.OpenTime > fetchedUpTo)
                    fetchedUpTo = candle.OpenTime;
                //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);
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


        int count = result.Data.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {fetchFrom.ToLocalTime()} .. {fetchedUpTo.ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }

}