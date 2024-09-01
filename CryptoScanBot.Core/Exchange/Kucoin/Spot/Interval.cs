using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;

public class Interval
{
    public static KlineInterval? GetExchangeInterval(CryptoInterval interval)
    {
        return interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => KlineInterval.OneMinute,
            CryptoIntervalPeriod.interval3m => KlineInterval.ThreeMinutes,
            CryptoIntervalPeriod.interval5m => KlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => KlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => KlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => KlineInterval.OneHour,
            CryptoIntervalPeriod.interval2h => KlineInterval.TwoHours,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHours,
            CryptoIntervalPeriod.interval6h => KlineInterval.SixHours,
            CryptoIntervalPeriod.interval8h => KlineInterval.EightHours,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            _ => null,
        };
    }



    /// <summary>
    /// Determine the startdate per interval
    /// </summary>
    public static long[] DetermineFetchStartDate(CryptoSymbol symbol, long fetchEndUnix)
    {
        // TODO: Find a better place, problem is the method "Interval.GetExchangeInterval" which is exchange specific
        DateTime fetchEndDate = CandleTools.GetUnixDate(fetchEndUnix);

        // Determine the maximum startdate per interval
        long[] fetchFrom = new long[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // Calculate date what we need for the (full) calculation of the indicators (and markettrend)
            long startFromUnixTime = CandleIndicatorData.GetCandleFetchStart(symbol, interval, fetchEndDate);
            fetchFrom[(int)interval.IntervalPeriod] = startFromUnixTime;
        }


        // If the exchange does not support the interval than retrieve more 
        // candles from a lower timeframe so we can calculate the candles.
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoInterval? lowerInterval = interval;
            while (GetExchangeInterval(lowerInterval) == null)
            {
                // Retrieve more candles from a lower timeframe
                lowerInterval = lowerInterval.ConstructFrom;

                // Extend if we need more (because of the interval nog being supported on the exchange)
                long startFromUnixTime = CandleIndicatorData.GetCandleFetchStart(symbol, lowerInterval!, fetchEndDate);
                fetchFrom[(int)lowerInterval!.IntervalPeriod] = startFromUnixTime;
            }
        }


        // Correct the (worst case scenario) startdate with what we previously collected..
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                long alreadyFetched = (long)symbolInterval.LastCandleSynchronized;
                // Huray, retrieve less candles
                if (alreadyFetched > fetchFrom[(int)interval.IntervalPeriod])
                    fetchFrom[(int)interval.IntervalPeriod] = alreadyFetched;
            }
            symbolInterval.LastCandleSynchronized = fetchFrom[(int)interval.IntervalPeriod];
        }

        return fetchFrom;
    }

}
