using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Kucoin.Futures;

public class Interval
{
    public static FuturesKlineInterval? GetExchangeInterval(CryptoInterval interval)
    {
        return interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => FuturesKlineInterval.OneMinute,
            //CryptoIntervalPeriod.interval3m => FuturesKlineInterval.ThreeMinutes,
            CryptoIntervalPeriod.interval5m => FuturesKlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => FuturesKlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => FuturesKlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => FuturesKlineInterval.OneHour,
            CryptoIntervalPeriod.interval2h => FuturesKlineInterval.TwoHours,
            CryptoIntervalPeriod.interval4h => FuturesKlineInterval.FourHours,
            //CryptoIntervalPeriod.interval6h => FuturesKlineInterval.SixHours,
            CryptoIntervalPeriod.interval8h => FuturesKlineInterval.EightHours,
            CryptoIntervalPeriod.interval12h => FuturesKlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => FuturesKlineInterval.OneDay,
            _ => null,
        };
    }


    /// <summary>
    /// Determine the startdate per interval
    /// </summary>
    public static long[] DetermineFetchStartDate(CryptoSymbol symbol, long fetchEndUnix)
    {
        DateTime fetchEndDate = CandleTools.GetUnixDate(fetchEndUnix);

        long[] fetchFrom = new long[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];


        // Determine the maximum startdate per interval
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // Calculate date what we need for the calculation of indicators (and markettrend)
            long startFromUnixTime = CandleIndicatorData.GetCandleFetchStart(symbol, interval, fetchEndDate);
            fetchFrom[(int)interval.IntervalPeriod] = startFromUnixTime;
        }


        // If the exchange does not support the interval than retrieve more 
        // candles from a lower timeframe so we can calculate the candles.
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoInterval? loopInterval = interval;
            while (Interval.GetExchangeInterval(loopInterval) == null)
            {
                // Retrieve more candles from a lower timeframe
                loopInterval = loopInterval.ConstructFrom;

                // Calculate date what we need for the calculation of indicators (and markettrend)
                long startFromUnixTime = CandleIndicatorData.GetCandleFetchStart(symbol, loopInterval!, fetchEndDate);
                fetchFrom[(int)loopInterval!.IntervalPeriod] = startFromUnixTime;
            }
        }


        // Correct the (worst case scenario) startdate with what we previously collected..
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                long alreadyFetched = (long)symbolInterval.LastCandleSynchronized;
                if (alreadyFetched > fetchFrom[(int)interval.IntervalPeriod])
                    fetchFrom[(int)interval.IntervalPeriod] = alreadyFetched;
            }
            symbolInterval.LastCandleSynchronized = fetchFrom[(int)interval.IntervalPeriod];
        }

        return fetchFrom;
    }

}
