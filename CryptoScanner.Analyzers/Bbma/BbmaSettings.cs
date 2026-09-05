using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Bbma;

/// <summary>
/// Settings of the BBMA Omni strategy. The state classifiers are a port of the OmniView indicator
/// (see Bbma.md); what can be switched is how strictly the Reentry is read, and the strategy's own
/// exit — the two rules the BBMA method gives a reentry trade: take profit at the outer Bollinger
/// band and the stop just beyond the reentry candle. The defaults are the rules as documented;
/// switching a setting off falls back to the looser OmniView reading.
/// </summary>
[Serializable]
public class BbmaSettings : SettingsSignalStrategyBase
{
    // Groupbox headers, spelled exactly as the Avalonia view does.
    private const string GroupReentry = "Reentry";
    private const string GroupHtfSetup = "HTF setup";
    private const string GroupExit = "Exit";

    /// <summary>
    /// The strict reading of the rules: the reentry candle must not close beyond the MA5/10 zone
    /// (close back above BOTH MA5 and MA10 for a long), and the zone itself sits at or above the
    /// mid-band — a pullback in an uptrend. Off is the loose OmniView "AllBBMA" form: back above
    /// MA5 OR MA10, zone anywhere.
    /// </summary>
    [SettingCaption("Strict reentry", Group = GroupReentry,
        Tooltip = "The reentry candle must close back beyond BOTH MA5 and MA10 (not inside the zone) "
            + "and the MA5/10 zone must sit on the trend side of the mid-band. Off is the loose "
            + "OmniView form: back beyond MA5 or MA10, zone anywhere.")]
    public bool ReentryStrict { get; set; } = true;

    /// <summary>
    /// The trigger the LTF walkback found (CSD, CSM, Extreme, ...) has to be at least this many
    /// candles behind the reentry candle — the "minimum of three candles" of the rules, so a
    /// pullback that has not started yet is not taken as one. Zero switches the check off.
    /// </summary>
    [SettingCaption("Min candles after trigger", Group = GroupReentry, Indented = true,
        Tooltip = "The trigger (CSD, CSM, Extreme, ...) has to be at least this many candles "
            + "behind the reentry candle. Zero switches the check off.")]
    public int ReentryMinCandlesAfterTrigger { get; set; } = 3;

    /// <summary>
    /// How many HTF candles back the setup behind the HTF reentry may lie. The rules give two
    /// reentry setups, after a CSD and after a CSM; the most recent one on the trade's side within
    /// this window is the setup, and an opposite-side CSM since then voids it. Zero switches the
    /// check off (every HTF reentry counts, the behaviour before 2026-09-05).
    /// </summary>
    [SettingCaption("Setup lookback (HTF candles)", Group = GroupHtfSetup,
        Tooltip = "How many HTF candles back the CSD or CSM behind the HTF reentry may lie. An "
            + "opposite-side CSM since then voids it. Zero switches the check off.")]
    public int HtfSetupLookback { get; set; } = 10;

    /// <summary>
    /// An opposite-side Extreme on the HTF after the setup (exhaustion at the far band — in the
    /// rules the start of the next cycle) voids the setup as well. Off lets only an opposite CSM
    /// void it.
    /// </summary>
    [SettingCaption("Opposite Extreme voids the setup", Group = GroupHtfSetup, Indented = true,
        Tooltip = "An opposite-side Extreme on the HTF after the setup (exhaustion at the far band) "
            + "voids it as well. Off lets only an opposite CSM void it.")]
    public bool HtfSetupExtremeInvalidates { get; set; } = true;

    /// <summary>
    /// Leave once a closed candle of the position's interval has reached the outer Bollinger band
    /// (the upper band for a long, the lower band for a short) — the take-profit target the BBMA
    /// rules give a reentry trade. Handled through SignalCreateBase.IsExitSignal, so the trader's
    /// stop loss and take profit keep working next to it; set the global take profit wide to
    /// measure the pure band exit.
    /// </summary>
    [SettingCaption("Take profit at the outer band", Group = GroupExit,
        Tooltip = "Leave once a closed candle has reached the outer Bollinger band (upper for a "
            + "long, lower for a short). The trader's stop loss and take profit keep working next "
            + "to it; set the global take profit wide to measure the pure band exit.")]
    public bool TakeProfitAtOuterBand { get; set; } = true;

    /// <summary>
    /// Aim at the outer band of the HTF of the fixed 3-TF triplet (the 1d band for a 1h entry)
    /// instead of the band of the position's own interval. The rules give the take profit on the
    /// band of the higher timeframe; the own band is the nearer, quicker target.
    /// </summary>
    [SettingCaption("Use the HTF band", Group = GroupExit, Indented = true, EnabledWhen = nameof(TakeProfitAtOuterBand),
        Tooltip = "Aim at the outer band of the higher timeframe of the 3-TF triplet (the 1d band "
            + "for a 1h entry) instead of the band of the position's own interval.")]
    public bool TakeProfitOnHtfBand { get; set; } = true;

    /// <summary>
    /// The signal hands its own stop-loss distance to the trader (OverrideSlPercentage): from the
    /// close of the reentry candle to its low (long) or high (short), plus the margin below. Off
    /// leaves the global percentage stop loss from the trading settings.
    /// </summary>
    [SettingCaption("Stop beyond the reentry candle", Group = GroupExit,
        Tooltip = "The stop loss sits just beyond the far side of the reentry candle: under its low "
            + "for a long, above its high for a short. Off uses the global stop loss percentage.")]
    public bool StopBeyondReentryCandle { get; set; } = true;

    /// <summary>
    /// Extra room beyond the reentry candle's extreme, as a percentage of the price, so a wick that
    /// exactly retests the low or high does not take the position out.
    /// </summary>
    [SettingCaption("Stop margin %", Group = GroupExit, Indented = true, EnabledWhen = nameof(StopBeyondReentryCandle),
        Tooltip = "Extra room beyond the reentry candle's extreme, as a percentage of the price.")]
    public decimal StopMarginPercentage { get; set; } = 0.1m;

    public BbmaSettings() : base()
    {
        SoundFileLong = "sound-bbma-long.wav";
        SoundFileShort = "sound-bbma-short.wav";
    }
}
