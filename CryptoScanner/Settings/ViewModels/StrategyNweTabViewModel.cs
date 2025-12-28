using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Settings.ViewModels;

public partial class StrategyNweTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyNweSettingsViewModel _strategyNweSettingsViewModel;

    [ObservableProperty]
    StrategyNweSettingsNweViewModel _strategyNweSettingsNweViewModel;

    public StrategyNweTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyNweSettingsViewModel = new();
        _strategyNweSettingsNweViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyNwe settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyNweSettingsViewModel.LoadConfig(settings);
        StrategyNweSettingsNweViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyNwe settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyNweSettingsViewModel.SaveConfig(settings);
        StrategyNweSettingsNweViewModel.SaveConfig(settings);
    }
}