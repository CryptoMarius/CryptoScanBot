using BitMart.Net.Clients;
using BitMart.Net.Enums;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BitMart.Futures;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        // Remarks:
        // The maximum is 500 candles per GetKlinesAsync call (ExchangeOptions.CandleLimit). A wider
        // window is not truncated but refused: "40039 Invalid Timestamp".
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        BitMartRestClient client;
        if (clientBase is BitMartRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BitMartRestClient");
        var api = client.UsdFuturesApi;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        FuturesKlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");

        //LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        var result = await api.ExchangeData.GetKlinesAsync(symbol.ExchangeName, (FuturesKlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime()); //, limit: ExchangeOptions.CandleLimit
        if (!result.Success)
        {
            GlobalData.AddErrorToLogTab($"{prefix} error getting klines {result.Error}");
#if DEBUG
            SaveCandleInfo(result, $"candles {symbol.Base}-{symbol.Quote} {interval.Name} no succes.json");
#endif
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
                if (kline.Timestamp == null)
                    continue;

                if (CheckFutureCandleReceived(kline.Timestamp.Value, symbol, interval, kline.ClosePrice))
                    continue;

                // The volume of a futures kline counts CONTRACTS and this endpoint carries no turnover
                // field of its own. QuantityTickSize holds the base amount of one contract (quantity
                // precision * contract quantity, see Symbol.cs), so this gives a quote volume that can
                // be compared with the other exchanges - multiplying the contract count by a price
                // straight away was a factor contract size too high (1000 on BTCUSDT).
                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.Timestamp.Value,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.Volume * symbol.QuantityTickSize * 0.5m * (kline.HighPrice + kline.LowPrice));

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
