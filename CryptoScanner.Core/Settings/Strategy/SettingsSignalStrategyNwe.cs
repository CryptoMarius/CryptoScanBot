namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyNwe : SettingsSignalStrategyBase
{
    // configuration:
    public decimal BandWidth { get; set; } = 8.0m;
    public decimal Multiplication { get; set; } = 3.0m;

    public bool IncludeRsi { get; set; } = false;
    public bool IncludeSoftSbm { get; set; } = false;
    public bool IncludeSbmPercAndCrossing { get; set; } = false;
    public bool OnlyIfLux5m { get; set; } = false;

    // Volume-klimax op de signaalcandle: filter losse "stille drift door de band"-tikken weg.
    public bool RequireVolumeClimax { get; set; } = false;
    public int VolumeClimaxLookback { get; set; } = 20;
    public decimal VolumeClimaxMultiplier { get; set; } = 1.5m;

    public SettingsSignalStrategyNwe() : base()
    {
        SoundFileLong = "sound-nwe-oversold.wav";
        SoundFileShort = "sound-nwe-overbought.wav";
    }

}