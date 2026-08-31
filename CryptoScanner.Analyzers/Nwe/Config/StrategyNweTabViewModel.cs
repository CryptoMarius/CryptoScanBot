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

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyNweTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyNweSettingsViewModel = new();
        _strategyNweSettingsNweViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(NweSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyNweSettingsViewModel.LoadConfig(settings);
        StrategyNweSettingsNweViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(NweSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyNweSettingsViewModel.SaveConfig(settings);
        StrategyNweSettingsNweViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}