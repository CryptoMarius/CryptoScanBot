using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Account;

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
    public List<ZigZagIndicator9>? ZigZagIndicators { get; set; }
    public ZigZagIndicator9? BestZigZagIndicator { get; set; }
    //public ZigZagIndicator9? FibIndicator { get; set; }

    // Administration liquidity zones calculation?? How?
    public long? TimeLastSwingPoint { get; set; }
    public decimal? BestLongZone { get; internal set; }
    public decimal? BestShortZone { get; internal set; }

    //public bool? NeedsZoneCalculation { get; set; } = true;


    public void Reset()
    {
        TrendInfoUnix = null;
        TrendInfoDate = null;
        TrendIndicator = CryptoTrendIndicator.Sideways;

        ZigZagLastCandleAdded = null;
        ZigZagIndicators = null;
        BestZigZagIndicator = null;
        //FibIndicator = null;

        //TimeLastSwingPoint = null;
        //NeedsZoneCalculation = true;
    }

}