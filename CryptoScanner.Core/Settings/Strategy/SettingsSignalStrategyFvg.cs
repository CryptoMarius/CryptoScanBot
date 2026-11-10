using CryptoScanner.Core.Enums;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{

    // The zones are built on these intervals; anything shorter is not offered.
    [JsonIgnore]
    public override CryptoIntervalPeriod MinimumInterval => CryptoIntervalPeriod.interval1h;

    // Groupbox headers, spelled exactly as the Avalonia views do.
    private const string GroupSettings = "Settings";
    private const string GroupZoneStrength = "Zone strength filter";

    // NOTE: the declaration order is the order on screen, and it follows the Avalonia FVG tab:
    // Settings, Zone strength filter, Intervals. Serialization is by name, so moving a property
    // does not affect an existing settings file.

    [SettingCaption("Minimum percentage", Group = GroupSettings)]
    public double MinimumPercentage { get; set; } = 0.25;

    // Off: only three candles of the zone's colour make a gap (three green for a long, three red
    // for a short), the "strong" FVG this always was. On: the colours do not matter (red, red,
    // green is fine too), only the gap between the first and the third candle does.
    //[SettingCaption("Allow mixed candle colours", Group = GroupSettings)]
    //public bool AllowMixedColours { get; set; } = false;
    // AllowMixedColours was removed on 31-10-2026: the colours of the three candles never matter
    // any more, only the gap does (what "on" used to do). A settings file that still carries the
    // key is read without it.

    // NearZonePercentage and RejectionLookback were removed on 26-09-2026 (open point 108): the
    // combined Stobb/StoRsi+FVG signals that read the first no longer exist, and an fvg.rejection
    // never did. A settings file that still carries the keys is read without them.

    // What closes a zone besides the break that always does: nothing (BodyThrough), the given number
    // of visits with a body close past the middle (BodyPastMidpoint), or the given number of visits
    // at the touch level (Touches). Replaces the combination of "Max touches (0=off)" and "Close
    // zones past the midpoint" (10-11-2026). See CryptoZoneCloseRule.
    // Default for the gaps: only a body close through the far side closes one ("fully filled"),
    // the way most of the literature and TradingView treat a gap. Was "2 touches" until 10-11-2026.
    [SettingCaption("Zone closes on", Group = GroupZoneStrength,
        Tooltip = "Body through: only a candle body closing through the far side closes the zone. "
            + "Body past midpoint: also after the given number of visits with a body close past the middle. "
            + "Touches: also after the given number of visits at the touch level.")]
    public CryptoZoneCloseRule CloseRule { get; set; } = CryptoZoneCloseRule.BodyThrough;

    // Maximum number of wick-touches before a zone is considered exhausted and closed.
    // Supply/demand theory: 0=fresh, 1=tested, 2=weakening, 3+=avoid. Default 2 keeps the
    // first retest signal but suppresses everything after that. Set to 0 to disable
    // touch-based closure (zones only close on body break through the far side).
    [SettingCaption("Number of visits (0=off)", Group = GroupZoneStrength,
        Tooltip = "For Body past midpoint and Touches: after this many visits the zone is closed. Not used with Body through.")]
    public int MaxTouches { get; set; } = 2;

    // How far price has to come into the zone before it counts as one visit. The ONLY thing that
    // differs between the three zone kinds - everything else about counting, weakening and closing
    // is one implementation in ZoneInvalidation. See CryptoZoneTouchLevel.
    [SettingCaption("Touch level", Group = GroupZoneStrength,
        Tooltip = "For Touches: how far price has to come in before a visit counts.")]
    public CryptoZoneTouchLevel TouchLevel { get; set; } = CryptoZoneTouchLevel.Edge;

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
    //[SettingCaption("Close zones past the midpoint", Group = GroupZoneStrength)]
    //public bool CloseZonesPastMidpoint { get; set; } = false;
    // CloseZonesPastMidpoint was removed on 10-11-2026: "past the middle" is now one of the choices
    // of CloseRule, as a body close counted per visit. A settings file that still carries the key is
    // read without it; a queue entry may only leave it at false.

    // The interval list itself lives on the base class now, so every strategy has one.


    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-oversold.wav";
        SoundFileShort = "sound-fvg-overbought.wav";

        IntervalList.Add("1h");
        IntervalList.Add("4h");
        IntervalList.Add("1d");
    }
}