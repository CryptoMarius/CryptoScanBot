using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.PsarCross.Config;

public partial class StrategyPsarCrossTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyPsarCrossSettingsViewModel _strategyPsarCrossSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyPsarCrossTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyPsarCrossSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(PsarCrossSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyPsarCrossSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(PsarCrossSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyPsarCrossSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
