using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyBabaTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBabaSettingsViewModel _strategyBabaSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBabaTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBabaSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyBaba settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyBabaSettingsViewModel.LoadConfig(caption, settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyBaba settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBabaSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
