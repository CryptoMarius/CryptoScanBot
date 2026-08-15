using CryptoScanner.Core.Settings;

namespace CryptoScanner.UI.Models;

/// <summary>
/// Editable snapshot of one <see cref="SettingsEntryConditions"/>, so the same editor can serve the
/// global trader conditions and the per-strategy override — exactly as Avalonia reuses
/// TraderEntryConditionsView inside StrategyEntryConditionsView.
/// </summary>
public class EntryConditionsData
{
    public bool CheckIncreasingRsi { get; set; }
    public bool WaitForRsiRecovery { get; set; }
    public bool CheckIncreasingStoch { get; set; }
    public bool WaitForStochRecovery { get; set; }

    public int StochExtremeLookback { get; set; }
    public int StochMinExtremeBars { get; set; }
    public decimal StochMinExtremeArea { get; set; }
    public decimal StochMinExtremeZScore { get; set; }

    public bool CheckIncreasingMacd { get; set; }
    public bool CheckFurtherPriceMove { get; set; }
    public bool CheckTrendPrimaryDirection { get; set; }
    public int TrendPrimaryDirectionCount { get; set; }
    public bool CheckTrendSecondaryDirection { get; set; }
    public int TrendSecondaryDirectionCount { get; set; }
    public bool CheckPriceAboveMa200 { get; set; }
    public decimal Ma200MinDistancePercentage { get; set; }
    public int Ma200ConfirmationCandles { get; set; }

    public void LoadFrom(SettingsEntryConditions e)
    {
        CheckIncreasingRsi = e.CheckIncreasingRsi;
        WaitForRsiRecovery = e.WaitForRsiRecovery;
        CheckIncreasingStoch = e.CheckIncreasingStoch;
        WaitForStochRecovery = e.WaitForStochRecovery;

        StochExtremeLookback = e.StochExtremeLookback;
        StochMinExtremeBars = e.StochMinExtremeBars;
        StochMinExtremeArea = e.StochMinExtremeArea;
        StochMinExtremeZScore = e.StochMinExtremeZScore;

        CheckIncreasingMacd = e.CheckIncreasingMacd;
        CheckFurtherPriceMove = e.CheckFurtherPriceMove;
        CheckTrendPrimaryDirection = e.CheckTrendPrimaryDirection;
        TrendPrimaryDirectionCount = e.TrendPrimaryDirectionCount;
        CheckTrendSecondaryDirection = e.CheckTrendSecondaryDirection;
        TrendSecondaryDirectionCount = e.TrendSecondaryDirectionCount;
        CheckPriceAboveMa200 = e.CheckPriceAboveMa200;
        Ma200MinDistancePercentage = e.Ma200MinDistancePercentage;
        Ma200ConfirmationCandles = e.Ma200ConfirmationCandles;
    }

    public void SaveTo(SettingsEntryConditions e)
    {
        e.CheckIncreasingRsi = CheckIncreasingRsi;
        e.WaitForRsiRecovery = WaitForRsiRecovery;
        e.CheckIncreasingStoch = CheckIncreasingStoch;
        e.WaitForStochRecovery = WaitForStochRecovery;

        e.StochExtremeLookback = StochExtremeLookback;
        e.StochMinExtremeBars = StochMinExtremeBars;
        e.StochMinExtremeArea = StochMinExtremeArea;
        e.StochMinExtremeZScore = StochMinExtremeZScore;

        e.CheckIncreasingMacd = CheckIncreasingMacd;
        e.CheckFurtherPriceMove = CheckFurtherPriceMove;
        e.CheckTrendPrimaryDirection = CheckTrendPrimaryDirection;
        e.TrendPrimaryDirectionCount = TrendPrimaryDirectionCount;
        e.CheckTrendSecondaryDirection = CheckTrendSecondaryDirection;
        e.TrendSecondaryDirectionCount = TrendSecondaryDirectionCount;
        e.CheckPriceAboveMa200 = CheckPriceAboveMa200;
        e.Ma200MinDistancePercentage = Ma200MinDistancePercentage;
        e.Ma200ConfirmationCandles = Ma200ConfirmationCandles;
    }
}
