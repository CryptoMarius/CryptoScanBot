using Alpaca.Markets;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

public class Interval
{
    public static BarTimeFrame? GetExchangeInterval(CryptoIntervalPeriod interval)
    {
        return interval switch
        {
            CryptoIntervalPeriod.interval1m => new BarTimeFrame(1, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval2m => new BarTimeFrame(2, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval3m => new BarTimeFrame(3, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval5m => new BarTimeFrame(5, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval10m => new BarTimeFrame(10, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval15m => new BarTimeFrame(15, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval30m => new BarTimeFrame(30, BarTimeFrameUnit.Minute),
            CryptoIntervalPeriod.interval1h => new BarTimeFrame(1, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval2h => new BarTimeFrame(2, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval3h => new BarTimeFrame(3, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval4h => new BarTimeFrame(4, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval6h => new BarTimeFrame(6, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval8h => new BarTimeFrame(8, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval12h => new BarTimeFrame(12, BarTimeFrameUnit.Hour),
            CryptoIntervalPeriod.interval1d => new BarTimeFrame(1, BarTimeFrameUnit.Day),
            CryptoIntervalPeriod.interval1w => new BarTimeFrame(1, BarTimeFrameUnit.Week),
            _ => null,
        };
    }
}
