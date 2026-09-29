using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SrFlip;

/// <summary>
/// The support/resistance flip: a level or line that breaks and is then retested from the other side
/// and holds. A broken resistance that turns into support gives a long, a broken support that turns
/// into resistance a short. The levels and lines come from the Core building block
/// (CryptoScanner.Core.Trend.SupportResistance), the same one the chart draws.
/// <para>
/// Measured offline before it was built (27-09-2026, 40 coins, 15m and 1h): on its own a flip earned
/// nothing - about the same as any random candle. It exists as a strategy so the signal can be seen,
/// heard and measured in the emulator; a trader should only switch it on after a run that says
/// otherwise.
/// </para>
/// </summary>
[Serializable]
public class SrFlipSettings : SettingsSignalStrategyBase
{
    private const string GroupFlip = "Flip";
    private const string GroupConfirmation = "Confirmation";
    private const string GroupExit = "Exit";

    /// <summary>Report a flip on a horizontal level (at least MinimumTouches pivots close together).</summary>
    [SettingCaption("Flip on horizontal levels", Group = GroupFlip,
        Tooltip = "A horizontal level (pivots close together) that broke and was retested from the other side and held.")]
    public bool UseHorizontal { get; set; } = true;

    /// <summary>How many pivots a horizontal level needs before its flip counts. Two is the minimum a level has.</summary>
    [SettingCaption("Minimum touches", Group = GroupFlip, Indented = true, EnabledWhen = nameof(UseHorizontal),
        Tooltip = "How many pivots a horizontal level must have before its flip counts. Since 28-09-2026 a level needs three before it breaks at all; four or more are the stronger levels.")]
    public int MinimumTouches { get; set; } = 3;

    /// <summary>Report a flip on a sloped line (through two lower highs or two higher lows).</summary>
    [SettingCaption("Flip on sloped lines", Group = GroupFlip,
        Tooltip = "A falling line through two lower highs, or a rising line through two higher lows, that broke and was retested from the other side and held.")]
    public bool UseSloped { get; set; } = true;

    /// <summary>
    /// How many candles of the signal interval the scan looks back over. The levels use the last 300
    /// of them; the rest lets the walk settle, so a longer window follows the chart more closely at
    /// a higher cost per candle.
    /// </summary>
    [SettingCaption("History candles", Group = GroupFlip,
        Tooltip = "How many candles of the signal interval are scanned for levels and lines. The levels come from the last 300; the rest lets the scan settle.")]
    public int HistoryCandles { get; set; } = 500;

    /// <summary>
    /// Wait for the candle that actually turns, the way a trader does (Marius, 29-09-2026): after the
    /// retest, the first candle that closes in the trade direction (green for a long, red for a short)
    /// and beyond the retest candle's high (long) or low (short). Off trades the retest candle itself.
    /// </summary>
    [SettingCaption("Wait for a confirmation candle", Group = GroupConfirmation,
        Tooltip = "After the retest, wait for the first candle that closes in the trade direction (green for a long, red for a short) and beyond the high (long) or low (short) of the retest candle. Off enters on the retest candle itself.")]
    public bool WaitForConfirmation { get; set; } = true;

    /// <summary>How many candles after the retest the confirmation may take; later and the flip is dropped.</summary>
    [SettingCaption("Within candles", Group = GroupConfirmation, Indented = true, EnabledWhen = nameof(WaitForConfirmation),
        Tooltip = "How many candles after the retest the confirmation candle may come. A close back through the level before that drops the flip.")]
    public int ConfirmationCandles { get; set; } = 3;

    /// <summary>The entry candle's volume against the average of the 20 candles before it. Zero is off.</summary>
    [SettingCaption("Volume (x the average)", Group = GroupConfirmation,
        Tooltip = "The entry candle must trade at least this many times the average volume of the 20 candles before it. 1.5 means half again as much. Zero is off.")]
    public decimal VolumeFactor { get; set; } = 1.5m;

    /// <summary>The stop just beyond the level (half an average candle past it), handed to the trader.</summary>
    [SettingCaption("Stop beyond the level", Group = GroupExit,
        Tooltip = "The stop loss sits half an average candle (ATR) beyond the level that flipped. Off uses the global stop loss percentage.")]
    public bool StopBeyondLevel { get; set; } = true;

    /// <summary>The take profit as a multiple of that stop distance. Zero leaves the trader's own take profit.</summary>
    [SettingCaption("Take profit (x the stop)", Group = GroupExit, Indented = true, EnabledWhen = nameof(StopBeyondLevel),
        Tooltip = "The take profit as a multiple of the stop distance, 2 means twice as far as the stop. Zero leaves the trader's own take profit.")]
    public decimal RiskRewardRatio { get; set; } = 2m;

    public SrFlipSettings() : base()
    {
    }
}
