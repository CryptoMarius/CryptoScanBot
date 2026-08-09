namespace CryptoScanner.Core.Settings.Strategy;

[Serializable]
public class StoRsiSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order is the order on screen and follows StrategyStorsiSettingsView.axaml.
    // Serialization is by name, so moving a property does not affect an existing settings file.

    // BB percentage — min and max sit behind one caption, as in the axaml
    [SettingCaption("Filter on BB%")]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("", SameRowAs = nameof(BBMinPercentage))]
    public double BBMaxPercentage { get; set; } = 100.0;

    [SettingCaption("Correction RSI")]
    public int AddRsiAmount { get; set; } = 0;

    [SettingCaption("Check if price is near the BB band")]
    public bool CheckBollingerBandsCondition { get; set; } = false;

    [SettingCaption("Only if there is a previous storsi signal")]
    public bool SkipFirstSignal { get; set; } = false;

    [SettingCaption("Only when macd shows recovery")]
    public bool CheckMacdRecovery { get; set; } = false;

    public StoRsiSettings() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}