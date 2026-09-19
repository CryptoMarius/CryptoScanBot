using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.FailedBreakout.Config;

public partial class StrategyFailedBreakoutTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyFailedBreakoutSettingsViewModel _strategyFailedBreakoutSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyFailedBreakoutTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyFailedBreakoutSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(FailedBreakoutSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyFailedBreakoutSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(FailedBreakoutSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyFailedBreakoutSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
