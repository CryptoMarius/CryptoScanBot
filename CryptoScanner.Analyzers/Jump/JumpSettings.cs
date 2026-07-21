using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Jump;

[Serializable]
public class JumpSettings : SettingsSignalStrategyBase
{
    public bool UseLowHighCalculation { get; set; } = false;
    public int CandlesLookbackCount { get; set; } = 5;
    public decimal CandlePercentage { get; set; } = 4m;

    public JumpSettings() : base()
    {
        SoundFileLong = "sound-jump-up.wav";
        SoundFileShort = "sound-jump-down.wav";
    }

}