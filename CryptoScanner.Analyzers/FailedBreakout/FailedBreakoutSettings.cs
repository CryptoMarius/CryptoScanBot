using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FailedBreakout;

/// <summary>
/// Price sets a new high or low over a lookback window and then closes back inside it. The break
/// that did not hold - an upthrust going up, a spring going down.
/// <para>
/// Built as a strategy of its own rather than as a filter on purpose. Everything we added as a
/// filter this month cost money (waiting, the adverse-move limit, the band range index), while the
/// candlestick shapes measured as strategies in their own right did make money - four of seven, at
/// full trade volume. So this competes, it does not filter.
/// </para>
/// </summary>
[Serializable]
public class FailedBreakoutSettings : SettingsSignalStrategyBase
{
    /// <summary>
    /// How many candles the level is taken from: the highest high (or lowest low) over this many
    /// candles before the break window. Longer means a level fewer people would argue with, and
    /// fewer signals.
    /// </summary>
    [SettingCaption("Lookback candles",
        Tooltip = "How many candles the level is taken from: the highest high (or lowest low) over "
            + "this many candles before the break window. Longer means a level fewer people would "
            + "argue with, and fewer signals.")]
    public int LookbackCandles { get; set; } = 20;

    /// <summary>
    /// How recently the break must have happened, counted back from the candle being evaluated and
    /// including it. One means the break and the close back inside are the same candle, which is the
    /// classic single-candle upthrust.
    /// </summary>
    [SettingCaption("Break within candles",
        Tooltip = "How recently the break must have happened, counted back from the candle being "
            + "evaluated and including it. One means the break and the close back inside are the "
            + "same candle.")]
    public int BreakWithinCandles { get; set; } = 3;

    /// <summary>
    /// How far beyond the level the break has to have gone, as a percentage OF THE LEVEL. Zero
    /// accepts a break by a single tick. A percentage rather than an amount for the same reason the
    /// candle patterns use one: an absolute threshold measures the price of the coin instead of the
    /// move (see CandlePatternHelper and Tools/PatternScan/README.md).
    /// </summary>
    [SettingCaption("Minimum break %",
        Tooltip = "How far beyond the level the break has to have gone, as a percentage of the "
            + "level. Zero accepts a break by a single tick.")]
    public decimal MinimumBreakPercentage { get; set; } = 0m;

    /// <summary>
    /// How far from the broken level the close may sit, as a percentage OF THE RANGE between the
    /// lookback high and the lookback low. A short wants the close in the top part of that range
    /// (an upthrust closes just under the ceiling), a long wants it in the bottom part (a spring
    /// closes just over the floor).
    /// <para>
    /// Added because one wide candle can break both levels within the same window and close in the
    /// middle, which used to fire a long AND a short on the same candle (SUSHIUSDC, 04-09-2026).
    /// At 50 or lower the two sides can never fire together; 100 switches the check off, which
    /// is what every run made before this setting existed did. A percentage of the range rather
    /// than of the price, because 0.3% back inside is most of a narrow range and nothing of a
    /// wide one.
    /// </para>
    /// </summary>
    [SettingCaption("Close within range %",
        Tooltip = "How far from the broken level the close may sit, as a percentage of the range "
            + "between the lookback high and low. A short needs the close in the top part of the "
            + "range, a long in the bottom part. At 50 or lower the two sides never fire together; "
            + "100 switches the check off.")]
    public decimal CloseWithinRangePercentage { get; set; } = 50m;

    /// <summary>
    /// Only fire when the breaking candle sits in a zone of the same side: "dlz", "fvg" and/or
    /// "smc". An empty list switches the requirement off, which is the default.
    /// <para>
    /// Not the same thing as the level this strategy builds itself. That level is the highest high
    /// or lowest low of the lookback window - what the candles did - while a zone is a level one of
    /// the three zone strategies found and holds on to. Ticking a zone asks for both at once: the
    /// break has to have failed AT a zone, which is the failed zone this was built to measure.
    /// </para>
    /// <para>
    /// Held as names for the same reason as the candle-pattern list: the settings file has no string
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
        Tooltip = "Only fire when the breaking candle touches a zone of the same side - the failed "
            + "break then happens AT a zone. Tick nothing to switch the requirement off. The zone "
            + "intervals under Signal.Zones* must be filled, otherwise there are no zones and "
            + "nothing fires.")]
    public List<string> RequireZone { get; set; } = [];

    /// <summary>
    /// How much room to allow around the zone, as a percentage of the zone's own price. Zero is
    /// strictly between Bottom and Top; a small value lets a candle that stops just short of the
    /// edge still count.
    /// </summary>
    [SettingCaption("Zone tolerance %",
        Tooltip = "Room around the zone, as a percentage. Zero means strictly between Bottom and Top.")]
    public decimal ZoneTolerancePercentage { get; set; } = 0m;

    public FailedBreakoutSettings() : base()
    {
    }
}
