using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Kraken.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class Interval
{
    public static KlineInterval? GetExchangeInterval(CryptoIntervalPeriod interval)
    {
        return interval switch
        {
            CryptoIntervalPeriod.interval1m => KlineInterval.OneMinute,
            CryptoIntervalPeriod.interval5m => KlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => KlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => KlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => KlineInterval.OneHour,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHour,
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
        // Calculate what we need for the (full) calculation of the indicators (and markettrend)
        long[] fetchFrom = new long[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            long startFromUnixTime = CandleIndicatorData.GetCandleFetchStart(symbol, interval, fetchEndDate);
            fetchFrom[(int)interval.IntervalPeriod] = startFromUnixTime;
        }


        // If the exchange does not support the interval than retrieve more 
        // candles from a lower timeframe so we can calculate the candles.
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoInterval? lowerInterval = interval;
            while (GetExchangeInterval(lowerInterval.IntervalPeriod) == null)
            {
                lowerInterval = lowerInterval.ConstructFrom;
                long startFromUnixTime = fetchFrom[(int)interval!.IntervalPeriod];
                if (startFromUnixTime < fetchFrom[(int)lowerInterval!.IntervalPeriod])
                    fetchFrom[(int)lowerInterval!.IntervalPeriod] = startFromUnixTime;
            }
        }


        // Correct the (worst case) startdate with what we previously collected..
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                long alreadyFetched = (long)symbolInterval.LastCandleSynchronized;
                // Huray, retrieve less candles, less work, more free time
                if (alreadyFetched > fetchFrom[(int)interval.IntervalPeriod])
                    fetchFrom[(int)interval.IntervalPeriod] = alreadyFetched;
            }
            symbolInterval.LastCandleSynchronized = fetchFrom[(int)interval.IntervalPeriod];
        }

        return fetchFrom;
    }

}
