using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SuperTrendBreakout;

[Serializable]
public class SuperTrendBreakoutSettings : SettingsSignalStrategyBase
{
    // Number of candles to look back for a recently closed DLZ zone
    [SettingCaption("Zone lookback (candles)",
        Tooltip = "The number of candles to look back for a zone the price was near. 0 means the current candle only.")]
    public int ZoneLookbackCandles { get; set; } = 5;

    // Include open zones in the proximity check
    [SettingCaption("Include open zones",
        Tooltip = "Count a zone that is still open when checking whether the flip happened near a zone.")]
    public bool IncludeOpenZones { get; set; } = true;

    // Include recently closed zones in the proximity check
    [SettingCaption("Include recently closed zones",
        Tooltip = "Count a zone that has already been broken, as long as it is not older than the maximum age below.")]
    public bool IncludeClosedZones { get; set; } = true;

    // Maximum age (in candles) for a closed zone to still count
    [SettingCaption("Maximum age of a closed zone (candles)", EnabledWhen = nameof(IncludeClosedZones),
        Tooltip = "How many candles ago a closed zone may have been broken and still count.")]
    public int ClosedZoneMaxAgeCandles { get; set; } = 10;

    public SuperTrendBreakoutSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";
    }
}
