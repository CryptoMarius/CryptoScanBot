using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.CandlePattern;

/// <summary>
/// One strategy for all the classic reversal patterns, with the pattern itself as a setting rather
/// than as a strategy of its own. That is what makes them comparable: a run varies
/// <see cref="Patterns"/> and nothing else, so the difference in the result is the pattern and not a
/// second implementation that happens to filter differently.
/// </summary>
[Serializable]
public class CandlePatternStrategySettings : SettingsSignalStrategyBase
{
    /// <summary>
    /// Which shapes to fire on. Long reads them bullish, short reads them bearish. Several at once
    /// is an OR: any one of them is enough, and the first one in the list that the candle forms is
    /// the one the signal is reported as. An empty list produces no signals at all.
    /// <para>
    /// Held as names rather than as the enum itself, exactly like
    /// SettingsEntryConditions.EntryWaitForPatterns. The settings file has no string converter for
    /// enums, so the enum would be stored as [0,3] - unreadable in the emulator queue, and silently
    /// repointed the moment a member is inserted in <see cref="CryptoCandlePattern"/>.
    /// </para>
    /// </summary>
    [SettingCaption("Patterns", EnumType = typeof(CryptoCandlePattern),
        Tooltip = "The reversal shapes this strategy fires on. Ticking several is an OR: any one of "
            + "them is enough. With nothing ticked the strategy produces no signals.")]
    public List<string> Patterns { get; set; } = [nameof(CryptoCandlePattern.Engulfing)];

    /// <summary>
    /// The thresholds the shape is measured against, all as a percentage of the candle's own range.
    /// Reachable per run through the dotted path "Shape.MinWickPercentage" and so on.
    /// </summary>
    [SettingCaption("Pattern shape", Group = "Pattern shape", Expand = true)]
    public CandlePatternSettings Shape { get; set; } = new();

    /// <summary>
    /// How many candles of movement AGAINST the trade have to precede the pattern. A reversal needs
    /// something to reverse: without this a hammer in a rising market counts as a buy signal, and
    /// hammer and hanging man become the same thing. Zero switches the requirement off, which is
    /// worth measuring separately - it is the difference between "the shape says something" and "the
    /// shape says something at the right moment".
    /// </summary>
    [SettingCaption("Preceding candles", SeparatorBefore = true, SubHeader = "Preceding move",
        Tooltip = "How many candles of movement AGAINST the trade have to precede the pattern. A "
            + "reversal needs something to reverse; zero switches the requirement off.")]
    public int PrecedingCandles { get; set; } = 3;

    /// <summary>
    /// How far price has to have moved over those candles, as a percentage. Zero means any move in
    /// the right direction counts.
    /// </summary>
    [SettingCaption("Preceding move %",
        Tooltip = "How far price has to have moved over those candles. Zero means any move in the "
            + "right direction counts.")]
    public decimal PrecedingPercentage { get; set; } = 0m;

    public CandlePatternStrategySettings() : base()
    {
    }
}
