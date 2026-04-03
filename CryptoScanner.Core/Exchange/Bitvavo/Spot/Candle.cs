using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

/// <summary>
/// Fetches historical candles from the Bitvavo REST API.
/// Endpoint: GET /v2/{market}/candles?interval={interval}&limit={limit}&start={ms}&end={ms}
/// Response: [[timestamp_ms, "open", "high", "low", "close", "volume"], ...]
/// Max 1440 candles per request.
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        if (clientBase is not BitvavoRestClient client)
            throw new Exception("Expected BitvavoRestClient");

        string? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        List<BitvavoCandle>? bars;
        try
        {
            bars = await client.GetCandlesAsync(
                symbol.ExchangeName, exchangeInterval,
                fetchFrom.ToDateTime(), maxTime.ToDateTime(),
                Api.ExchangeOptions.CandleLimit);
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting candles {ex.Message}");
            return (false, 0, fetchFrom);
        }

        if (bars == null || bars.Count == 0)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToDateTime()} no candles received");
            // No data in this window — skip ahead so we don't loop forever
            CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
            CandleTime skipTo = maxTime > currentTime ? currentTime : maxTime;
            return (true, 0, skipTo);
        }


        CandleTime fetchedUpTo = CandleTime.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var bar in bars)
            {
                if (CheckFutureCandleReceived(bar.OpenTime, symbol, interval))
                    continue;

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, bar.OpenTime,
                    bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);

                if (candle.OpenTime > fetchedUpTo)
                    fetchedUpTo = candle.OpenTime;
            }

            if (fetchedUpTo > CandleTime.MinValue)
            {
                fetchedUpTo += interval.Duration;
            }
            else
            {
                CandleTime currentTime = CandleTime.AlignFromDateTime(DateTimeOffset.UtcNow.UtcDateTime, 1);
                fetchedUpTo = maxTime > currentTime ? currentTime : maxTime;
            }
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }


        int count = bars.Count;
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {fetchFrom.ToLocalTime()} .. {fetchedUpTo.ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }
}
