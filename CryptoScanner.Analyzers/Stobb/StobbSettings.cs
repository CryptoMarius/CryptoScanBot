namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class StobbSettings : SettingsSignalStrategyBase
{
    // BB percentage
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 5.0;
    public bool UseLowHigh { get; set; } = false;

    public bool IncludeRsi { get; set; } = false;
    public bool IncludeSoftSbm { get; set; } = false;
    public bool OnlyIfPreviousStobb { get; set; } = false;
    public bool IncludeSbmPercAndCrossing { get; set; } = false;
    public StobbSettings() : base()
    {
        SoundFileLong = "sound-stobb-oversold.wav";
        SoundFileShort = "sound-stobb-overbought.wav";
    }

}