using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Account;

public class AccountSymbolIntervalData
{
    public required virtual CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // Trend: Last calculated trend & date
    public long? TrendInfoUnix { get; set; }
    public DateTime? TrendInfoDate { get; set; }
    public CryptoTrendIndicator TrendIndicator { get; set; }


    // Zones: Primary trend, recalculated if outside
    // Administration liquidity zones calculation?? How?
    public long? TimeLastSwingPoint { get; set; }

    public decimal? BestLongZone { get; internal set; } = 100m; // distance%
    public decimal? LastSwingHigh { get; internal set; } = null;

    public decimal? BestShortZone { get; internal set; } = 100m; // distance%
    public decimal? LastSwingLow { get; internal set; } = null;


    public void ResetZoneData()
    {
        LastSwingLow = null;
        LastSwingHigh = null;
        TimeLastSwingPoint = null;
    }

    public void ResetTrendData()
    {
        TrendInfoUnix = null;
        TrendInfoDate = null;
        TrendIndicator = CryptoTrendIndicator.Sideways;
    }
}