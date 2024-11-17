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
    public List<ZigZagIndicator9>? ZigZagIndicators { get; set; } = null;
    public ZigZagIndicator9? BestIndicator { get; set; } = null;
    public ZigZagIndicator9? FibIndicator { get; set; } = null;

    // Administration liquidity zones calculation
    //public DateTime? LastZigZagPoint { get; set; } = null;
    //public bool? NeedsZoneCalculation { get; set; } = true;


    public void Reset()
    {
        TrendInfoUnix = null;
        TrendInfoDate = null;
        TrendIndicator = CryptoTrendIndicator.Sideways;

        ZigZagLastCandleAdded = null;
        ZigZagIndicators = null;
        BestIndicator = null;
        FibIndicator = null;

        //LastZigZagPoint = null;
        //NeedsZoneCalculation = true;
    }

}