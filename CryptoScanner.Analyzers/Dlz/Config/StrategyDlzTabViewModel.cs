using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyDlzSettingsViewModel _strategyDlzSettingsViewModel;

    [ObservableProperty]
    StrategyDlzSettingsUnzoomedBoxViewModel _strategyDlzSettingsUnzoomedBoxViewModel;

    [ObservableProperty]
    StrategyDlzSettingsZoomedBoxViewModel _strategyDlzSettingsZoomedBoxViewModel;

    [ObservableProperty]
    StrategyDlzSettingsZoneFilterViewModel _strategyDlzSettingsZoneFilterViewModel;


    [ObservableProperty]
    IndicatorZigZagViewModel _indicatorZigZagViewModel;

    [ObservableProperty]
    private IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyDlzTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyDlzSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyDlzSettingsZoomedBoxViewModel = new();
        _strategyDlzSettingsUnzoomedBoxViewModel = new();
        _strategyDlzSettingsZoneFilterViewModel = new();
        _indicatorZigZagViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(SettingsSignalStrategyDlz settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyDlzSettingsViewModel.LoadConfig(settings);
        StrategyDlzSettingsZoomedBoxViewModel.LoadConfig(settings);
        StrategyDlzSettingsUnzoomedBoxViewModel.LoadConfig(settings);
        StrategyDlzSettingsZoneFilterViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList, CryptoIntervalPeriod.interval1h);
        IndicatorZigZagViewModel.LoadConfig(settings.ZigZag);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }


    internal void SaveConfig(SettingsSignalStrategyDlz settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyDlzSettingsViewModel.SaveConfig(settings);
        StrategyDlzSettingsZoomedBoxViewModel.SaveConfig(settings);
        StrategyDlzSettingsUnzoomedBoxViewModel.SaveConfig(settings);
        StrategyDlzSettingsZoneFilterViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        IndicatorZigZagViewModel.SaveConfig(settings.ZigZag);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
