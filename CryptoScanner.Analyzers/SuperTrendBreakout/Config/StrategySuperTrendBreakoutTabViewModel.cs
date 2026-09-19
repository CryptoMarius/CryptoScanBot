using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.SuperTrendBreakout.Config;

public partial class StrategySuperTrendBreakoutTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategySuperTrendBreakoutSettingsViewModel _strategySuperTrendBreakoutSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategySuperTrendBreakoutTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySuperTrendBreakoutSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(SuperTrendBreakoutSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategySuperTrendBreakoutSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(SuperTrendBreakoutSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySuperTrendBreakoutSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
