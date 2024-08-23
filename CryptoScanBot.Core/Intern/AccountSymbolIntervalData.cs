using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Intern;

public class AccountSymbolIntervalData
{
    public required virtual CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // The last calculated trend & generated date
    public long? TrendInfoUnix { get; set; }
    public DateTime? TrendInfoDate { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }

    // Caching ZigZag indicator because of emulator speed
    public long? ZigZagLastCandleAdded { get; set; }
    public List<ZigZagIndicator7>? ZigZagIndicators = null;

    public void Reset()
    {
        TrendInfoUnix = null;
        TrendInfoDate = null;
        TrendIndicator = CryptoTrendIndicator.Sideways;

        ZigZagIndicators = null;
        ZigZagLastCandleAdded = null;
    }


}