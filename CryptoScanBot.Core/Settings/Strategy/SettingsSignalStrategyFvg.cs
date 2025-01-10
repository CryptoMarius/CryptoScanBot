namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{
    public bool ShowSignalsLong { get; set; } = false;
    public bool ShowSignalsShort { get; set; } = false;

    public double MinimumPercentage { get; set; } = 0.25;

    public List<string> Interval { get; set; } = [];

    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-long.wav";
        SoundFileShort = "sound-fvg-short.wav";

        Interval.Add("1h");
        Interval.Add("2h");
        Interval.Add("4h");
        Interval.Add("12h");
        Interval.Add("1d");
    }
}