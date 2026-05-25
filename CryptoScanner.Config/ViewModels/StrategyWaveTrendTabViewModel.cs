using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyWaveTrendTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyWaveTrendSettingsViewModel _strategyWaveTrendSettingsViewModel;

    public StrategyWaveTrendTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyWaveTrendSettingsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyWaveTrend settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyWaveTrendSettingsViewModel.LoadConfig(caption, settings);
    }

    internal void SaveConfig(SettingsSignalStrategyWaveTrend settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyWaveTrendSettingsViewModel.SaveConfig(settings);
    }
}
