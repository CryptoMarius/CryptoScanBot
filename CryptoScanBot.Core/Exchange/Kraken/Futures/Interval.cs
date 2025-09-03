using CryptoScanBot.Core.Enums;

using Kraken.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Kraken.Futures;

public class Interval
{
    public static FuturesKlineInterval? GetExchangeInterval(CryptoIntervalPeriod interval)
    {
        return interval switch
        {
            CryptoIntervalPeriod.interval1m => FuturesKlineInterval.OneMinute,
            CryptoIntervalPeriod.interval5m => FuturesKlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => FuturesKlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => FuturesKlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => FuturesKlineInterval.OneHour,
            CryptoIntervalPeriod.interval4h => FuturesKlineInterval.FourHours,
            CryptoIntervalPeriod.interval1d => FuturesKlineInterval.OneDay,
            CryptoIntervalPeriod.interval1w => FuturesKlineInterval.OneWeek,
            _ => null,
        };
    }

}
