using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyAtrRbTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyAtrRbSettingsViewModel _strategyAtrRbSettingsViewModel;

    public StrategyAtrRbTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyAtrRbSettingsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyAtrRb settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyAtrRbSettingsViewModel.LoadConfig(caption, settings);
    }

    internal void SaveConfig(SettingsSignalStrategyAtrRb settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyAtrRbSettingsViewModel.SaveConfig(settings);
    }
}
