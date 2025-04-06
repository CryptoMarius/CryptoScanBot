namespace CryptoScanBot.Core.Model;

public class CryptoSymbolIntervalZoneCalc
{
    // Automaticly calculate zones
    // Based on the primary trend, recalculate if price is outside this range
    public long? TimeLastSwingPoint { get; set; }
    public decimal? LastSwingHigh { get; internal set; } = null;
    public decimal? LastSwingLow { get; internal set; } = null;

    public void Reset()
    {
        LastSwingLow = null;
        LastSwingHigh = null;
        TimeLastSwingPoint = null;
    }
}
