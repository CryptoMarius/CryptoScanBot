using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitEu.Futures;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime minFetch, CandleTime maxFetch)
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

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");
        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime minTime = minFetch;
        DateTime minDate = minTime.ToDateTime();
        CandleTime maxTime = minTime + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;
        DateTime maxDate = maxTime.ToDateTime();

        var result = await api.ExchangeData.GetKlinesAsync(Category.Linear, symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: minDate, endTime: maxDate, limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return (false, 0, minTime);
        }


        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {minFetch.ToDateTime()} no candles received");
            return (false, 0, minTime);
        }


        CandleTime fetchedUpTo = CandleTime.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var kline in result.Data.List)
            {
                if (CheckFutureCandleReceived(kline.StartTime, symbol, interval, maxFetch))
                    continue;

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.StartTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume);

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


        int count = result.Data.List.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {minDate.ToLocalTime()} .. {fetchedUpTo.ToDateTime().ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }

}
