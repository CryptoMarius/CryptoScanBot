using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyNweSettingsViewModel _strategyNweSettingsViewModel;

    [ObservableProperty]
    StrategyNweSettingsNweViewModel _strategyNweSettingsNweViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyNweTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyNweSettingsViewModel = new();
        _strategyNweSettingsNweViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(NweSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyNweSettingsViewModel.LoadConfig(settings);
        StrategyNweSettingsNweViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(NweSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyNweSettingsViewModel.SaveConfig(settings);
        StrategyNweSettingsNweViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}