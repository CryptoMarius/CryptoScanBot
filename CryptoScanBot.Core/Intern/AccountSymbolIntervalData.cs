using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Intern;

public class AccountSymbolIntervalData
{
    public required virtual CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }


    // The last calculated trend & generated date
    public CryptoTrendIndicator TrendIndicator { get; set; }
    public DateTime? TrendInfoDate { get; set; }
    public long? TrendInfoUnix { get; set; }
    // Caching ZigZag indicator because of emulator speed
    public ZigZagIndicatorCache? ZigZagCache { get; set; }


    public void Reset()
    {
        TrendIndicator = CryptoTrendIndicator.Sideways;
        TrendInfoDate = null;
        TrendInfoUnix = null;
        ZigZagCache = null;
    }
}

