using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.XsMom;

/// <summary>
/// Cross-sectional momentum: at every rebalance moment the coins of one quote are ranked on their
/// return over <see cref="LookbackDays"/>, within the top <see cref="TopByVolume"/> by average volume.
/// Long the strongest <see cref="CoinsPerSide"/>, short the weakest; a coin that drops out of its side
/// is closed at the next rebalance. See XsMom.md.
/// </summary>
[Serializable]
public class XsMomSettings : SettingsSignalStrategyBase
{
    /// <summary>The return a coin is ranked on: the close of the day before the rebalance against the close this many days earlier.</summary>
    [SettingCaption("Return over (days)", SubHeader = "Ranking",
        Tooltip = "The coins are ranked on their return over this many days, measured on the daily closes "
            + "before the rebalance moment.")]
    public int LookbackDays { get; set; } = 14;

    /// <summary>Only the coins with the highest average quote volume take part in the ranking.</summary>
    [SettingCaption("Top by volume (coins)",
        Tooltip = "Only this many coins with the highest average daily volume (in the quote) take part in the ranking.")]
    public int TopByVolume { get; set; } = 100;

    /// <summary>The number of days the average volume is taken over.</summary>
    [SettingCaption("Volume over (days)",
        Tooltip = "The average daily volume that decides the top is taken over this many days before the rebalance moment.")]
    public int VolumeDays { get; set; } = 30;

    /// <summary>Perpetuals on a share, a commodity or an index (product TradFi) do not take part.</summary>
    [SettingCaption("Skip TradFi contracts",
        Tooltip = "Leave the perpetuals on shares, commodities and indices (product TradFi) out of the ranking.")]
    public bool SkipTradFi { get; set; } = true;

    /// <summary>The number of coins on each side: long the strongest, short the weakest.</summary>
    [SettingCaption("Coins per side", SeparatorBefore = true, SubHeader = "Selection",
        Tooltip = "Long the strongest this many coins of the ranking, short the weakest this many.")]
    public int CoinsPerSide { get; set; } = 20;

    /// <summary>
    /// A coin that is held keeps its place while it is within this many times <see cref="CoinsPerSide"/>
    /// of its end of the ranking. 1 keeps only the coins that are still in the selection itself.
    /// </summary>
    [SettingCaption("Keep within (x coins per side)",
        Tooltip = "A coin that is held stays while it is within this many times the coins per side of its end "
            + "of the ranking. 1 = off: a coin that leaves the selection is closed.")]
    public decimal KeepWithinFactor { get; set; } = 1m;

    /// <summary>The day of the week the ranking is made, at 00:00 UTC.</summary>
    [SettingCaption("Rebalance day", SeparatorBefore = true, SubHeader = "Rebalance",
        Tooltip = "The day of the week on which the ranking is made, at 00:00 UTC, on the candles that closed before it.")]
    public DayOfWeek RebalanceDay { get; set; } = DayOfWeek.Monday;

    /// <summary>Rebalance every this many weeks.</summary>
    [SettingCaption("Every (weeks)",
        Tooltip = "Rebalance every this many weeks. Between two rebalance moments nothing is bought or closed by this strategy.")]
    public int RebalanceEveryWeeks { get; set; } = 1;

    public XsMomSettings() : base()
    {
    }
}
