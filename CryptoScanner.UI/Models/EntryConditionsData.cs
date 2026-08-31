using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Helpers;

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

    public int EntryWaitCandles { get; set; }
    public decimal EntryMaxAdversePercentage { get; set; }

    /// <summary>
    /// The reversal shapes an entry waits for, by name. With one or more of them the wait above
    /// stops being a delay and becomes a search window. Held as names because that is what the
    /// setting itself holds; the editor ticks them off against <see cref="PatternNames"/>.
    /// </summary>
    public List<string> EntryWaitForPatterns { get; } = [];

    /// <summary>The thresholds those shapes are measured against, edited in place.</summary>
    public CandlePatternSettings EntryPatternShape { get; } = new();

    /// <summary>Every shape there is, in the order the enum declares them - one checkbox each.</summary>
    public static string[] PatternNames { get; } = Enum.GetNames<CryptoCandlePattern>();

    /// <summary>Turn one shape on or off. Case-insensitive, so a name typed by hand still matches.</summary>
    public void TogglePattern(string name, bool selected)
    {
        EntryWaitForPatterns.RemoveAll(p => p.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (selected)
            EntryWaitForPatterns.Add(name);
    }

    public bool HasPattern(string name)
        => EntryWaitForPatterns.Exists(p => p.Equals(name, StringComparison.OrdinalIgnoreCase));

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

        EntryWaitCandles = e.EntryWaitCandles;
        EntryMaxAdversePercentage = e.EntryMaxAdversePercentage;

        EntryWaitForPatterns.Clear();
        EntryWaitForPatterns.AddRange(e.EntryWaitForPatterns);

        EntryPatternShape.MaxBodyPercentage = e.EntryPatternShape.MaxBodyPercentage;
        EntryPatternShape.MinBodyPercentage = e.EntryPatternShape.MinBodyPercentage;
        EntryPatternShape.MinWickPercentage = e.EntryPatternShape.MinWickPercentage;
        EntryPatternShape.MaxOppositeWickPercentage = e.EntryPatternShape.MaxOppositeWickPercentage;
        EntryPatternShape.TweezerTolerancePercentage = e.EntryPatternShape.TweezerTolerancePercentage;
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

        e.EntryWaitCandles = EntryWaitCandles;
        e.EntryMaxAdversePercentage = EntryMaxAdversePercentage;

        // In the order the enum declares its members, not in the order they were ticked - the
        // Avalonia editor builds its list from the enum and cannot do anything else, and the entry
        // is taken on the FIRST shape in the list that fits.
        e.EntryWaitForPatterns = [.. PatternNames.Where(HasPattern)];

        e.EntryPatternShape.MaxBodyPercentage = EntryPatternShape.MaxBodyPercentage;
        e.EntryPatternShape.MinBodyPercentage = EntryPatternShape.MinBodyPercentage;
        e.EntryPatternShape.MinWickPercentage = EntryPatternShape.MinWickPercentage;
        e.EntryPatternShape.MaxOppositeWickPercentage = EntryPatternShape.MaxOppositeWickPercentage;
        e.EntryPatternShape.TweezerTolerancePercentage = EntryPatternShape.TweezerTolerancePercentage;
    }
}
