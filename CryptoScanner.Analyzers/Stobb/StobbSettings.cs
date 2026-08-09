namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class StobbSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order is the order on screen and follows StrategyStobbSettingsView.axaml.
    // Serialization is by name, so moving a property does not affect an existing settings file.

    // BB percentage — min and max sit behind one caption, as in the axaml
    [SettingCaption("Filter on BB%")]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("", SameRowAs = nameof(BBMinPercentage))]
    public double BBMaxPercentage { get; set; } = 5.0;

    [SettingCaption("Calculate BB oversold/overbought via low/high instead of open/close")]
    public bool UseLowHigh { get; set; } = false;

    [SettingCaption("With RSI oversold/overbought conditions")]
    public bool IncludeRsi { get; set; } = false;

    [SettingCaption("With SBM conditions MA-lines")]
    public bool IncludeSoftSbm { get; set; } = false;

    [SettingCaption("With SBM conditions percentages/crossings")]
    public bool IncludeSbmPercAndCrossing { get; set; } = false;

    [SettingCaption("Only if a previous signal exists")]
    public bool OnlyIfPreviousStobb { get; set; } = false;
    public StobbSettings() : base()
    {
        SoundFileLong = "sound-stobb-oversold.wav";
        SoundFileShort = "sound-stobb-overbought.wav";
    }

}