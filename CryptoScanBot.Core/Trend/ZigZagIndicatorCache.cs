using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Trend;

public class ZigZagIndicatorCache
{
    public long? LastCandleAdded { get; set; }
    public ZigZagIndicator Indicator = new();
}
