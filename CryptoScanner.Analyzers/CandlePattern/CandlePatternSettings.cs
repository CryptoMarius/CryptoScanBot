using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

using System.Text.Json.Serialization;
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
    /// Held as names rather than as the enum itself. The settings file has no string converter for
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

    /// <summary>
    /// How large the decisive candle has to be compared with the candles before it: its body at least
    /// this many times the average body of the <see cref="AverageBodyCandles"/> candles before it. For
    /// three methods both decisive candles, the first and the last, have to pass. Zero switches it off.
    /// <para>
    /// Relative to the coin's own recent candles, so it means the same at any price and in any
    /// volatility; the shape thresholds only compare a candle with its own range.
    /// </para>
    /// </summary>
    [SettingCaption("Min size vs average", SeparatorBefore = true, SubHeader = "Size and trend", Unit = "x average body",
        Tooltip = "The body of the decisive candle at least this many times the average body of the "
            + "20 candles before it. For three methods the first and the last candle both. Zero "
            + "switches it off.")]
    public decimal MinSizeFactor { get; set; } = 0m;

    /// <summary>The number of candles the average body of <see cref="MinSizeFactor"/> is taken over.</summary>
    public const int AverageBodyCandles = 20;

    /// <summary>
    /// Only fire when the close of the pattern is on the trade's side of an EMA of this length: above
    /// it for a long, below it for a short. Zero switches it off. Meant for the continuation shapes,
    /// which follow a move that is already going, but it applies to every pattern in the list.
    /// </summary>
    [SettingCaption("Trend EMA length",
        Tooltip = "Only fire when the close is above an EMA of this length (long) or below it "
            + "(short). Zero switches it off. Meant for continuation shapes such as three methods.")]
    public int TrendEmaLength { get; set; } = 0;

    /// <summary>
    /// Hand the trader a stop-loss beyond the pattern instead of the global one: under the lowest low
    /// of the pattern candles for a long, above the highest high for a short, and for three methods
    /// beyond the first candle. Off leaves the stop-loss and the take profit of the trader settings.
    /// </summary>
    [SettingCaption("Stop-loss beyond the pattern", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Hand the trader a stop-loss beyond the pattern: under its lowest low (long) or "
            + "above its highest high (short); for three methods beyond the first candle. Off keeps "
            + "the stop-loss and take profit of the trader settings.")]
    public bool StopBeyondPattern { get; set; } = false;

    /// <summary>
    /// With <see cref="StopBeyondPattern"/>: the take profit at this many times the stop-loss
    /// distance. Zero keeps the take profit of the trader settings.
    /// </summary>
    [SettingCaption("Risk/reward", Indented = true, EnabledWhen = nameof(StopBeyondPattern), Unit = "x the stop distance",
        Tooltip = "The take profit at this many times the stop-loss distance. Zero keeps the take "
            + "profit of the trader settings.")]
    public decimal RiskReward { get; set; } = 2m;

    /// <summary>
    /// Only fire when the pattern candle sits in a zone of the same side: "dlz", "fvg" and/or "smc".
    /// An empty list switches the requirement off, which is the default.
    /// <para>
    /// This belongs here and not in SettingsEntryConditions for the same reason
    /// <see cref="PrecedingCandles"/> does: it decides whether the shape IS a signal, not what the
    /// trader does with one. A hammer at an arbitrary price is a different thing from a hammer on a
    /// level, and putting the test in the entry conditions would create the signal first and throw
    /// it away afterwards.
    /// </para>
    /// <para>
    /// Held as names for the same reason as <see cref="Patterns"/>: the settings file has no string
    /// converter for enums, so a list of them would be stored as unreadable numbers. The names are
    /// the members of <see cref="CryptoZoneSource"/>, read case-insensitively.
    /// </para>
    /// <para>
    /// The zones have to exist: a kind whose IntervalList under Signal.ZonesDlz/ZonesFvg/ZonesSmc is
    /// empty produces no zones at all, and every signal is then rejected. Only DLZ falls back to 1h
    /// on its own.
    /// </para>
    /// </summary>
    [SettingCaption("Require zone", SeparatorBefore = true, SubHeader = "Zone",
        EnumType = typeof(CryptoZoneSource),
        Tooltip = "Only fire when the pattern candle touches a zone of the same side. Tick nothing "
            + "to switch the requirement off. The zone intervals under Signal.Zones* must be filled, "
            + "otherwise there are no zones and nothing fires.")]
    public List<string> RequireZone { get; set; } = [];

    [JsonIgnore]
    public override IEnumerable<string> RequiredZoneKinds => RequireZone;

    /// <summary>
    /// How much room to allow around the zone, as a percentage of the zone's own price. Zero is
    /// strictly between Bottom and Top; a small value lets a candle that stops just short of the
    /// edge still count.
    /// </summary>
    [SettingCaption("Zone tolerance %",
        Tooltip = "Room around the zone, as a percentage. Zero means strictly between Bottom and Top.")]
    public decimal ZoneTolerancePercentage { get; set; } = 0m;

    public CandlePatternStrategySettings() : base()
    {
    }
}
