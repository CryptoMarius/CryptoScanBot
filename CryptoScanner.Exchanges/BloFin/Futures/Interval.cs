using BloFin.Net.Enums;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange.BloFin.Futures;

public class Interval
{
    public static KlineInterval? GetExchangeInterval(CryptoIntervalPeriod interval)
    {
        return interval switch
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
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            CryptoIntervalPeriod.interval1w => KlineInterval.OneWeek,
            _ => null
        };
    }
}
