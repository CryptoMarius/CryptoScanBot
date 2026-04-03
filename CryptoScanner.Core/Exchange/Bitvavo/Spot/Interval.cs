using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot;

public class Interval
{
    // Bitvavo supported intervals: 1m, 5m, 15m, 30m, 1h, 2h, 4h, 6h, 8h, 12h, 1d, 1w
    public static string? GetExchangeInterval(CryptoIntervalPeriod interval)
    {
        return interval switch
        {
            CryptoIntervalPeriod.interval1m  => "1m",
            CryptoIntervalPeriod.interval5m  => "5m",
            CryptoIntervalPeriod.interval15m => "15m",
            CryptoIntervalPeriod.interval30m => "30m",
            CryptoIntervalPeriod.interval1h  => "1h",
            CryptoIntervalPeriod.interval2h  => "2h",
            CryptoIntervalPeriod.interval4h  => "4h",
            CryptoIntervalPeriod.interval6h  => "6h",
            CryptoIntervalPeriod.interval8h  => "8h",
            CryptoIntervalPeriod.interval12h => "12h",
            CryptoIntervalPeriod.interval1d  => "1d",
            CryptoIntervalPeriod.interval1w  => "1w",
            _ => null,
        };
    }
}
