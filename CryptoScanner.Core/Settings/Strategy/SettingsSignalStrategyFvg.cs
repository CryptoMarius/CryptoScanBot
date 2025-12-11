namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyFvg : SettingsSignalStrategyBase
{
    public List<string> IntervalList { get; set; } = [];

    public double MinimumPercentage { get; set; } = 0.25;


    public SettingsSignalStrategyFvg() : base()
    {
        SoundFileLong = "sound-fvg-oversold.wav";
        SoundFileShort = "sound-fvg-overbought.wav";

        IntervalList.Add("1h");
        IntervalList.Add("4h");
        IntervalList.Add("1d");
    }
}