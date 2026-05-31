using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

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

    public StrategyDlzTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyDlzSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyDlzSettingsZoomedBoxViewModel = new();
        _strategyDlzSettingsUnzoomedBoxViewModel = new();
        _strategyDlzSettingsZoneFilterViewModel = new();
        _indicatorZigZagViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyZones settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyDlzSettingsViewModel.LoadConfig(settings);
        StrategyDlzSettingsZoomedBoxViewModel.LoadConfig(settings);
        StrategyDlzSettingsUnzoomedBoxViewModel.LoadConfig(settings);
        StrategyDlzSettingsZoneFilterViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList, CryptoIntervalPeriod.interval1h);
        IndicatorZigZagViewModel.LoadConfig(settings.ZigZag);
    }


    internal void SaveConfig(SettingsSignalStrategyZones settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyDlzSettingsViewModel.SaveConfig(settings);
        StrategyDlzSettingsZoomedBoxViewModel.SaveConfig(settings);
        StrategyDlzSettingsUnzoomedBoxViewModel.SaveConfig(settings);
        StrategyDlzSettingsZoneFilterViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        IndicatorZigZagViewModel.SaveConfig(settings.ZigZag);
    }
}