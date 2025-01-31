namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{
    public bool ShowSignalsLong { get; set; } = false;
    public bool ShowSignalsShort { get; set; } = false;
    public List<string> IntervalList { get; set; } = [];

    public double MinimumPercentage { get; set; } = 0.25;

    
    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-long.wav";
        SoundFileShort = "sound-fvg-short.wav";

        IntervalList.Add("1h");
        IntervalList.Add("2h");
        IntervalList.Add("4h");
        IntervalList.Add("12h");
        IntervalList.Add("1d");
    }
}