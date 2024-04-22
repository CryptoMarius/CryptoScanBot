namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyJump : SettingsSignalStrategyBase
{
    public bool UseLowHighCalculation { get; set; } = false;
    public int CandlesLookbackCount { get; set; } = 5;
    public decimal CandlePercentage { get; set; } = 4m;

    public SettingsSignalStrategyJump() : base()
    {
        SoundFileLong = "sound-jump-up.wav";
        SoundFileShort = "sound-jump-down.wav";
    }

}