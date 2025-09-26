namespace CryptoScanBot.Core.Settings.Strategy;

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

    public SettingsSignalStrategyNwe() : base()
    {
        SoundFileLong = "sound-nwe-oversold.wav";
        SoundFileShort = "sound-nwe-overbought.wav";
    }

}