using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.HeikinAshi.Config;

public partial class StrategyHeikinAshiTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyHeikinAshiSettingsViewModel _strategyHeikinAshiSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyHeikinAshiTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyHeikinAshiSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(HeikinAshiSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyHeikinAshiSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(HeikinAshiSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyHeikinAshiSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
