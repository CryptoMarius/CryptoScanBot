using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.FailedBreakout.Config;

public partial class StrategyFailedBreakoutTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyFailedBreakoutSettingsViewModel _strategyFailedBreakoutSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyFailedBreakoutTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyFailedBreakoutSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(FailedBreakoutSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyFailedBreakoutSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(FailedBreakoutSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyFailedBreakoutSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
