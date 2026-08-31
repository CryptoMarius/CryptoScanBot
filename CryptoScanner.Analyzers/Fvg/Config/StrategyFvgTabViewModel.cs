using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Fvg.Config;

public partial class StrategyFvgTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyFvgSettingsViewModel _strategyFvgSettingsViewModel;

    [ObservableProperty]
    private IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyFvgTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyFvgSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(SettingsSignalStrategyFvg settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyFvgSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList, CryptoIntervalPeriod.interval1h);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyFvg settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyFvgSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
