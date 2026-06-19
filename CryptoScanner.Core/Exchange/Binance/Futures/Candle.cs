using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.ExtensionMethods;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Futures;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        // Remarks:
        // The maximum is 1000 candles per GetKlinesAsync call
        // The results can be from new to old (wrong order)
        // The results can also contain in progress candles

        BinanceRestClient client;
        if (clientBase is BinanceRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BinanceRestClient");
        var api = client.UsdFuturesApi;

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;
        var result = await api.ExchangeData.GetKlinesAsync(symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime(), limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToLocalTime()} error getting klines {result.Error}");
            return (false, 0, fetchFrom);
        }

        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToLocalTime()} no candles received");
            return (false, 0, fetchFrom);
        }


        // Be carefull not going over boundaries (we stop early at 700..800 while the limit is actually 1200)
        int? weight = result.ResponseHeaders?.Select(h => new KeyValuePair<string, string[]>(h.Key, h.Value.ToArray())).ToArray().UsedWeight();
        if (weight > 700)
        {
            GlobalData.AddTextToLogTab($"{prefix} delay needed because of rate limits");
            if (weight > 800)
                await Task.Delay(10000);
            if (weight > 900)
                await Task.Delay(10000);
            if (weight > 1000)
                await Task.Delay(15000);
            if (weight > 1100)
                await Task.Delay(15000);
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
