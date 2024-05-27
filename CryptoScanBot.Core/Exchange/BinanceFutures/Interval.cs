using Binance.Net.Enums;

using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.BinanceFutures;

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
            CryptoIntervalPeriod.interval2h => KlineInterval.TwoHour,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHour,
            CryptoIntervalPeriod.interval6h => KlineInterval.SixHour,
            CryptoIntervalPeriod.interval8h => KlineInterval.EightHour,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHour,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            //case IntervalPeriod.interval1w:
            //    binanceInterval = KlineInterval.OneWeek;
            //    break;
            _ => KlineInterval.OneMonth,// Ten teken dat het niet ondersteund wordt
        };
        return binanceInterval;
    }
}
