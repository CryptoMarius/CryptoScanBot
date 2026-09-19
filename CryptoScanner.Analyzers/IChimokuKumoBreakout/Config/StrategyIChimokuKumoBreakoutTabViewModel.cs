using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout.Config;

public partial class StrategyIChimokuKumoBreakoutTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyIChimokuKumoBreakoutTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(IChimokuKumoBreakoutSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(IChimokuKumoBreakoutSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
