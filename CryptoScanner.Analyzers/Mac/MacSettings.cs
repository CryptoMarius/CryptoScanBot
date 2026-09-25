using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Mac;

/// <summary>
/// MAC - a trend-following breakout strategy on a cloud of four moving averages. The cloud gives
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
/// <summary>
/// The three speeds, plus one for our own numbers.
/// <para>
/// there is exactly ONE setting for the lines - a line speed - and all it does is move three of the four
/// moving average lengths. Read off its status line on two candles of thirty minute bitcoin and
/// fitted against our own candles, every number lands on one length to the cent. The FIRST line
/// does not move: it is EMA(20) on all three.
/// </para>
/// </summary>
public enum MacSpeed
{
    /// <summary>EMA(20), EMA(40), SMA(50), SMA(150).</summary>
    Standard,

    /// <summary>EMA(20), EMA(30), SMA(40), SMA(80).</summary>
    Fast,

    /// <summary>EMA(20), EMA(50), SMA(100), SMA(200).</summary>
    Slow,

    /// <summary>The four lengths below, whatever they are set to.</summary>
    Custom,
}


public class MacSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order below is the order the settings appear on screen, and it follows
    // StrategyMacSettingsView.axaml.

    /// <summary>
    /// The four markers the strategy draws, and whether each one opens a position.
    /// <para>
    /// These are not four ideas of our own: they are exactly what the indicator puts on the chart,
    /// and the scanner reproduces all four on the candle - 4063 of 4063 markers over eleven coins
    /// and five timeframes. What is NOT known is which of them earns money, and that is why only
    /// the first is on: it is the only one the indicator itself calls an entry. The other three are
    /// worth watching and can be switched on one at a time to be measured. See Mac.md.
    /// </para>
    /// </summary>
    [SettingCaption("Entry on Open Long / Open Short", SubHeader = "Entry trigger",
        Tooltip = "The fast line crossing the second. This is the marker the strategy draws as the "
            + "entry itself.")]
    public bool EntryOnOpenMarker { get; set; } = true;

    /// <summary>
    /// The second line crossing the third: Cross Up and Cross Down.
    /// <para>
    /// It fires BEFORE the cloud has turned, so it is earlier than the entry marker and it is not
    /// confirmed by it. On the chart it reads as "the trend is turning", which is why it is off
    /// until a run says what it is worth.
    /// </para>
    /// </summary>
    [SettingCaption("Entry on Cross Up / Cross Down",
        Tooltip = "The second line crossing the third. Earlier than the entry marker, and not "
            + "confirmed by it.")]
    public bool EntryOnCrossMarker { get; set; } = false;

    /// <summary>
    /// The close crossing back through the second line: Close Long and Close Short.
    /// <para>
    /// the strategy draws this to CLOSE the position on the other side, so taking it as an entry is
    /// deliberately counter-trend - the cloud is still pointing the other way when it fires.
    /// </para>
    /// </summary>
    [SettingCaption("Entry on Close Long / Close Short",
        Tooltip = "The close crossing back through the second line. the strategy draws it to close the "
            + "opposite position, so as an entry it is counter-trend.")]
    public bool EntryOnCloseMarker { get; set; } = false;

    /// <summary>
    /// The break through the level: Breakout and Breakdown.
    /// <para>
    /// The candle closes beyond the level with its wick past the hundred candles before it, and it
    /// is one of the first <see cref="BreakoutEntriesPerRun"/> such candles since the position
    /// opened. A follow-through marker rather than an entry, so it is off by default.
    /// </para>
    /// </summary>
    [SettingCaption("Entry on Breakout / Breakdown",
        Tooltip = "The close beyond the level with the wick past the hundred candles before it. A "
            + "follow-through marker, not an entry of its own.")]
    public bool EntryOnBreakMarker { get; set; } = false;

    /// <summary>
    /// How many break entries one POSITION may carry, counted from the candle the fast line crossed
    /// the second.
    /// <para>
    /// Until 25 September 2026 this counted inside a stretch beyond the level, and that was the
    /// wrong anchor. Measured against its own markers on eleven coins over five
    /// timeframes, 747 of them: counted per stretch the best reading reaches 93% of the markers
    /// while only 43% of what it fires is right; counted per position it reaches 89% at 81%.
    /// </para>
    /// <para>
    /// And three is the number, not two and not four: two reaches 64% of the markers, four reaches
    /// 96% but its precision falls from 87% to 75%. See Mac.md.
    /// </para>
    /// </summary>
    [SettingCaption("Breakout entries per position",
        Tooltip = "At most this many break entries between one entry and the next. Three is what "
            + "the strategy draws.")]
    public int BreakoutEntriesPerRun { get; set; } = 3;

    /// <summary>
    /// Which set of lengths the four lines use.
    /// <para>
    /// This does NOT write the four numbers below: nothing here quietly changes another setting.
    /// The speed and the numbers are read together in <see cref="Lines"/>, and the numbers are
    /// what counts only on <see cref="MacSpeed.Custom"/>.
    /// </para>
    /// </summary>
    [SettingCaption("Line speed", SeparatorBefore = true, SubHeader = "Cloud",
        Tooltip = "The three speeds. Standard is 20/40/50/150, Fast is "
            + "20/30/40/80, Slow is 20/50/100/200. Choose Custom to use the four numbers below.")]
    public MacSpeed Speed { get; set; } = MacSpeed.Standard;

    /// <summary>
    /// The four lengths the lines are actually built from: the preset of <see cref="Speed"/>, or
    /// the four settings below when that is <see cref="MacSpeed.Custom"/>.
    /// <para>
    /// One place decides this, so the indicator and the chart overlay cannot drift apart. Clamping
    /// stays where it was, at the two places that build the lines.
    /// </para>
    /// </summary>
    public (int Fast, int Second, int Medium, int Slow) Lines() => Speed switch
    {
        MacSpeed.Fast => (20, 30, 40, 80),
        MacSpeed.Slow => (20, 50, 100, 200),
        MacSpeed.Custom => (FastEmaLength, SecondEmaLength, MediumSmaLength, SlowSmaLength),
        _ => (20, 40, 50, 150),
    };

    /// <summary>
    /// The fast line: EMA(20) on the close. A pullback bounces off this one, and it is the first
    /// half of the crossing that drives the cross trigger.
    /// <para>
    /// The four lengths belong together - see the note on <see cref="SlowSmaLength"/> before
    /// changing one of them.
    /// </para>
    /// </summary>
    [SettingCaption("Fast EMA", Indented = true,
        Tooltip = "The fast line, EMA(20) on the close. Counts only when the speed is "
            + "Custom. The four lengths belong together.")]
    public int FastEmaLength { get; set; } = 20;

    /// <summary>
    /// The second line: EMA(40) on the close. The fast line crossing THIS one is the cross trigger.
    /// </summary>
    [SettingCaption("Second EMA", Indented = true,
        Tooltip = "The second line, EMA(40) on the close. The fast line crossing this one is the "
            + "cross trigger."
            + " Counts only when the speed is Custom.")]
    public int SecondEmaLength { get; set; } = 40;

    /// <summary>The third line: SMA(50) on the close.</summary>
    [SettingCaption("Medium SMA", Indented = true,
        Tooltip = "The third line, SMA(50) on the close."
            + " Counts only when the speed is Custom.")]
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
    [SettingCaption("Slow SMA", Indented = true,
        Tooltip = "The slow line, SMA(150) on the close, whose slope drives the trend filter."
            + " Counts only when the speed is Custom.")]
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

    /// <summary>The RSI the levels are read from. Wilder's, the same one the RSI filter uses.</summary>
    [SettingCaption("RSI length for levels", SeparatorBefore = true, SubHeader = "Levels",
        Tooltip = "Length of the RSI the levels are taken from.")]
    public int RsiLevelLength { get; set; } = 14;

    /// <summary>
    /// The RSI level a support crossing goes up through. Measured at 35: the readings sit between
    /// 30.1 and 34.9 the candle before and 37.2 and 45.0 on the candle itself, so 30 misses every
    /// one of them and 40 misses half.
    /// </summary>
    [SettingCaption("RSI cross for support", Indented = true,
        Tooltip = "The RSI level that has to be crossed upwards for the candle's low to become "
            + "the support.")]
    public decimal RsiLevelSupportCross { get; set; } = 35m;

    /// <summary>The RSI level a resistance crossing comes down through. Measured at 65.</summary>
    [SettingCaption("RSI cross for resistance", Indented = true,
        Tooltip = "The RSI level that has to be crossed downwards for the candle's high to become "
            + "the resistance.")]
    public decimal RsiLevelResistanceCross { get; set; } = 65m;

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
    /// Leave the position when the close crosses back through the SECOND line against it: down
    /// through it for a long, up through it for a short.
    /// <para>
    /// This is the strategy's Close Long and Close Short, measured rather than invented: over
    /// twelve catalogued markers on four coins the close crossing the second line hits every one,
    /// eight of eight on the short side and four of four on the long side. Its own guide calls
    /// these the signals to act on, and a cluster of them near a level its best setup.
    /// </para>
    /// <para>
    /// Off by default, and it stacks with <see cref="ExitOnCloudFlip"/>: whichever fires first
    /// ends the position.
    /// </para>
    /// </summary>
    [SettingCaption("Exit on second line cross", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Leave when the close crosses back through the second line against the position.")]
    public bool ExitOnSecondLineCross { get; set; } = false;

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

    public MacSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";

        // A breakout strategy: the mean-reversion entry conditions of the global fallback would
        // fight the idea, so this strategy brings its own (empty) set.
        EntryConditions = new SettingsEntryConditions();
    }
}
