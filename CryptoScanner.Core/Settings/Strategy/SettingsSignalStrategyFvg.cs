using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{
    // Groupbox headers, spelled exactly as the Avalonia views do.
    private const string GroupSettings = "Settings";
    private const string GroupZoneStrength = "Zone strength filter";
    private const string GroupIntervals = "Intervals";

    // NOTE: the declaration order is the order on screen, and it follows the Avalonia FVG tab:
    // Settings, Zone strength filter, Intervals. Serialization is by name, so moving a property
    // does not affect an existing settings file.

    [SettingCaption("Minimum percentage", Group = GroupSettings)]
    public double MinimumPercentage { get; set; } = 0.25;

    // How far outside the zone edge (in %) the candle low/high may still be for the combined
    // Stobb+FVG / StoRsi+FVG signals to qualify. Kept separate from WarnPercentage (which does
    // not exist here) so the two purposes do not interfere.
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
    public int RejectionLookback { get; set; } = 2;

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

    // The intervals this strategy reports signals for. Avalonia renders this with IntervalView.
    [SettingCaption("Intervals", Group = GroupIntervals)]
    public List<string> IntervalList { get; set; } = [];


    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-oversold.wav";
        SoundFileShort = "sound-fvg-overbought.wav";

        IntervalList.Add("1h");
        IntervalList.Add("4h");
        IntervalList.Add("1d");
    }
}