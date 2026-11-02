using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.PsarCross;

/// <summary>
/// The parabolic SAR cross: in when the SAR flips to the other side of the price, out when it flips
/// back. The standard SAR (step 0.02, maximum 0.2) every strategy already gets from the indicator hub.
/// <para>
/// Built to measure the idea itself, so the defaults are the bare rule: fire on the flip candle,
/// leave on the flip back, no filters. Every other setting below makes it stricter and is off by
/// default, so a run can switch them on one at a time and see what each one is worth.
/// </para>
/// <para>
/// The intervals are not fixed here: the interval box of the strategy decides, and empty follows the
/// side like every other strategy.
/// </para>
/// </summary>
[Serializable]
public class PsarCrossSettings : SettingsSignalStrategyBase
{
    /// <summary>
    /// How many closed candles the SAR has to stay on the new side after the flip before the signal
    /// fires. Zero fires on the flip candle itself. In a flat market the SAR flips every few candles;
    /// asking for a few candles of confirmation sits those out, at the price of a later entry.
    /// </summary>
    [SettingCaption("Confirmation candles",
        Tooltip = "How many closed candles the SAR has to stay on the new side after the flip "
            + "before the signal fires. Zero fires on the flip candle itself. A few candles sit "
            + "out the flips of a flat market, at the price of a later entry.")]
    public int ConfirmationCandles { get; set; } = 0;

    /// <summary>
    /// How far the SAR has to be from the close at the signal candle, as a percentage OF THE PRICE.
    /// Zero accepts any distance.
    /// </summary>
    [SettingCaption("Minimum distance %",
        Tooltip = "How far the SAR has to be from the close at the signal candle, as a percentage "
            + "of the price. Zero accepts any distance.")]
    public decimal MinimumDistancePercentage { get; set; } = 0m;

    /// <summary>
    /// The volume of the last <see cref="RelativeVolumeCandles"/> candles has to be at least this
    /// many times the average volume of the <see cref="RelativeVolumeAverageCandles"/> candles before
    /// them. Says nothing about the direction. Zero switches the check off.
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
    public int RelativeVolumeAverageCandles { get; set; } = 20;

    /// <summary>
    /// Leave when the SAR flips back against the position. This is the exit the idea comes with.
    /// Stop loss and take profit keep working next to it, so a run that wants to measure the pure
    /// flip exit sets those wide. Off leaves the position to the trader's normal exits.
    /// </summary>
    [SettingCaption("Exit on flip back", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Leave when the SAR flips back against the position. Stop loss and take profit "
            + "keep working next to it; set those wide to measure the pure flip exit.")]
    public bool ExitOnFlipBack { get; set; } = true;

    /// <summary>
    /// How many closed candles the SAR has to be against the position before it leaves. Zero
    /// leaves on the first candle it flips back.
    /// </summary>
    [SettingCaption("Exit confirmation candles", Indented = true, EnabledWhen = nameof(ExitOnFlipBack),
        Tooltip = "How many closed candles the SAR has to be against the position before it "
            + "leaves. Zero leaves on the first candle it flips back.")]
    public int ExitConfirmationCandles { get; set; } = 0;

    public PsarCrossSettings() : base()
    {
    }
}
