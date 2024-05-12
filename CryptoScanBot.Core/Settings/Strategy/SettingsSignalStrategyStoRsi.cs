namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyStoRsi : SettingsSignalStrategyBase
{
    public int AddAmount { get; set; } = 0;

    public SettingsSignalStrategyStoRsi() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}