using Mexc.Net.Enums;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Mexc.Spot;

public class Interval
{
    public static KlineInterval? GetExchangeInterval(CryptoInterval interval)
    {
        return interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => KlineInterval.OneMinute,
            CryptoIntervalPeriod.interval5m => KlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => KlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => KlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => KlineInterval.OneHour,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHours,
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
            CryptoInterval? loopInterval = interval;
            while (GetExchangeInterval(loopInterval) == null)
            {
                // Retrieve more candles from a lower timeframe
                loopInterval = loopInterval.ConstructFrom;

                // Extend if we need more (because of the interval nog being supported on the exchange)
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
                // Huray, retrieve less candles
                if (alreadyFetched > fetchFrom[(int)interval.IntervalPeriod])
                    fetchFrom[(int)interval.IntervalPeriod] = alreadyFetched;
            }
            symbolInterval.LastCandleSynchronized = fetchFrom[(int)interval.IntervalPeriod];
        }

        return fetchFrom;
    }


    //private static CryptoInterval GetConstructFromInterval(CryptoInterval intervalStart)
    //{
    //    // Some intervals are not supported on the exchange, so we need to calculate it
    //    long durationStart = intervalStart.Duration;

    //    int index = GlobalData.IntervalList.IndexOf(intervalStart);
    //    for (int i = index - 1; i >= 0; i--)
    //    {
    //        CryptoInterval interval = GlobalData.IntervalList[i];

    //        if (GetExchangeInterval(interval) != null && durationStart % interval.Duration == 0)
    //            return interval;
    //    }

    //    return GlobalData.IntervalList[0]; // 1m
    //}


}
