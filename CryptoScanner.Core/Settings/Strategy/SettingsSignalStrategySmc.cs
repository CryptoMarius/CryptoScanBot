using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Settings.Strategy;

/// <summary>
/// Settings for the SMC (Smart Money Concepts) supply/demand strategy. Persisted under
/// Signal.ZonesSmc in CryptoScanBot-settings.json, in the data folder of the instance
/// (GlobalData.AppDataFolder) — not in an appsettings.json, and there is one such file per scanner
/// instance and per emulator session.
///
/// Both user interfaces can edit them, same as DLZ and FVG: Avalonia through the hand-built
/// SmcConfigView tab, Photino by building the screen out of the SettingCaption attributes below
/// (PluginSettingsEditState), which is why a new setting shows up there on its own and has to be
/// added to the Avalonia view by hand.
///
/// Two groups of knobs:
///   • Detector tuning — how a base + expansion is recognised (read by ZoneSmc.Detect)
///   • Signal tuning   — how the SignalOrderBlock* classes turn a zone into an alarm/entry
///
/// Two entry flavours, increasing in confirmation:
///   • smc           — price TOUCHES the zone band (still no proof it holds)
///   • smc.rejection — price tested the zone AND closed back outside the proximal edge
///                     (the actual bounce/rejection → the entry-grade signal)
/// </summary>
[Serializable]
public class SettingsSignalStrategySmc : SettingsSignalStrategyBase
{
    // Groupbox headers, spelled exactly as the Avalonia views do.
    private const string GroupDetector = "Detector (base + expansion)";
    private const string GroupSignal = "Signal (entry)";
    private const string GroupIntervals = "Intervals";

    // NOTE: the declaration order and the groups follow StrategySmcTabView.axaml, because that
    // order is what the Blazor hosts render. Serialization is by name, so moving a property does
    // not affect an existing settings file.

    // ---- Detector tuning (base + expansion) ----

    // Trailing window used to compute the "average candle range" reference that base and
    // expansion are measured against (ATR-like mean of High-Low).
    [SettingCaption("Average window (candles)", Group = GroupDetector)]
    public int AverageWindow { get; set; } = 20;

    // A candle belongs to a BASE when its range is at or below this fraction of the average.
    [SettingCaption("Base max range factor", Group = GroupDetector)]
    public decimal BaseMaxRangeFactor { get; set; } = 0.8m;

    // A candle is an EXPANSION (leg-out) when its range is at least this multiple of the
    // average range (and its body dominates the range, see ExpansionBodyFraction).
    [SettingCaption("Expansion min range factor", Group = GroupDetector)]
    public decimal ExpansionMinRangeFactor { get; set; } = 1.5m;

    // The expansion candle's body must be at least this fraction of its range so a huge-wick
    // indecision candle doesn't masquerade as an impulsive move.
    [SettingCaption("Expansion body fraction", Group = GroupDetector)]
    public decimal ExpansionBodyFraction { get; set; } = 0.5m;

    // At or above this range multiple the expansion is considered powerful → Strong zone,
    // otherwise Weak.
    [SettingCaption("Strong expansion factor", Group = GroupDetector)]
    public decimal StrongExpansionFactor { get; set; } = 2.5m;

    // Maximum number of consecutive small candles absorbed into one base.
    [SettingCaption("Base max candles", Group = GroupDetector)]
    public int BaseMaxCandles { get; set; } = 6;

    // Cap on zones kept per interval (newest kept) to avoid overloading memory / chart.
    [SettingCaption("Max blocks per interval", Group = GroupDetector)]
    public int MaxBlocksPerInterval { get; set; } = 50;

    // Tighten the detector toward classical ICT/SMC Order Block semantics: when true, only
    // accept a zone where the LAST base candle (the one immediately adjacent to the impulse)
    // has the OPPOSITE color of the impulse. For a long zone the expansion is bullish, so the
    // last base candle must close below its open ("the last bearish candle before the BOS").
    // Mirrors for short zones. Dojis (close == open on the base candle) are rejected when
    // this filter is on.
    // Default false — keeps the broader supply/demand (base + expansion) behaviour.
    [SettingCaption("Require opposite base colour", Group = GroupDetector)]
    public bool RequireOppositeBaseColor { get; set; } = false;

    // ---- Signal tuning (entry) ----

    // Maximum number of visits before the zone is used up and closed - the same meaning as for the
    // DLZ and FVG zones since 24-08-2026, when the three grew one shared implementation
    // (ZoneInvalidation). It used to mean something narrower here: how many touches a zone could
    // ALREADY have and still produce a signal, tested with > instead of >=, and it never closed the
    // zone. The default moved from 1 to 2 because that is the value that keeps the old behaviour:
    // old "allow 0 and 1 touches" is new "used up at 2".
    // 0 disables touch-based closure entirely; a zone then only closes on a break.
    [SettingCaption("Max touches (0=off)", Group = GroupSignal)]
    public int MaxTouches { get; set; } = 2;

    // How far price has to come into the zone before it counts as one visit. The ONLY thing that
    // differs between the three zone kinds - everything else about counting, weakening and closing
    // is one implementation in ZoneInvalidation. See CryptoZoneTouchLevel.
    [SettingCaption("Touch level", Group = GroupSignal)]
    public CryptoZoneTouchLevel TouchLevel { get; set; } = CryptoZoneTouchLevel.Midpoint;

    // When true, a zone closes as soon as price has been at or past its middle - regardless of how
    // many visits it has left. The reasoning: half of what made the level hold has been taken out of
    // it, and what remains is not worth trading a bounce off. Off by default.
    //
    // Was DisqualifyOnMitigation, which did something narrower: leave the zone open but do not offer
    // it as a place to trade. The only code that did that was ZoneProximityHelper, which nothing ever
    // called and which was deleted on 24-08-2026 - so the setting had no reader left. It is a closing
    // rule now, in the one place where a zone's life is decided (ZoneInvalidation).
    //
    // Note with TouchLevel = Midpoint: every counted visit reaches the midpoint there, so switching
    // this on is the same as setting MaxTouches to 1.
    [SettingCaption("Close zones past the midpoint", Group = GroupSignal)]
    public bool CloseZonesPastMidpoint { get; set; } = false;

    // How many candles back (including the current one) the smc.rejection variant may look
    // for the "tested the zone" wick. 1 = the rejection wick + close-back-outside must happen
    // on the same candle. 3 = the wick into the zone may be up to 2 candles before the
    // confirming close-back-outside candle.
    [SettingCaption("Rejection lookback (smc.rejection)", Group = GroupSignal)]
    public int RejectionLookback { get; set; } = 3;

    // Only fire on Strong zones (powerful expansion). Set false to also alarm on Weak zones.
    [SettingCaption("Only strong zones", Group = GroupSignal)]
    public bool OnlyStrong { get; set; } = false;

    // ---- Intervals ----

    // Intervals on which SMC zones are calculated (and on which the strategy reacts).
    // Avalonia renders this with IntervalView.
    [SettingCaption("Intervals", Group = GroupIntervals)]
    public List<string> IntervalList { get; set; } = [];

    public SettingsSignalStrategySmc() : base()
    {
        SoundFileLong = "sound-dlz-oversold.wav";
        SoundFileShort = "sound-dlz-overbought.wav";

        IntervalList.Add("1h");
    }
}
