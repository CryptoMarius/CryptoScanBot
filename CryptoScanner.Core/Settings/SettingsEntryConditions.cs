namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsEntryConditions
{
    public bool CheckIncreasingRsi { get; set; } = false;
    public bool CheckIncreasingMacd { get; set; } = false;
    public bool CheckIncreasingStoch { get; set; } = false;
    public bool CheckFurtherPriceMove { get; set; } = false;

    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    public bool CheckPriceAboveMa200 { get; set; } = false;
    public decimal Ma200MinDistancePercentage { get; set; } = 0m;
    public int Ma200ConfirmationCandles { get; set; } = 0;

    public bool WaitForStochRecovery { get; set; } = false;
    public bool WaitForRsiRecovery { get; set; } = false;

    public int StochExtremeLookback { get; set; } = 20;
    public int StochMinExtremeBars { get; set; } = 0;
    public decimal StochMinExtremeArea { get; set; } = 0m;
    public decimal StochMinExtremeZScore { get; set; } = 0m;

    // The multi-timeframe band-break confirmation used to live here as TimeframeConsensusCount, but
    // only the three band strategies could act on it while every strategy showed the field. It now
    // sits in the settings of those strategies as BandBreakConfirmationCount.
}
