using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyDlz : SettingsSignalStrategyBase
{
    // Groupbox headers, spelled exactly as the Avalonia views do.
    private const string GroupDominantZones = "Settings dominant zones";
    private const string GroupZoneStrength = "Zone strength filter";
    private const string GroupFilter = "Filter";
    private const string GroupUnzoomedBox = "Settings unzoomed box";
    private const string GroupZoomedBox = "Settings zoomed box";
    private const string GroupIntervals = "Intervals";

    // Defaults for zigzag calculation
    public SettingsZigZag ZigZag { get; set; } = new(false, TrendType.Primary);


    // NOTE: the declaration order and the groups below follow the five groupboxes of the Avalonia
    // DLZ tab (StrategyDlzTabView.axaml), because that order is what the Blazor hosts render.
    // Serialization is by name, so regrouping a property does not affect an existing settings file.

    // --- Settings dominant zones ---

    // "Candles back" used to live here, defaulting to 500 with 3000 suggested next to it. Removed on
    // 2026-08-22: it was a depth the storage could not deliver. Candles and pivots are trimmed
    // together now (ZigZagIndicator.TrimBefore runs on the candle window, because a pivot holds a
    // candle alive), so asking for 3000 gave 500 candles of pivots and 2500 candles of bookkeeping
    // that claimed otherwise - and zones in that gap were deleted for the wrong reason. There is one
    // depth for the whole engine, CandleTools.CandleCountFetch, and raising it is a decision about
    // memory across every symbol and interval at once rather than a knob for the zones.
    //
    // Existing settings files still carry the old key; unknown properties are skipped on load and it
    // disappears the next time the file is written.

    // Signal percentage — used by SignalDominantLevelNearLong/Short for the "approaching zone" alarm.
    [SettingCaption("Approach warning percentage", Group = GroupDominantZones)]
    public decimal WarnPercentage { get; set; } = 0.25m;

    [SettingCaption("Candles zoom", Group = GroupDominantZones, Unit = "(1h candles)")]
    public int CandleCountZoom { get; set; } = 125;

    // --- Zone strength filter ---

    // How far outside the zone edge (in %) the candle low/high may still be for the combined
    // Stobb+DLZ / StoRsi+DLZ signals to qualify. Separate from WarnPercentage so the two
    // purposes (alarm timing vs. combined-signal proximity) can be tuned independently.
    [SettingCaption("Near zone percentage", Group = GroupZoneStrength)]
    public decimal NearZonePercentage { get; set; } = 0.25m;

    // Maximum number of wick-touches before a zone is considered exhausted and closed.
    // Supply/demand theory: 0=fresh, 1=tested, 2=weakening, 3+=avoid. Default 2 keeps the
    // first retest signal but suppresses everything after that. Set to 0 to disable
    // touch-based closure (zones only close on body break through the far side).
    [SettingCaption("Max touches (0=off)", Group = GroupZoneStrength)]
    public int MaxTouches { get; set; } = 2;

    // How far price has to come into the zone before it counts as one visit. The ONLY thing that
    // differs between the three zone kinds - everything else about counting, weakening and closing
    // is one implementation in ZoneInvalidation. See CryptoZoneTouchLevel.
    [SettingCaption("Touch level", Group = GroupZoneStrength)]
    public CryptoZoneTouchLevel TouchLevel { get; set; } = CryptoZoneTouchLevel.Edge;

    // How many candles back (including the current one) the rejection check may inspect.
    // 1 = only the current candle must show the test+close-back-outside pattern.
    // 2 = a previous candle may have done the wick, with the current candle as confirmation close.
    [SettingCaption("Rejection lookback", Group = GroupZoneStrength)]
    public int RejectionLookback { get; set; } = 1;

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
    [SettingCaption("Close zones past the midpoint", Group = GroupZoneStrength)]
    public bool CloseZonesPastMidpoint { get; set; } = false;

    // --- Filter ---

    // Filter on start
    [SettingCaption("Filter on start of zone", Group = GroupFilter)]
    public bool ZoneStartApply { get; set; } = false;

    [SettingCaption("Candles back", Group = GroupFilter)]
    public int ZoneStartCandleCount { get; set; } = 5; // 5 candles back

    [SettingCaption("Percentage", Group = GroupFilter)]
    public double ZoneStartPercentage { get; set; } = 2.5; // %

    // --- Settings unzoomed box ---

    // Limits unzoomed box
    [SettingCaption("Apply unzoomed filter", Group = GroupUnzoomedBox)]
    public bool ZonesApplyUnzoomed { get; set; } = false;

    [SettingCaption("Minimum unzoomed percentage", Group = GroupUnzoomedBox)]
    public double MinimumUnZoomedPercentage { get; set; } = 0.0;

    [SettingCaption("Maximum unzoomed percentage", Group = GroupUnzoomedBox)]
    public double MaximumUnZoomedPercentage { get; set; } = 0.0;

    // --- Settings zoomed box ---

    // Limits zoomed box
    [SettingCaption("Zoom in on lower intervals", Group = GroupZoomedBox)]
    public bool ZoomLowerTimeFrames { get; set; } = true;

    [SettingCaption("Minimum zoomed percentage", Group = GroupZoomedBox)]
    public double MinimumZoomedPercentage { get; set; } = 0.2;

    [SettingCaption("Maximum zoomed percentage", Group = GroupZoomedBox)]
    public double MaximumZoomedPercentage { get; set; } = 0.7;

    // --- Intervals ---

    // Show signals from. Avalonia renders this with IntervalView.
    [SettingCaption("Intervals", Group = GroupIntervals)]
    public List<string> IntervalList { get; set; } = [];


    public SettingsSignalStrategyDlz() : base()
    {
        SoundFileLong = "sound-dlz-oversold.wav";
        SoundFileShort = "sound-dlz-overbought.wav";

        IntervalList.Add("1h");
    }

}
