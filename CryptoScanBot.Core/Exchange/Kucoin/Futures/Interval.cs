using CryptoScanBot.Core.Enums;
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

}
