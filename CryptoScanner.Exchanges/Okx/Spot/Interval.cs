using CryptoScanner.Core.Enums;

using OKX.Net.Enums;

namespace CryptoScanner.Core.Exchange.Okx.Spot;

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
            // The UTC variants: the plain 6H, 12H, 1D and 1W of Okx start at 00:00 in UTC+8 (16:00
            // UTC), off the scanner's grid, and they ended up beside the scanner's own candles in the
            // candle store. The Utc ones start where the scanner does (1Wutc on Monday 00:00 UTC).
            //CryptoIntervalPeriod.interval6h => KlineInterval.SixHours,
            //CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHours,
            //CryptoIntervalPeriod.interval1d => KlineInterval.OneDay,
            //CryptoIntervalPeriod.interval1w => KlineInterval.OneWeek,
            CryptoIntervalPeriod.interval6h => KlineInterval.SixHoursUtc,
            CryptoIntervalPeriod.interval12h => KlineInterval.TwelveHoursUtc,
            CryptoIntervalPeriod.interval1d => KlineInterval.OneDayUtc,
            CryptoIntervalPeriod.interval1w => KlineInterval.OneWeekUtc,
            _ => null,
        };
    }

}
