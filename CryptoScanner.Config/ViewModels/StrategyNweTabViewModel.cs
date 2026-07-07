using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

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


    internal void LoadConfig(string caption, SettingsSignalStrategyNwe settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyNweSettingsViewModel.LoadConfig(settings);
        StrategyNweSettingsNweViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyNwe settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyNweSettingsViewModel.SaveConfig(settings);
        StrategyNweSettingsNweViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}