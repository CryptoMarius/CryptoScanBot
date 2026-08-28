using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;

namespace CryptoScanner.Core.Exchange.Kraken.Spot;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        // Remarks:
        // The maximum is 720 candles per GetKlinesAsync call (ExchangeOptions.CandleLimit).
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.
        //
        // IMPORTANT - this endpoint keeps 720 closed candles per interval and nothing before that, no
        // matter the symbol. It is a retention window, not a page size: what falls outside of it cannot
        // be fetched at all, not by asking again and not by asking differently. Measured on 14-08-2026
        // with since=1 (as far back as possible): 5m reaches back 2 days, 1h 30 days, 4h 120 days, 1d
        // 2 years. Only where a symbol has less history than that do you get less: BTC/USD 1w returns
        // 672 candles (its first week is 03-10-2013) and a coin listed last year returns what it has.
        //
        // Within that window the since parameter behaves completely normally, which is what makes the
        // paging work. Measured on the same day with fetchFrom 60, 300, 700 and 719 minutes back on 1m:
        // exactly 60, 300, 700 and 719 candles come back, always up to now, so it anchors on the START
        // of the requested range and not on the end. It is inclusive as well: a fetchFrom on an exact
        // candle boundary returns that candle itself, so no candle is lost or fetched twice between two
        // pages. Only a fetchFrom that lies BEFORE the window is clamped, and then the answer starts at
        // the oldest candle the exchange still has - which is the best that can be given, there is
        // nothing older to be had.
        //
        // What that means for the loop in CandleBase.GetCandlesForIntervalAsync, which fetches until
        // LastCandleSynchronized reaches the current time:
        // - It terminates in every case. Either the request fits in the window and behaves like any
        //   other exchange, or it is clamped and fetchedUpTo below jumps to "now" in one call.
        // - The higher intervals are not affected at all. The scanner wants 500 candles per interval
        //   and 720 are available, so the whole warmup fits inside the window with room to spare. Only
        //   a zone interval configured deeper than 720 candles would come up short.
        // - The 1m is short at startup. GetCandleFetchStart asks for 24 hours plus the barometer hours
        //   (about 1450 candles) and 720 arrive, so the series starts about 12 hours late. That is well
        //   above the 260 candles the indicators need, so nothing errors; anything that wants a full day
        //   of 1m history has less than a day of it until the socket has filled the rest in.
        // - A restart after 12 to 24 hours downtime is the one case that produces made up data, and the
        //   cause is on our side rather than the exchange's: the candles from before the downtime are
        //   still inside the 1m window, the fetch adds the newest 12 hours, and the minutes in between
        //   are gone from Kraken for good - after which CandleTools.BulkAddMissingCandles closes that
        //   hole with flat candles (previous close, volume 0, IsFilled = true). Longer downtime is
        //   harmless again: those older candles fall outside the window and are cleaned up, leaving a
        //   series that starts later instead of a series with invented candles in the middle.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        KrakenRestClient client;
        if (clientBase is KrakenRestClient client1)
            client = client1;
        else
            throw new Exception("Expected KrakenRestClient");
        var api = client.SpotApi;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        int limit = Api.ExchangeOptions.CandleLimit;
        CandleTime maxTime = fetchFrom + (limit - 1) * interval.Duration;

        // Should the exchange refuse a request anyway ("Too many requests"), then waiting out the
        // window and asking again is the only sensible answer - the same treatment BitMart and
        // Coinbase got. Giving up returns the same fetchFrom to the caller, which stops the loop over
        // this symbol and interval and leaves a hole in the history until the next refresh cycle.
        // Bounded on purpose: an address that stays blocked must not keep a fetch thread here forever.
        int attempt = 0;
    Again:
        var result = await api.ExchangeData.GetKlinesAsync(symbol.ExchangeName, (KlineInterval)exchangeInterval,
            fetchFrom.ToDateTime());
        if (!result.Success)
        {
            if (await RetryAfterRateLimitAsync(result.Error, prefix, ++attempt))
                goto Again;
            GlobalData.AddErrorToLogTab($"{prefix} error getting klines {result.Error}");
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
            foreach (var kline in result.Data.Data)
            {
                if (CheckFutureCandleReceived(kline.OpenTime, symbol, interval, kline.ClosePrice))
                    continue;

                // Kraken reports the volume of a candle in BASE units, while the scanner stores the
                // QUOTE volume everywhere else (Binance QuoteVolume, Okx VolumeCurrencyQuote, and
                // the kline ticker of this exchange, which converts as well). Kraken hands the volume
                // weighted average price of the candle along, so the conversion is exact instead of
                // the approximation with the middle of the candle that the exchanges without a vwap
                // need - without this the history is in a different unit than the live candles.
                decimal quoteVolume = kline.VolumeWeightedAveragePrice > 0
                    ? kline.Volume * kline.VolumeWeightedAveragePrice
                    : kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice);

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, quoteVolume);

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


        int count = result.Data.Data.Count();
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {fetchFrom.ToLocalTime()} .. {fetchedUpTo.ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }

}
