using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.ChochFvg.Config;

public partial class StrategyChochFvgTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyChochFvgSettingsViewModel _strategyChochFvgSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyChochFvgTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyChochFvgSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(ChochFvgSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyChochFvgSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList, settings.MinimumInterval);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(ChochFvgSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyChochFvgSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
