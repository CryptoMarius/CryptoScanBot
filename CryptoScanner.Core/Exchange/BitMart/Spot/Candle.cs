using BitMart.Net.Clients;
using BitMart.Net.Enums;

using CryptoExchange.Net.Objects.Errors;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;


namespace CryptoScanner.Core.Exchange.BitMart.Spot;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        // Remarks:
        // The maximum is 200 candles per GetKlinesAsync call (ExchangeOptions.CandleLimit). A wider
        // window is not truncated but refused: "71004 request kline num exceed the limit".
        // Without an explicit limit the endpoint hands over 100 candles whatever the window is, so
        // it has to be passed - the oldest ones of the window, which is the direction we page in.
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        BitMartRestClient client;
        if (clientBase is BitMartRestClient client1)
            client = client1;
        else
            throw new Exception("Expected BitmartRestClient");
        var api = client.SpotApi;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        // Should the exchange refuse a request anyway ("Server rate limit exceeded"), then waiting out
        // the window and asking again is the only sensible answer. Giving up returns the same fetchFrom
        // to the caller, which stops the loop over this symbol and interval and leaves a hole in the
        // history until the next refresh cycle. Bounded on purpose: an address that stays blocked must
        // not keep a fetch thread here forever.
        int attempt = 0;
    Again:
        var result = await api.ExchangeData.GetKlinesAsync(symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime(), limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success)
        {
            if (result.Error?.ErrorType == ErrorType.RateLimitRequest && ++attempt <= 5)
            {
                GlobalData.AddTextToLogTab($"{prefix} delay needed because of rate limits (attempt {attempt})");
                await Task.Delay(5000);
                if (ExchangeBase.CancellationToken.IsCancellationRequested)
                    return (false, 0, fetchFrom);
                goto Again;
            }
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
                if (CheckFutureCandleReceived(kline.OpenTime, symbol, interval, kline.ClosePrice))
                    continue;

                // QuoteVolume ("quote_volume") is the turnover the exchange itself reports for the
                // candle, so there is no need to estimate it from the base volume and a middle price.
                // It is nullable in the library, hence the fall back to that estimate.
                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.QuoteVolume ?? kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));

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
