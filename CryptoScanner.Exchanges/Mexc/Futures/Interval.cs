using CryptoScanner.Core.Enums;

using Mexc.Net.Enums;

namespace CryptoScanner.Core.Exchange.Mexc.Futures;

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
            // The futures side has an 8 hour candle where the spot side has none
            CryptoIntervalPeriod.interval8h => FuturesKlineInterval.EightHours,
            CryptoIntervalPeriod.interval1d => FuturesKlineInterval.OneDay,
            CryptoIntervalPeriod.interval1w => FuturesKlineInterval.OneWeek,
            _ => null,
        };
    }

}
