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

        IMultiPage<IBar> result;
        try
        {
            result = await client.GetHistoricalBarsAsync(request);
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting bars {ex.Message}");
            return (false, 0, fetchFrom);
        }

        if (result?.Items == null || !result.Items.TryGetValue(symbol.ExchangeName, out var bars) || bars == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {fetchFrom.ToDateTime()} no bars received");
            return (false, 0, fetchFrom);
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
}
