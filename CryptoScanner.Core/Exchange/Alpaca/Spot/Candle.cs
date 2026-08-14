using Alpaca.Markets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

/// <summary>
/// Fetch historical bars (candles) from Alpaca.
/// Alpaca returns bars as IBar with TimeUtc, Open, High, Low, Close, Volume.
/// GetHistoricalBarsAsync returns IMultiPage keyed by symbol name.
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<(bool, int, CandleTime)> GetCandlesForInterval(IDisposable clientBase,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom)
    {
        if (clientBase is not IAlpacaDataClient client)
            throw new Exception("Expected IAlpacaDataClient");

        BarTimeFrame? barTimeFrame = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (barTimeFrame == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime maxTime = fetchFrom + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;

        var timeInterval = new Interval<DateTime>(fetchFrom.ToDateTime(), maxTime.ToDateTime());
        var request = new HistoricalBarsRequest(symbol.ExchangeName, barTimeFrame.Value, timeInterval)
            .WithPageSize((uint)Api.ExchangeOptions.CandleLimit);

        // The feed the plan allows (see Api.DataFeed), and prices that are corrected for stock splits.
        // Without the correction the history holds the prices exactly as they were traded, so a 10:1
        // split reads as a 90% crash on the day it happened - and every indicator reads it that way too.
        request.Feed = Api.DataFeed;
        request.Adjustment = Adjustment.SplitsOnly;

        IMultiPage<IBar> result;
        try
        {
            result = await client.GetHistoricalBarsAsync(request, ExchangeBase.CancellationToken);
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting bars {ex.Message}");
            return (false, 0, fetchFrom);
        }

        if (result?.Items == null || !result.Items.TryGetValue(symbol.ExchangeName, out var bars) || bars == null)
        {
            // A stock market is closed at night, in the weekend and on holidays, so a period without a
            // single bar is normal here instead of exceptional. Returning the moment we started from
            // would leave LastCandleSynchronized where it is (see CandleBase.GetCandlesForIntervalAsync)
            // and the next cycle would ask for the very same empty period again, forever - a weekend is
            // longer than the period one request covers, so it would never move past a friday evening.
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToDateTime()} no bars received");
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
                if (CheckFutureCandleReceived(bar.TimeUtc, symbol, interval, bar.Close))
                    continue;

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, bar.TimeUtc,
                    bar.Open, bar.High, bar.Low, bar.Close, GetQuoteVolume(bar));

                if (candle.OpenTime > fetchedUpTo)
                    fetchedUpTo = candle.OpenTime;
            }

            if (fetchedUpTo > CandleTime.MinValue)
            {
                fetchedUpTo += interval.Duration;
            }
            else
            {
                // No data in this period, skip ahead
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


        int count = bars.Count;
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolPeriod.CandleList;
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {fetchFrom.ToLocalTime()} .. {fetchedUpTo.ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return (true, count, fetchedUpTo);
    }


    /// <summary>
    /// The volume of a bar in the quote currency (dollars). Alpaca states a number of shares, while
    /// the scanner works in quote volume everywhere else - the 24 hour volume of the symbol is stated
    /// that way as well, and so is the volume of every other exchange. The VWAP is the average price
    /// those shares changed hands at; the middle of the bar is the fallback for a bar without one.
    /// Shared with the live stream so both sides of the same candle count the same way.
    /// </summary>
    internal static decimal GetQuoteVolume(IBar bar)
    {
        decimal price = bar.Vwap > 0 ? bar.Vwap : 0.5m * (bar.High + bar.Low);
        return bar.Volume * price;
    }
}
