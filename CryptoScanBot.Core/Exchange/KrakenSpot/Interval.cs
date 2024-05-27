using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Kraken.Net.Enums;

namespace CryptoScanBot.Core.Exchange.KrakenSpot;

public class Interval
{
    public static KlineInterval GetExchangeInterval(CryptoInterval interval)
    {
        var binanceInterval = interval.IntervalPeriod switch
        {
            CryptoIntervalPeriod.interval1m => KlineInterval.OneMinute,
            CryptoIntervalPeriod.interval5m => KlineInterval.FiveMinutes,
            CryptoIntervalPeriod.interval15m => KlineInterval.FifteenMinutes,
            CryptoIntervalPeriod.interval30m => KlineInterval.ThirtyMinutes,
            CryptoIntervalPeriod.interval1h => KlineInterval.OneHour,
            CryptoIntervalPeriod.interval4h => KlineInterval.FourHour,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            //case IntervalPeriod.interval1w:
            //    binanceInterval = KlineInterval.OneWeek;
            //    break;
            _ => KlineInterval.FifteenDays,// Ten teken dat het niet ondersteund wordt
        };
        return binanceInterval;
    }
}
