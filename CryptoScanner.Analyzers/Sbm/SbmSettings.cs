using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Sbm;

[Serializable]
public class SbmSettings : SettingsSignalStrategyBase
{
    // Groupbox headers, spelled exactly as the Avalonia views do.
    private const string GroupSettings = "Settings";
    private const string GroupMethods = "Settings for SBM methods";

    // NOTE: the declaration order, the groups, the sub-headings and the shared rows below follow
    // StrategySbmSettingsView.axaml and StrategySbmSettingsMethodsView.axaml, because that is what
    // the Blazor hosts render. Serialization is by name, so moving a property does not affect an
    // existing settings file.

    // SBM1 signals
    [SettingCaption("Candle lookback", Group = GroupSettings, SubHeader = "SBM1")]
    public int Sbm1CandlesLookbackCount { get; set; } = 5;

    [SettingCaption("Use low/high instead of open/close", Group = GroupSettings)]
    public bool UseLowHigh { get; set; } = false;

    // SBM2 signals
    [SettingCaption("Percentage in relation to BB bands", Group = GroupSettings, SubHeader = "SBM2", SpaceBefore = true)]
    public decimal Sbm2BbPercentage { get; set; } = 2.5m;

    [SettingCaption("Candle lookback", Group = GroupSettings)]
    public int Sbm2CandlesLookbackCount { get; set; } = 3;

    [SettingCaption("Use low/high instead of open/close", Group = GroupSettings)]
    public bool Sbm2UseLowHigh { get; set; } = false;

    // SBM3 signals
    [SettingCaption("Percentage BB stretching", Group = GroupSettings, SubHeader = "SBM3", SpaceBefore = true)]
    public decimal Sbm3CandlesBbRecoveryPercentage { get; set; } = 225m;

    [SettingCaption("Candle lookback", Group = GroupSettings)]
    public int Sbm3CandlesLookbackCount { get; set; } = 8;


    // SBM algemene
    // Het BB percentage kan via de user interface uit worden gezet (nomargin)
    [SettingCaption("Filter on BB%", Group = GroupMethods)]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("", SameRowAs = nameof(BBMinPercentage))]
    public double BBMaxPercentage { get; set; } = 100.0;

    [SettingCaption("MACD recovery candles", Group = GroupMethods)]
    public int CandlesForMacdRecovery { get; set; } = 2;

    // The lookback / percentage boxes sit BEHIND the caption of the checkbox that governs them,
    // and are greyed out while it is off (IsEnabled in the axaml).
    [SettingCaption("Check for a crossing of the ma200 and ma50 in the last x candles",
        Group = GroupMethods, SeparatorBefore = true)]
    public bool Ma200AndMa50Crossing { get; set; } = true;

    [SettingCaption("", SameRowAs = nameof(Ma200AndMa50Crossing))]
    public int Ma200AndMa50Lookback { get; set; } = 30;

    [SettingCaption("Check for a crossing of the ma200 and ma20 in the last x candles", Group = GroupMethods)]
    public bool Ma200AndMa20Crossing { get; set; } = true;

    [SettingCaption("", SameRowAs = nameof(Ma200AndMa20Crossing))]
    public int Ma200AndMa20Lookback { get; set; } = 15;

    [SettingCaption("Check for a crossing of the ma50 and ma20 in the last x candles", Group = GroupMethods)]
    public bool Ma50AndMa20Crossing { get; set; } = true;

    [SettingCaption("", SameRowAs = nameof(Ma50AndMa20Crossing))]
    public int Ma50AndMa20Lookback { get; set; } = 10;

    [SettingCaption("Minimal percentage between ma200 and ma50",
        Group = GroupMethods, SeparatorBefore = true)]
    public bool CheckMa200AndMa50Percentage { get; set; } = true;

    [SettingCaption("", SameRowAs = nameof(CheckMa200AndMa50Percentage))]
    public decimal Ma200AndMa50Percentage { get; set; } = 0.25m;

    [SettingCaption("Minimal percentage between ma200 and ma20", Group = GroupMethods)]
    public bool CheckMa200AndMa20Percentage { get; set; } = true;

    [SettingCaption("", SameRowAs = nameof(CheckMa200AndMa20Percentage))]
    public decimal Ma200AndMa20Percentage { get; set; } = 0.50m;

    [SettingCaption("Minimal percentage between ma50 and ma20", Group = GroupMethods)]
    public bool CheckMa50AndMa20Percentage { get; set; } = true;

    [SettingCaption("", SameRowAs = nameof(CheckMa50AndMa20Percentage))]
    public decimal Ma50AndMa20Percentage { get; set; } = 0.25m;

    public SbmSettings() : base()
    {
        SoundFileLong = "sound-sbm-oversold.wav";
        SoundFileShort = "sound-sbm-overbought.wav";
    }


}