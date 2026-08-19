using BitMart.Net.Enums;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange.BitMart.Futures;

public class Interval
{
    public static FuturesKlineInterval? GetExchangeInterval(CryptoIntervalPeriod interval)
    {
        return interval switch
        {
            CryptoIntervalPeriod.interval1m => FuturesKlineInterval.OneMinute,
            CryptoIntervalPeriod.interval3m => FuturesKlineInterval.ThreeMinutes,
            CryptoIntervalPeriod.interval5m => FuturesKlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => FuturesKlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => FuturesKlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => FuturesKlineInterval.OneHour,
            CryptoIntervalPeriod.interval2h => FuturesKlineInterval.TwoHours,
            CryptoIntervalPeriod.interval4h => FuturesKlineInterval.FourHours,
            CryptoIntervalPeriod.interval1d => FuturesKlineInterval.OneDay,
            CryptoIntervalPeriod.interval1w => FuturesKlineInterval.OneWeek,
            _ => null
        };
    }

}
