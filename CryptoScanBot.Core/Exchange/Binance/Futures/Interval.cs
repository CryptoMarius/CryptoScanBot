using Binance.Net.Enums;
using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

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
            CryptoIntervalPeriod.interval2h => KlineInterval.TwoHour,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHour,
            CryptoIntervalPeriod.interval6h => KlineInterval.SixHour,
            CryptoIntervalPeriod.interval8h => KlineInterval.EightHour,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHour,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            _ => null,
        };
    }

}
