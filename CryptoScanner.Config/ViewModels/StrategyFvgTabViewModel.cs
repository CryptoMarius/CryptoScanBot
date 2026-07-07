using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

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


    internal void LoadConfig(string caption, SettingsSignalStrategyFvg settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
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