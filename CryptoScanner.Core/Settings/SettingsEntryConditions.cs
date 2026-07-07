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

    public bool WaitForStochRecovery { get; set; } = false;
    public bool WaitForRsiRecovery { get; set; } = false;

    public int StochExtremeLookback { get; set; } = 20;
    public int StochMinExtremeBars { get; set; } = 0;
    public decimal StochMinExtremeArea { get; set; } = 0m;
    public decimal StochMinExtremeZScore { get; set; } = 0m;
}
