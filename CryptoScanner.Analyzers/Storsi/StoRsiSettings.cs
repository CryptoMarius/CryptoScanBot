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

    // storsi.multi only: how many higher timeframes (on top of the signal interval) must also be
    // oversold/overbought. 3 is what the strategy always used (4 intervals including the signal one).
    [SettingCaption("Multi: number of higher timeframes")]
    public int MultiHigherTimeframes { get; set; } = 3;

    // storsi.multi only: by how much the RSI and Stochastic levels are relaxed on each higher
    // timeframe. 2 is what the strategy always used: RSI oversold 30 - correction on the signal
    // interval, then 2 points more room per higher interval (and the same for the Stochastic).
    // 0 asks every interval for the same level.
    [SettingCaption("Multi: relaxation per higher timeframe")]
    public int MultiCorrectionStep { get; set; } = 2;

    // CheckMacdRecovery was removed on 26-09-2026 (open point 108): storsi never did a MACD
    // recovery test, see StoRsiBase. A settings file that still carries the key is read without it.

    public StoRsiSettings() : base()
    {
        SoundFileLong = "sound-storsi-oversold.wav";
        SoundFileShort = "sound-storsi-overbought.wav";
    }

}