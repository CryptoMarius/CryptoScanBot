using CryptoScanner.Core.Enums;

using HyperLiquid.Net.Enums;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Spot;

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
            CryptoIntervalPeriod.interval8h => KlineInterval.EightHours,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            // Not taken from the exchange: HyperLiquid starts its week on Thursday 00:00 UTC, the
            // scanner on Monday 00:00 UTC. Both kinds ended up side by side in the candle store, so
            // the week is derived from 1d instead, as for an exchange without 1w.
            //CryptoIntervalPeriod.interval1w => KlineInterval.OneWeek,
            _ => null,
        };
    }

}
