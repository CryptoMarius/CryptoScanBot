using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyBreTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBreSettingsViewModel _strategyBreSettingsViewModel;

    public StrategyBreTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBreSettingsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyBre settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyBreSettingsViewModel.LoadConfig(caption, settings);
    }

    internal void SaveConfig(SettingsSignalStrategyBre settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBreSettingsViewModel.SaveConfig(settings);
    }
}
