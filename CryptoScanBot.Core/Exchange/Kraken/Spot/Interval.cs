using CryptoScanBot.Core.Enums;

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

}
