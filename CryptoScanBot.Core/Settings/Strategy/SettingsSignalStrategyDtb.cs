namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyDtb : SettingsSignalStrategyBase
{
    public bool ShowSignalsLong { get; set; } = false;
    public bool ShowSignalsShort { get; set; } = false;
    public double MinimumPercentage { get; set; } = 0.25;

    public SettingsSignalStrategyDtb() : base()
    {
        SoundFileLong = "sound-dtb-long.wav";
        SoundFileShort = "sound-dtb-short.wav";
    }

}