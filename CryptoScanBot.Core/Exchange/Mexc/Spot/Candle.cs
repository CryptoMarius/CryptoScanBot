using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Mexc.Net.Clients;
using Mexc.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

/// <summary>
/// Fetch candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
#if MEXCDEBUG
    private static int tickerIndex = 0;
#endif


    private static async Task<long> GetCandlesForInterval(MexcRestClient client, CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval, long fetchEndUnix)
    {
        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            return 0;

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";


        // Fetch candles, sorting is not guaranteed (its even recersed on bybit)
        DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        while (true) // only for error
        {
            int exchangeLimit = 500;
            DateTime dateEnd = dateStart.AddSeconds(exchangeLimit * interval.Duration); // To create a valid date period
            var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, (KlineInterval)exchangeInterval, startTime: dateStart, endTime: dateEnd, limit: exchangeLimit);
            //GlobalData.AddTextToLogTab($"Debug: {symbol.Name} {interval.Name} volume={symbol.Volume} start={dateStart} end={dateEnd} url={result.RequestUrl}");
            if (!result.Success)
            {
                // This is based on the kucoin error number,, does Mexc have an error for overloading the exchange as wel?
                // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429: Too Many Requests
                if (result.Error?.Code == 429) // not sure if this error exists on Mexc? Copied?
                {
                    GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                    Thread.Sleep(10000); // We just retry after a delay
                    continue;
                }
                // Might do something better than this
                GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
                return 0;
            }


            // Might have problems with no internet etc.
            if (result == null || result.Data == null)
            {
                GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no data received");
                return 0;
            }

            // Remember for reporting
            long? startFetchDate = symbolInterval.LastCandleSynchronized;

            await symbol.CandleLock.WaitAsync();
            try
            {
                if (result.Data.Any())
                {
                    long last = long.MinValue;
                    foreach (var kline in result.Data)
                    {
                        // Add candle & overwriting all previous data
                        CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                            kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.QuoteVolume, false);

                        //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);
                        // Remember the last candle
                        // We assume we have fetched all candles up to this point in time
                        // (FYI: Sorting is not guaranteed (its even recersed on bybit))
                        if (candle.OpenTime > last)
                            last = candle.OpenTime;
                    }
                    symbolInterval.LastCandleSynchronized = last + interval.Duration; // For the next session && the + saves retrieving 1 candle)
                }
                else
                {
                    // Assume there are no candles available in the requested date period ()
                    GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
                    symbolInterval.LastCandleSynchronized = CandleTools.GetUnixTime(dateEnd, interval.Duration) + interval.Duration;
                }


#if MEXCDEBUG
                // Debug, wat je al niet moet doen voor een exchange...
                Interlocked.Increment(ref tickerIndex);
                long unix = CandleTools.GetUnixTime(DateTime.UtcNow, 0);
                string filename = $@"{GlobalData.GetBaseDir()}\{Api.ExchangeOptions.ExchangeName}\Candles-{symbol.Name}-{interval.Name}-{unix}-#{tickerIndex}.json";
                string text = System.Text.Json.JsonSerializer.Serialize(result, GlobalData.JsonSerializerIndented);
                File.WriteAllText(filename, text);
#endif
            }
            finally
            {
                symbol.CandleLock.Release();
            }


            GlobalData.AddTextToLogTab($"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from UTC {CandleTools.GetUnixDate(startFetchDate).ToLocalTime()} .. " +
                $"{CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized).ToLocalTime()} UTC received: {result.Data.Count()} total {symbolInterval.CandleList.Count}");
            return result.Data.Count();
        }
    }


    public async Task GetCandlesForIntervalAsync(IDisposable? clientBase, CryptoSymbol symbol, CryptoInterval interval, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData!.FetchCandles)
            return;

        // Weird piece of code, unable todo: (!clientBase is MexcRestClient client1)
        MexcRestClient client;
        if (clientBase is MexcRestClient client1)
            client = client1;
        else
            throw new Exception("Expected MexcRestClient");

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        bool intervalSupported = Interval.GetExchangeInterval(interval.IntervalPeriod) != null;
        if (intervalSupported)
        {
            // Fetch the candles
            while (symbolInterval.LastCandleSynchronized < fetchEndUnix)
            {
                if (symbolInterval.LastCandleSynchronized + interval.Duration > fetchEndUnix)
                    break;

                long lastDate = (long)symbolInterval.LastCandleSynchronized;
                //DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                //GlobalData.AddTextToLogTab("Debug: Fetching " + symbol.Name + " " + interval.Name + " " + dateStart.ToLocalTime());


                // Nothing more? (we have coins stopping, beaware for endless loops)
                long candleCount = await GetCandlesForInterval(client, symbol, interval, symbolInterval, fetchEndUnix);

                if (candleCount == 0 && symbolInterval.LastCandleSynchronized > fetchEndUnix)
                    symbolInterval.LastCandleSynchronized = fetchEndUnix; // reset
                CandleTools.UpdateCandleFetched(symbol, interval);

                if (candleCount == 0 && symbolInterval.LastCandleSynchronized == lastDate)
                    break;
            }
        }


        await symbol.CandleLock.WaitAsync();
        try
        {
            // Add missing candles (the only place we know it can be done safely)
            CandleTools.BulkAddMissingCandles(symbol, interval);

            // Bulk calculate the higher interval candles
            if (interval.IntervalPeriod < Enum.GetValues(typeof(CryptoIntervalPeriod)).Cast<CryptoIntervalPeriod>().Last())
            {
                CryptoInterval targetInterval = GlobalData.IntervalListPeriod[interval.IntervalPeriod + 1];
                CryptoInterval sourceInterval = targetInterval.ConstructFrom!;
                CandleTools.BulkCalculateCandles(symbol, sourceInterval, targetInterval, fetchEndUnix);
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }


        // Adjust the administration for the not supported interval's
        if (!intervalSupported && symbolInterval.CandleList.Count > 0)
        {
            CryptoCandle candle = symbolInterval.CandleList.Values.Last();
            symbolInterval.LastCandleSynchronized = candle.OpenTime + symbolInterval.Interval.Duration;
        }
    }

    public async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        using MexcRestClient client = new();
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            await GetCandlesForIntervalAsync(client, symbol, interval, fetchEndUnix);
        }


        // Remove the candles we needed because of the not supported intervals & bulk calculation
        await CandleTools.CleanCandleDataAsync(symbol, null);
    }


}