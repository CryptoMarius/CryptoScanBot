using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategySmcSettingsViewModel : ObservableObject
{
    // ---- Detector tuning (base + expansion) ----

    [ObservableProperty]
    private int _averageWindow = 20;

    [ObservableProperty]
    private decimal _baseMaxRangeFactor = 0.8m;

    [ObservableProperty]
    private decimal _expansionMinRangeFactor = 1.5m;

    [ObservableProperty]
    private decimal _expansionBodyFraction = 0.5m;

    [ObservableProperty]
    private decimal _strongExpansionFactor = 2.5m;

    [ObservableProperty]
    private int _baseMaxCandles = 6;

    [ObservableProperty]
    private int _maxBlocksPerInterval = 50;

    [ObservableProperty]
    private bool _requireOppositeBaseColor = false;

    // ---- Signal tuning (entry) ----

    [ObservableProperty]
    private bool _onlyStrong = true;

    [ObservableProperty]
    private int _maxTouches = 0;

    [ObservableProperty]
    private int _rejectionLookback = 3;


    public void LoadConfig(SettingsSignalStrategySmc settings)
    {
        AverageWindow = settings.AverageWindow;
        BaseMaxRangeFactor = settings.BaseMaxRangeFactor;
        ExpansionMinRangeFactor = settings.ExpansionMinRangeFactor;
        ExpansionBodyFraction = settings.ExpansionBodyFraction;
        StrongExpansionFactor = settings.StrongExpansionFactor;
        BaseMaxCandles = settings.BaseMaxCandles;
        MaxBlocksPerInterval = settings.MaxBlocksPerInterval;
        RequireOppositeBaseColor = settings.RequireOppositeBaseColor;

        OnlyStrong = settings.OnlyStrong;
        MaxTouches = settings.MaxTouches;
        RejectionLookback = settings.RejectionLookback;
    }

    public void SaveConfig(SettingsSignalStrategySmc settings)
    {
        settings.AverageWindow = AverageWindow;
        settings.BaseMaxRangeFactor = BaseMaxRangeFactor;
        settings.ExpansionMinRangeFactor = ExpansionMinRangeFactor;
        settings.ExpansionBodyFraction = ExpansionBodyFraction;
        settings.StrongExpansionFactor = StrongExpansionFactor;
        settings.BaseMaxCandles = BaseMaxCandles;
        settings.MaxBlocksPerInterval = MaxBlocksPerInterval;
        settings.RequireOppositeBaseColor = RequireOppositeBaseColor;

        settings.OnlyStrong = OnlyStrong;
        settings.MaxTouches = MaxTouches;
        settings.RejectionLookback = RejectionLookback;
    }
}
