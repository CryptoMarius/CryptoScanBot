using CryptoExchange.Net.Objects.Errors;
using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Market;

namespace CryptoScanner.Core.Exchange.Okx.Futures;

/// <summary>
/// Fetch klines/candles from the exchange
/// </summary>
public class Candle(ExchangeBase api) : CandleBase(api), ICandle
{
    public async Task<CandleTime> GetCandlesForInterval(IDisposable clientBase, 
        CryptoSymbol symbol, CryptoInterval interval, CandleTime minFetch, CandleTime maxFetch)
    {
        // Remarks:
        // The maximum is 1000 candles per GetKlinesAsync call.
        // The results can be from new to old (wrong order).
        // The results can contain in progress candles.

        // Weird piece of code, unable todo: (!clientBase is BinanceRestClient client1)
        OKXRestClient client;
        if (clientBase is OKXRestClient client1)
            client = client1;
        else
            throw new Exception("Expected OKXRestClient");

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        KlineInterval? exchangeInterval = Interval.GetExchangeInterval(interval.IntervalPeriod);
        if (exchangeInterval == null)
            throw new Exception($"Not supported interval");

        LimitRate.WaitForFairWeight(1);
        string prefix = $"{ExchangeBase.ExchangeOptions.ExchangeName} {symbol.Name} {interval!.Name}";

        CandleTime minTime = minFetch;
        DateTime minDate = minTime.ToDateTime();
        CandleTime maxTime = minTime + (Api.ExchangeOptions.CandleLimit - 1) * interval.Duration;
        DateTime maxDate = maxTime.ToDateTime();

        //string symbolName = OkxExchange.FormatSymbol(symbol.Base, symbol.Quote, TradingMode.PerpetualLinear);
        string symbolName = symbol.Base + '-' + symbol.Quote;
    Again:
        var result = await client.UnifiedApi.ExchangeData.GetKlinesAsync(symbolName, (KlineInterval)exchangeInterval,
            startTime: minDate, endTime: maxDate, limit: Api.ExchangeOptions.CandleLimit);
        if (!result.Success && result.Error?.ErrorType == ErrorType.RateLimitRequest)
        {
            GlobalData.AddTextToLogTab($"{prefix} delay needed because of rate limits");
            Thread.Sleep(15000);
            //continue;
            goto Again;
        }
        if (!result.Success)
        {
            GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
#if DEBUG
            SaveCandleInfo(result, $"candles {symbol.Base}-{symbol.Quote} {interval.Name} no succes.json");
#endif
            return minFetch;
        }


        // Might have problems with no internet etc.
        if (result.Data == null)
        {
            GlobalData.AddTextToLogTab($"{prefix} fetch from {minFetch.ToDateTime()} no candles received");
#if DEBUG
            SaveCandleInfo(result, $"candles {symbol.Base}-{symbol.Quote} {interval.Name} no data.json");
#endif
            return minFetch;
        }


        CandleTime fetchedUpTo = CandleTime.MinValue;
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var kline in result.Data)
            {
                if (symbolInterval.IntervalPeriod != CryptoIntervalPeriod.interval1m)
                {
                    CandleTime unix = CandleTime.AlignFromDateTime(kline.Time, 1);
                    if (unix + symbolInterval.Interval.Duration > maxFetch) // future candle?
                        continue;
                }

                CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.Time,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 
                    kline.VolumeCurrencyQuote);

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
                if (maxTime > maxFetch)
                    fetchedUpTo = maxFetch;
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
        string s = $"{symbol.Exchange.Name} {symbol.Name} {interval.Name} fetch from {minDate.ToLocalTime()} .. {fetchedUpTo.ToDateTime().ToLocalTime()}";
        GlobalData.AddTextToLogTab($"{s} received: {count} total: {candles.Count}");
        return fetchedUpTo;
    }

}
