using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitEu.Spot;

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

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");
        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        var result = await api.ExchangeData.GetKlinesAsync(Category.Spot, symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime(), limit: 1000);
        if (!result.Success)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
            return (false, 0, fetchFrom);
        }


        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToDateTime()} no candles received");
            return (false, 0, fetchFrom);
        }


        CandleTime fetchedUpTo = CandleTime.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var kline in result.Data.List)
            {
                if (CheckFutureCandleReceived(kline.StartTime, symbol, interval))
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
