namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyStobb : SettingsSignalStrategyBase
{
    // BB percentage
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 5.0;
    public bool UseLowHigh { get; set; } = false;

    public bool IncludeRsi { get; set; } = false;
    public bool IncludeSoftSbm { get; set; } = false;
    public bool OnlyIfPreviousStobb { get; set; } = false;
    public bool IncludeSbmPercAndCrossing { get; set; } = false;
    public bool OnlyIfLux5m { get; set; } = false;
    // Lux 5m threshold (percentage 1..100). Long needs Lux5mValue <= -Lux5mPercentage,
    // short needs Lux5mValue >= +Lux5mPercentage. Default 50 matches the previous
    // hard-coded behavior; raise toward 100 for stricter (rarer) signals.
    public int Lux5mPercentage { get; set; } = 50;

    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    // Zone confirmations — when any of these is enabled, at least one of the enabled
    // zone rejections must match (OR). All disabled = no zone filter.
    public bool UseDlzZone { get; set; } = false;
    public bool UseFvgZone { get; set; } = false;
    public bool UseSmcZone { get; set; } = false;

    public SettingsSignalStrategyStobb() : base()
    {
        SoundFileLong = "sound-stobb-oversold.wav";
        SoundFileShort = "sound-stobb-overbought.wav";
    }

}