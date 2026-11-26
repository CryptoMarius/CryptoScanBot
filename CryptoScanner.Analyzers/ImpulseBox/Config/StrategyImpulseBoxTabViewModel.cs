using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.ImpulseBox.Config;

public partial class StrategyImpulseBoxTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyImpulseBoxSettingsViewModel _strategyImpulseBoxSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyImpulseBoxTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyImpulseBoxSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(ImpulseBoxSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyImpulseBoxSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(ImpulseBoxSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyImpulseBoxSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
