namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{
    public bool ShowSignalsLong { get; set; } = false;
    public bool ShowSignalsShort { get; set; } = false;

    public double MinimumPercentage { get; set; } = 0.25;

    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-long.wav";
        SoundFileShort = "sound-fvg-short.wav";
    }

}