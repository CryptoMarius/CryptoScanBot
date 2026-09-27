using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout.Config;

public partial class StrategyIChimokuKumoBreakoutTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyIChimokuKumoBreakoutSettingsViewModel _strategyIChimokuKumoBreakoutSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyIChimokuKumoBreakoutTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyIChimokuKumoBreakoutSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(IChimokuKumoBreakoutSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyIChimokuKumoBreakoutSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(IChimokuKumoBreakoutSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyIChimokuKumoBreakoutSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
