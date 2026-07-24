using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SuperTrendBreakout;

[Serializable]
public class SuperTrendBreakoutSettings : SettingsSignalStrategyBase
{
    // Number of candles to look back for a recently closed DLZ zone
    public int ZoneLookbackCandles { get; set; } = 5;

    // Include open zones in the proximity check
    public bool IncludeOpenZones { get; set; } = true;

    // Include recently closed zones in the proximity check
    public bool IncludeClosedZones { get; set; } = true;

    // Maximum age (in candles) for a closed zone to still count
    public int ClosedZoneMaxAgeCandles { get; set; } = 10;

    public SuperTrendBreakoutSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";
    }
}
