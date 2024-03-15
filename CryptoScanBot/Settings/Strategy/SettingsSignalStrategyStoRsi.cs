namespace CryptoScanBot.Settings.Strategy;

public class SettingsSignalStrategyStoRsi : SettingsSignalStrategyBase
{
    public SettingsSignalStrategyStoRsi() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}