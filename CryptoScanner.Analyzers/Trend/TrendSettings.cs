using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Trend;

[Serializable]
public class TrendSettings : SettingsSignalStrategyBase
{

    public TrendSettings() : base()
    {
        SoundFileLong = "sound-trend-oversold.wav";
        SoundFileShort = "sound-trend-overbought.wav";
    }

}