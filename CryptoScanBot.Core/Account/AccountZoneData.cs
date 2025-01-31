namespace CryptoScanBot.Core.Account;

public class AccountZoneData
{
    // Automaticly calculate zones
    // Based on the primary trend, recalculate if price is outside this range
    public long? TimeLastSwingPoint { get; set; }
    public decimal? LastSwingHigh { get; internal set; } = null;
    public decimal? LastSwingLow { get; internal set; } = null;

    // Display only (an initial hidden column in the symbol grid)
    // These are the closest zones (calculated from all the AccountInterval zones)
    public decimal? BestLongZone { get; internal set; } = 100m; // distance%
    public decimal? BestShortZone { get; internal set; } = 100m; // distance%

    public void ResetSwingPointData()
    {
        LastSwingLow = null;
        LastSwingHigh = null;
        TimeLastSwingPoint = null;
    }

}