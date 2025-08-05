namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyNwe : SettingsSignalStrategyBase
{
    // configuration:
    public decimal BandWidth { get; set; } = 8.0m;
    public decimal Multiplication { get; set; } = 3.0m;

    //// Slope: Candes back + percentage
    //public int CandleCountSlope { get; set; } = 15;
    //public decimal IgnorePercentage { get; set; } = 0.75m;

    public SettingsSignalStrategyNwe() : base()
    {
        SoundFileLong = "sound-nwe-long.wav";
        SoundFileShort = "sound-nwe-short.wav";
    }

}