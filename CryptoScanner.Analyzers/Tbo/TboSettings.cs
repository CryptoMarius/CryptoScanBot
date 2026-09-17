using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Tbo;

/// <summary>
/// TBO - a trend-following breakout strategy on a cloud of four moving averages. The cloud gives
/// the direction and the strength of the trend, and the entry is one of three events inside it: a
/// break through a pivot level, a crossing of the two EMAs, or a pullback to the fast line.
/// <para>
/// Everything below the four lengths is a switch that makes the bare rule stricter, and all of them
/// are off by default, so a run can turn them on one at a time and see what each one is worth. The
/// squeeze is deliberately not among them: BbSqueeze already measures that idea as a strategy of
/// its own.
/// </para>
/// </summary>
[Serializable]
public class TboSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order below is the order the settings appear on screen, and it follows
    // StrategyTboSettingsView.axaml.

    /// <summary>
    /// Fire on the break through the last pivot level: the trade this strategy is named after.
    /// </summary>
    [SettingCaption("Entry on breakout", SubHeader = "Entry trigger",
        Tooltip = "Fire when the candle closes through the last pivot level, with the cloud pointing "
            + "the way of the trade.")]
    public bool EntryOnBreakout { get; set; } = true;

    /// <summary>
    /// Fire on the cloud turning: the fast EMA closing above the second one for a long, under it for
    /// a short. A different idea from the level break - the turn itself instead of a level that
    /// gives way - which is why each trigger has its own switch and they are measured apart.
    /// </summary>
    [SettingCaption("Entry on cloud cross",
        Tooltip = "Fire when the fast EMA crosses the second one: above it for a long, under it for "
            + "a short. A separate trigger from the breakout; both may be on at the same time.")]
    public bool EntryOnCloudCross { get; set; } = false;

    /// <summary>
    /// Fire on the pullback to the fast EMA inside a trend that is already running: wait for the
    /// trend, then buy the dip to the fast line instead of chasing a break.
    /// <para>
    /// The candle has to reach the fast EMA and close back on the trade's side of it, with the
    /// previous candle already on that side - so it is a dip INTO the line, not a first crossing.
    /// </para>
    /// </summary>
    [SettingCaption("Entry on springboard bounce",
        Tooltip = "Fire when the price dips to the fast EMA inside a running trend and closes back "
            + "above it (below for a short).")]
    public bool EntryOnSpringboard { get; set; } = false;

    /// <summary>
    /// The fast line: EMA(20) on the close. A pullback bounces off this one, and it is the first
    /// half of the crossing that drives the cross trigger.
    /// <para>
    /// The four lengths belong together - see the note on <see cref="SlowSmaLength"/> before
    /// changing one of them.
    /// </para>
    /// </summary>
    [SettingCaption("Fast EMA", SeparatorBefore = true, SubHeader = "Cloud",
        Tooltip = "The fast line, EMA(20) on the close. The four lengths belong together.")]
    public int FastEmaLength { get; set; } = 20;

    /// <summary>
    /// The second line: EMA(40) on the close. The fast line crossing THIS one is the cross trigger.
    /// </summary>
    [SettingCaption("Second EMA",
        Tooltip = "The second line, EMA(40) on the close. The fast line crossing this one is the "
            + "cross trigger.")]
    public int SecondEmaLength { get; set; } = 40;

    /// <summary>The third line: SMA(50) on the close.</summary>
    [SettingCaption("Medium SMA",
        Tooltip = "The third line, SMA(50) on the close.")]
    public int MediumSmaLength { get; set; } = 50;

    /// <summary>
    /// The slow line and the far edge of the cloud: SMA(150) on the close. Its slope drives the
    /// trend filter further down.
    /// <para>
    /// The four lengths are a SET, not four independent knobs: 20, 40, 50 and 150 together are what
    /// this strategy is. A run that wants a faster or a slower cloud scales all four in proportion.
    /// A test fails when one of the defaults moves, so changing one is always a deliberate act.
    /// </para>
    /// </summary>
    [SettingCaption("Slow SMA",
        Tooltip = "The slow line, SMA(150) on the close, whose slope drives the trend filter.")]
    public int SlowSmaLength { get; set; } = 150;

    /// <summary>
    /// The candle has to close on the side of the WHOLE cloud the trade wants: above all four lines
    /// for a long, under all four for a short. A price between the edges is consolidation, not a
    /// trend.
    /// <para>
    /// Applies to the BREAKOUT trigger only. At a cloud cross the price sits on the cloud by
    /// definition, so asking it to be outside as well would switch that trigger off entirely.
    /// </para>
    /// </summary>
    [SettingCaption("Price outside the cloud",
        Tooltip = "The candle has to close above all four lines for a long, under all four for a "
            + "short. Applies to the breakout trigger only - at a cross the price is on the cloud.")]
    public bool RequirePriceOutsideCloud { get; set; } = true;

    /// <summary>
    /// How far the top and the bottom of the cloud have to be apart at the signal candle, as a
    /// percentage OF THE PRICE, so the same number means the same thing on a coin at 65 000 and one
    /// at 0.01. Zero accepts any width - which includes the flat market where the lines sit on top
    /// of each other.
    /// </summary>
    [SettingCaption("Minimum cloud width %",
        Tooltip = "How far the top and bottom of the cloud have to be apart at the signal candle, "
            + "as a percentage of the price. Zero accepts any width, including a flat market.")]
    public decimal MinimumCloudWidthPercentage { get; set; } = 0m;

    /// <summary>
    /// The cloud has to be WIDENING: top and bottom further apart at the signal candle than at the
    /// candle before it, so the trend is gaining strength. A cloud that is narrowing is a trend
    /// running out of breath.
    /// </summary>
    [SettingCaption("Cloud must be widening",
        Tooltip = "The cloud has to be wider than on the candle before, so the trend is gaining "
            + "strength rather than running out of it.")]
    public bool RequireCloudWidening { get; set; } = false;

    /// <summary>
    /// How far the slow line has to have moved over <see cref="SlowLineLookbackCandles"/> candles,
    /// as a percentage of its own value, in the direction of the trade. Zero switches the check off.
    /// <para>
    /// The slow line says how much weight every other signal deserves: a trade in the direction of a
    /// slow line that is going nowhere is a trade in a range. Which is why this is a filter rather
    /// than a trigger of its own.
    /// </para>
    /// </summary>
    [SettingCaption("Minimum slow line slope %", SubHeader = "Slow line", ColumnBreak = true,
        Tooltip = "How far the slow line has to have moved over the lookback, as a percentage of "
            + "its own value, in the direction of the trade. Zero switches the check off.")]
    public decimal MinimumSlowLineSlopePercentage { get; set; } = 0m;

    /// <summary>Over how many candles that movement is measured.</summary>
    [SettingCaption("Slow line lookback candles", Indented = true,
        Tooltip = "Over how many candles the movement of the slow line is measured. Changing this "
            + "rebuilds the indicator, like the line lengths do.")]
    public int SlowLineLookbackCandles { get; set; } = 10;

    /// <summary>
    /// How many candles left and right of a candle have to be lower (higher) before it counts as a
    /// pivot high (low). Larger means fewer and more important levels, and a level that is
    /// confirmed later: a pivot is only known once its right-hand candles are in.
    /// </summary>
    [SettingCaption("Pivot candles left", SeparatorBefore = true, SubHeader = "Support and resistance",
        Tooltip = "How many candles to the left have to be lower than the pivot candle.")]
    public int PivotLeftCandles { get; set; } = 5;

    /// <summary>
    /// The right-hand side of the pivot. Also the delay: the level is only confirmed this many
    /// candles after the fact, which is why a break can never be measured against the candle that
    /// made the level.
    /// </summary>
    [SettingCaption("Pivot candles right",
        Tooltip = "How many candles to the right have to be lower. Also the delay before the level "
            + "is confirmed, so a break is always against a level that was already there.")]
    public int PivotRightCandles { get; set; } = 5;

    /// <summary>
    /// How old the level may be, in candles. Zero - the default - accepts a level of any age.
    /// <para>
    /// It started at 200 on the assumption that an old level is a stale one, which is an assumption
    /// and not a finding: a level that has held for months is exactly the one the market watches. An
    /// age limit therefore has to earn its place in a run of its own.
    /// </para>
    /// </summary>
    [SettingCaption("Maximum level age",
        Tooltip = "How many candles ago the level may sit. Zero - the default - accepts a level of "
            + "any age, however long it has been there.")]
    public int PivotMaximumAgeCandles { get; set; } = 0;

    /// <summary>
    /// How far beyond the level the candle has to close, as a percentage of the level. Zero takes
    /// any close beyond it, which on a coin with a wide spread means a break of one tick counts.
    /// <para>
    /// It stood at 2% for a day on the strength of a reference chart where a day that cleared its
    /// level by 0.64% appeared to carry no mark. It does carry one, read off the chart itself later,
    /// so the margin has no measurement behind it and is back to zero. It is a setting to measure in
    /// a run, not something to read off a picture.
    /// </para>
    /// </summary>
    [SettingCaption("Breakout buffer %",
        Tooltip = "How far beyond the level the candle has to close, as a percentage of the level. "
            + "Zero takes any close beyond it.")]
    public decimal BreakoutBufferPercentage { get; set; } = 0m;

    /// <summary>
    /// A long wants the RSI at or above its minimum, a short at or below its maximum: the momentum
    /// has to agree with the entry. Applies to whichever trigger fires.
    /// </summary>
    [SettingCaption("Use RSI filter", SeparatorBefore = true, SubHeader = "RSI filter",
        Tooltip = "Ask the RSI to agree with the entry: at or above the minimum for a long, at or "
            + "below the maximum for a short.")]
    public bool UseRsiFilter { get; set; } = false;

    /// <summary>The RSI a long needs at least, on the 0..100 scale.</summary>
    [SettingCaption("RSI minimum (long)", Indented = true, VisibleWhen = nameof(UseRsiFilter),
        Tooltip = "The RSI a long needs at least, on the 0..100 scale.")]
    public decimal RsiLongMinimum { get; set; } = 50m;

    /// <summary>The RSI a short may have at most, on the 0..100 scale.</summary>
    [SettingCaption("RSI maximum (short)", Indented = true, VisibleWhen = nameof(UseRsiFilter),
        Tooltip = "The RSI a short may have at most, on the 0..100 scale.")]
    public decimal RsiShortMaximum { get; set; } = 50m;

    /// <summary>
    /// The volume of the signal candle against a moving average of the volume before it: a break
    /// nobody trades is a break that does not hold. Off by default.
    /// </summary>
    [SettingCaption("Use volume filter", SeparatorBefore = true, SubHeader = "Volume",
        Tooltip = "The signal candle has to trade at a multiple of the average volume before it.")]
    public bool UseVolumeFilter { get; set; } = false;

    /// <summary>How many times the average volume the signal candle has to trade.</summary>
    [SettingCaption("Volume multiplier", Indented = true, VisibleWhen = nameof(UseVolumeFilter),
        Tooltip = "How many times the average volume the signal candle has to trade.")]
    public decimal VolumeMultiplier { get; set; } = 3m;

    /// <summary>
    /// How many candles the average is taken over. The signal candle itself is left out of it, so a
    /// spike is not part of its own average.
    /// </summary>
    [SettingCaption("Volume average candles", Indented = true, VisibleWhen = nameof(UseVolumeFilter),
        Tooltip = "How many candles before the signal candle the average volume is taken over. The "
            + "signal candle is left out of it.")]
    public int VolumeAverageCandles { get; set; } = 20;

    /// <summary>
    /// Leave when the cloud turns against the position. Off by default: the position is left to the
    /// stop loss and the take profit first, which is the setup every other strategy is measured in.
    /// Switching it on adds an extra way out, it does not replace the stop loss or the take profit.
    /// </summary>
    [SettingCaption("Exit on cloud flip", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Leave when the cloud turns against the position. Stop loss and take profit keep "
            + "working next to it.")]
    public bool ExitOnCloudFlip { get; set; } = false;

    /// <summary>
    /// How many closed candles the cloud has to be against the position before it leaves. Zero
    /// leaves on the first candle the EMAs cross back.
    /// </summary>
    [SettingCaption("Exit confirmation candles", Indented = true, EnabledWhen = nameof(ExitOnCloudFlip),
        Tooltip = "How many closed candles the cloud has to be against the position before it "
            + "leaves. Zero leaves on the first candle the EMAs cross back.")]
    public int ExitConfirmationCandles { get; set; } = 0;

    public TboSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";

        // A breakout strategy: the mean-reversion entry conditions of the global fallback would
        // fight the idea, so this strategy brings its own (empty) set.
        EntryConditions = new SettingsEntryConditions();
    }
}
