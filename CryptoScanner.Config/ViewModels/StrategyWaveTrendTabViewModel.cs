using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyWaveTrendTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyWaveTrendSettingsViewModel _strategyWaveTrendSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyWaveTrendTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyWaveTrendSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyWaveTrend settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyWaveTrendSettingsViewModel.LoadConfig(caption, settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyWaveTrend settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyWaveTrendSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
