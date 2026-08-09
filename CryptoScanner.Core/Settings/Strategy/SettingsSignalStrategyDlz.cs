using CryptoScanner.Core.Core;

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

    [SettingCaption("Candles back", Group = GroupDominantZones, Unit = "(1h candles)")]
    public int CandleCount { get; set; } = 500; // 3000; // 3000=150 day's back, 500=20.8 day's

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

    // How many candles back (including the current one) the rejection check may inspect.
    // 1 = only the current candle must show the test+close-back-outside pattern.
    // 2 = a previous candle may have done the wick, with the current candle as confirmation close.
    [SettingCaption("Rejection lookback", Group = GroupZoneStrength)]
    public int RejectionLookback { get; set; } = 1;

    // ICT consequent encroachment: when true, a zone is disqualified for new combined-signals
    // once price has pierced past its 50% midpoint, even if TouchCount has not yet hit MaxTouches.
    [SettingCaption("Disqualify on mitigation (CE)", Group = GroupZoneStrength)]
    public bool DisqualifyOnMitigation { get; set; } = false;

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
