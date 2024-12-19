namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyStobb : SettingsSignalStrategyBase
{
    // Het BB percentage kan via de user interface uit worden gezet (nomargin)
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 5.0;
    public bool UseLowHigh { get; set; } = false;
    //[JsonConverter(typeof(Intern.ColorConverter))]
    //public Color ColorStobbLong { get; set; } = Color.White;
    //[JsonConverter(typeof(Intern.ColorConverter))]
    //public Color ColorStobbShort { get; set; } = Color.White;
    //public bool PlaySoundStobbSignal { get; set; } = false;
    //public bool PlaySpeechStobbSignal { get; set; } = false;
    //public string SoundStobbLong { get; set; } = "sound-stobb-oversold.wav";
    //public string SoundStobbShort { get; set; } = "sound-stobb-overbought.wav";
    public bool IncludeRsi { get; set; } = false;
    public bool IncludeSoftSbm { get; set; } = false;
    public bool OnlyIfPreviousStobb { get; set; } = false;
    public bool IncludeSbmPercAndCrossing { get; set; } = false;
    public decimal TrendLong { get; set; } = -999m;
    public decimal TrendShort { get; set; } = -999m;

    public SettingsSignalStrategyStobb() : base()
    {
        SoundFileLong = "sound-stobb-oversold.wav";
        SoundFileShort = "sound-stobb-overbought.wav";
    }

}