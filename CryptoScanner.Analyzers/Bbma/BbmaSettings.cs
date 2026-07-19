using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Bbma;

[Serializable]
public class BbmaSettings : SettingsSignalStrategyBase
{
    public BbmaSettings() : base()
    {
        SoundFileLong = "sound-bbma-long.wav";
        SoundFileShort = "sound-bbma-short.wav";
    }
}
