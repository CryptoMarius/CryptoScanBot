using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.DoubleTopBottom;

[Serializable]
public class DoubleTopBottomSettings : SettingsSignalStrategyBase
{

    public DoubleTopBottomSettings() : base()
    {
        SoundFileLong = "sound-dtb-oversold.wav";
        SoundFileShort = "sound-dtb-overbought.wav";
    }

}