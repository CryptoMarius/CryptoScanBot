using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.MacdCross;

/// <summary>
/// The MACD crossover: in when the MACD line crosses its signal line, out when the two cross back.
/// The standard 12/26/9 MACD every strategy already gets from the indicator hub.
/// <para>
/// Built to measure the idea itself, so the defaults are the bare rule: fire on the cross candle,
/// leave on the cross back, no filters. Every other setting below makes it stricter and is off by
/// default, so a run can switch them on one at a time and see what each one is worth.
/// </para>
/// <para>
/// The trend-strength and volume filters exist because the known weakness of the crossover is the
/// flat market, where the lines circle each other. They ask whether the coin is moving at all
/// (ADX, volume) and whether the move is young (ADX came from under a threshold recently), which is
/// the pre-selection Ross Cameron makes by hand before he looks at the MACD.
/// </para>
/// </summary>
[Serializable]
public class MacdCrossSettings : SettingsSignalStrategyBase
{
    /// <summary>
    /// How many closed candles the lines have to stay on the new side after the cross before the
    /// signal fires. Zero fires on the cross candle itself. In a flat market the lines circle each
    /// other and produce a cross every few candles; asking for a few candles of confirmation is the
    /// simplest way to sit those out, at the price of a later entry.
    /// </summary>
    [SettingCaption("Confirmation candles",
        Tooltip = "How many closed candles the lines have to stay on the new side after the cross "
            + "before the signal fires. Zero fires on the cross candle itself. A few candles sit "
            + "out the crosses of a flat market, at the price of a later entry.")]
    public int ConfirmationCandles { get; set; } = 0;

    /// <summary>
    /// How far apart the lines have to be at the signal candle, as a percentage OF THE PRICE. The
    /// MACD is measured in price units, so dividing by the close makes the same number mean the
    /// same thing on a coin at 65 000 and one at 0.01. Zero accepts any separation.
    /// </summary>
    [SettingCaption("Minimum distance %",
        Tooltip = "How far apart the lines have to be at the signal candle, as a percentage of the "
            + "price. Zero accepts any separation.")]
    public decimal MinimumDistancePercentage { get; set; } = 0m;

    /// <summary>
    /// Only take a cross on the far side of the zero line: a long when the MACD line is still under
    /// zero at the cross, a short when it is still above. The classic filter - a cross near the zero
    /// line is a market without direction, a cross far from it is a move that has run its course
    /// and is turning.
    /// </summary>
    [SettingCaption("Cross beyond zero line only",
        Tooltip = "Only take a cross on the far side of the zero line: a long when the MACD line is "
            + "still under zero at the cross, a short when it is still above.")]
    public bool RequireCrossBeyondZeroLine { get; set; } = false;

    /// <summary>
    /// The ADX(14) at the signal candle has to be at least this. The ADX measures the strength of a
    /// move on a 0..100 scale regardless of its direction: under 20 the market is ranging, above 25
    /// a trend is running. Zero switches the check off. Wilder's 25 is the textbook value; crypto on
    /// the short intervals runs hotter, so measure rather than assume.
    /// </summary>
    [SettingCaption("Minimum ADX", SeparatorBefore = true, SubHeader = "Trend strength",
        Tooltip = "The ADX(14) at the signal candle has to be at least this. Under 20 the market is "
            + "ranging, above 25 a trend is running. Zero switches the check off.")]
    public decimal AdxMinimum { get; set; } = 0m;

    /// <summary>
    /// The trend has to be young: somewhere in the last <see cref="AdxRecentlyWithinCandles"/>
    /// candles (the signal candle included) the ADX has to have been UNDER this value. A cross while
    /// the ADX has sat at 45 for an hour is the tail of a move, not the start of one. Zero switches
    /// the check off. Combined with the minimum above this asks for an ADX that is climbing out of
    /// the ranging zone right now.
    /// </summary>
    [SettingCaption("ADX recently below",
        Tooltip = "Somewhere in the last N candles the ADX has to have been under this value, so the "
            + "cross is the start of a move and not its tail. Zero switches the check off.")]
    public decimal AdxRecentlyBelow { get; set; } = 0m;

    /// <summary>How many candles back the "recently below" test looks, the signal candle included.</summary>
    [SettingCaption("ADX recently within candles", Indented = true,
        Tooltip = "How many candles back the 'recently below' test looks, the signal candle included.")]
    public int AdxRecentlyWithinCandles { get; set; } = 10;

    /// <summary>
    /// The volume of the last <see cref="RelativeVolumeCandles"/> candles has to be at least this
    /// many times the average volume of the <see cref="RelativeVolumeAverageCandles"/> candles before
    /// them. Two means the coin is trading at twice its usual pace: something is happening in it.
    /// Says nothing about the direction. Zero switches the check off.
    /// </summary>
    [SettingCaption("Minimum relative volume", SeparatorBefore = true, SubHeader = "Volume",
        Tooltip = "The average volume of the recent candles has to be at least this many times the "
            + "average of the candles before them. Two means twice the usual pace. Zero switches the "
            + "check off.")]
    public decimal RelativeVolumeMinimum { get; set; } = 0m;

    /// <summary>The recent window: how many candles, counted back from and including the signal candle.</summary>
    [SettingCaption("Recent candles", Indented = true,
        Tooltip = "The recent window, counted back from and including the signal candle.")]
    public int RelativeVolumeCandles { get; set; } = 3;

    /// <summary>
    /// The baseline: how many candles BEFORE the recent window the usual volume is averaged over.
    /// The recent candles are left out of it, so a spike does not inflate its own baseline.
    /// </summary>
    [SettingCaption("Average over candles", Indented = true,
        Tooltip = "How many candles before the recent window the usual volume is averaged over. "
            + "The recent candles are left out of it.")]
    public int RelativeVolumeAverageCandles { get; set; } = 50;

    /// <summary>
    /// Leave when the lines cross back against the position. This is the exit the idea comes with.
    /// Stop loss and take profit keep working next to it, so a run that wants to measure the pure
    /// crossover exit sets those wide. Off leaves the position to the trader's normal exits.
    /// </summary>
    [SettingCaption("Exit on cross back", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Leave when the lines cross back against the position. Stop loss and take profit "
            + "keep working next to it; set those wide to measure the pure crossover exit.")]
    public bool ExitOnCrossBack { get; set; } = true;

    /// <summary>
    /// How many closed candles the lines have to be against the position before it leaves. Zero
    /// leaves on the first candle they cross back.
    /// </summary>
    [SettingCaption("Exit confirmation candles", Indented = true, EnabledWhen = nameof(ExitOnCrossBack),
        Tooltip = "How many closed candles the lines have to be against the position before it "
            + "leaves. Zero leaves on the first candle they cross back.")]
    public int ExitConfirmationCandles { get; set; } = 0;

    public MacdCrossSettings() : base()
    {
    }
}
