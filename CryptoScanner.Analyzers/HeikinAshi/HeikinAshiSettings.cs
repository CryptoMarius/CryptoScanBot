using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.HeikinAshi;

/// <summary>
/// The Heikin Ashi trend: in on the colour change of the Heikin Ashi candle when the Heikin Ashi
/// candle of the higher interval has the same colour, out on the first Heikin Ashi candle with a wick
/// on the other side or of the other colour.
/// <para>
/// The intervals are not fixed here: the interval box of the strategy decides, and empty follows the
/// side like every other strategy. The rule was measured on 1d with 1w as the higher interval.
/// </para>
/// </summary>
[Serializable]
public class HeikinAshiSettings : SettingsSignalStrategyBase
{
    /// <summary>
    /// The higher interval whose last closed Heikin Ashi candle has to have the colour of the trade.
    /// Empty switches the check off, which leaves the bare colour change.
    /// </summary>
    [SettingCaption("Higher interval", Trim = true,
        Tooltip = "The last closed Heikin Ashi candle of this interval has to have the colour of the "
            + "trade: green for a long, red for a short. Empty switches the check off.")]
    public string HigherInterval { get; set; } = "1w";

    /// <summary>
    /// Leave on the first closed Heikin Ashi candle of the other colour, or with a wick on the other
    /// side when <see cref="ExitOnOppositeWick"/> is on. Stop loss and take profit keep working next
    /// to it. Off leaves the position to the trader's normal exits.
    /// </summary>
    [SettingCaption("Exit on the Heikin Ashi candle", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Leave on the first closed Heikin Ashi candle of the other colour. Stop loss and "
            + "take profit keep working next to it; set those wide to measure this exit alone.")]
    public bool ExitOnSignal { get; set; } = true;

    /// <summary>
    /// Also leave on a candle of the trade's own colour that has a wick on the other side: a wick
    /// under the body for a long, above the body for a short. A Heikin Ashi candle of a strong trend
    /// has no wick on that side, so the wick is the first sign of weakness.
    /// </summary>
    [SettingCaption("Also on a wick on the other side", Indented = true, EnabledWhen = nameof(ExitOnSignal),
        Tooltip = "Also leave on a candle with a wick on the other side: under the body for a long, "
            + "above it for a short. A strong trend has no wick on that side.")]
    public bool ExitOnOppositeWick { get; set; } = true;

    /// <summary>
    /// How long a wick has to be to count, as a percentage of the candle's range (high to low).
    /// </summary>
    [SettingCaption("Minimum wick %", Indented = true, EnabledWhen = nameof(ExitOnSignal), Minimum = 0,
        Tooltip = "How long a wick has to be to count, as a percentage of the candle's range from "
            + "high to low.")]
    public decimal WickMinimumPercentage { get; set; } = 5m;

    /// <summary>
    /// Put the stop under the real low of the signal candle (above its real high for a short). It
    /// goes to the trader as a percentage from the signal price, in place of the stop of the trading
    /// settings. Off leaves the stop of the trading settings.
    /// </summary>
    [SettingCaption("Stop beyond the signal candle", SeparatorBefore = true, SubHeader = "Stop",
        Tooltip = "Put the stop under the real low of the signal candle, above its real high for a "
            + "short, in place of the stop of the trading settings.")]
    public bool StopBeyondSignalCandle { get; set; } = true;

    public HeikinAshiSettings() : base()
    {
    }
}
