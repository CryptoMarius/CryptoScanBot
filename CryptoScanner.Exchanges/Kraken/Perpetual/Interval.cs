using CryptoScanner.Core.Enums;

using Kraken.Net.Enums;

namespace CryptoScanner.Core.Exchange.Kraken.Perpetual;

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
            // Not taken from the exchange: it starts its week on Thursday 00:00 UTC, the scanner on
            // Monday 00:00 UTC. Both kinds ended up side by side in the candle store, so the week
            // is derived from 1d instead, as for an exchange without 1w.
            //CryptoIntervalPeriod.interval1w => FuturesKlineInterval.OneWeek,
            _ => null,
        };
    }

}
