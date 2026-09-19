using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Choch.Config;

public partial class StrategyChochTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyChochSettingsViewModel _strategyChochSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyChochTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyChochSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(ChochSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyChochSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(ChochSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyChochSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
