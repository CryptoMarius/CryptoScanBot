using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using HyperLiquid.Net.Clients;
using HyperLiquid.Net.Enums;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Perpetual;

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
        //
        // Correction, measured against the live API on 28-08-2026: the maximum is 5000, not 1000, and
        // it is a count and not a time span - 5000 x 15m came back with 4994 rows. Over that count the
        // exchange returns the NEWEST 5000 of the window, so the window itself has to stay inside it.
        // ExchangeOptions.CandleLimit carries that number and the reasoning, see Api.cs.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        HyperLiquidRestClient client;
        if (clientBase is HyperLiquidRestClient client1)
            client = client1;
        else
            throw new Exception("Expected HyperLiquidRestClient");
        var api = client.FuturesApi;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod)
            ?? throw new Exception($"Not supported interval");

        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        // Should the exchange refuse a request anyway ("Server rate limit exceeded"), then waiting
        // out the window and asking again is the only sensible answer. Giving up returns the same
        // fetchFrom to the caller, which stops the loop over this symbol and interval and leaves a
        // hole in the history until the next refresh cycle.
        //
        // This one needs the retry more than the others: HyperLiquid grants 1200 request weight per
        // minute and an info request such as this one weighs 20, so the library's rate limiter holds
        // us to exactly 60 requests per minute - measured over the night of 19/20-08-2026, 58 minutes
        // on Perpetual and 150 on Spot sat at precisely that ceiling. Running permanently AT the
        // documented budget leaves no room for the calls the limiter does not see coming (the symbol
        // and ticker refresh), which is where the four rejections of that night came from.
        //
        // Correction, checked against the API documentation on 28-08-2026: a candle request does NOT
        // weigh a flat 20. candleSnapshot carries an additional weight per 60 candles in the answer,
        // which the package cannot know beforehand and therefore never books. So 60 requests per
        // minute was never reachable - our average request came to 32 weight, which puts the address
        // ceiling at some 37 of them. The surcharge is booked below, once the answer is in; the whole
        // reasoning and the measurements live in HyperLiquidLimits.
        int attempt = 0;
    Again:
        var result = await api.ExchangeData.GetKlinesAsync(symbol.ExchangeName, (KlineInterval)exchangeInterval,
            startTime: fetchFrom.ToDateTime(), endTime: maxTime.ToDateTime()); //, limit: ExchangeOptions.CandleLimit
        if (!result.Success)
        {
            if (await RetryAfterRateLimitAsync(result.Error, prefix, ++attempt))
                goto Again;
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


        // What the exchange charged on top of the flat 20 the package booked. Deliberately here and
        // not after the loop below: this call waits when the budget is full, and holding the candle
        // lock of a symbol while waiting out a rate limit window would block the analysis threads for
        // up to a minute.
        int count = result.Data.Count();
        await HyperLiquidLimits.BookCandleWeightAsync(count);

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
                    kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));

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


        // count was already taken above, where the rate limit surcharge is booked
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {fetchFrom.ToLocalTime()} .. {fetchedUpTo.ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }

}
