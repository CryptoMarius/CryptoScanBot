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

    // Multi-timeframe consensus: when > 0 the signal also requires this many consecutive higher
    // timeframes to confirm the same band break condition. 0 = single-timeframe (normal behavior).
    public int TimeframeConsensusCount { get; set; } = 0;
}
