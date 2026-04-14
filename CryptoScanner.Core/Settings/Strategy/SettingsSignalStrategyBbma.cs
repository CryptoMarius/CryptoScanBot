namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyBbma : SettingsSignalStrategyBase
{
    public SettingsSignalStrategyBbma() : base()
    {
        SoundFileLong = "sound-bbma-long.wav";
        SoundFileShort = "sound-bbma-short.wav";
    }
}
