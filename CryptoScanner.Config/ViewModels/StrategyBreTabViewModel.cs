using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyBreTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBreSettingsViewModel _strategyBreSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBreTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBreSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyBre settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyBreSettingsViewModel.LoadConfig(caption, settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyBre settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBreSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
