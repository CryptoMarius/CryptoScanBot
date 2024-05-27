using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Kucoin.Net.Enums;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;

public class Interval
{
    public static KlineInterval GetExchangeInterval(CryptoInterval interval)
    {
        var binanceInterval = interval.IntervalPeriod switch
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
            CryptoIntervalPeriod.interval8h => KlineInterval.EightHours,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHours,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            //case IntervalPeriod.interval1w:
            //    binanceInterval = KlineInterval.OneWeek;
            //    break;
            _ => KlineInterval.OneWeek,// Ten teken dat het niet ondersteund wordt
        };
        return binanceInterval;
    }
}
