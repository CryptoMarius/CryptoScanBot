using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.VwapMr.Config;

public partial class StrategyVwapMrTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyVwapMrSettingsViewModel _strategyVwapMrSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyVwapMrTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyVwapMrSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(VwapMrSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyVwapMrSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(VwapMrSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyVwapMrSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
