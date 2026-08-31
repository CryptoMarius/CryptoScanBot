using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Stobb.Config;

public partial class StrategyStobbTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyStobbSettingsViewModel _strategyStobbSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyStobbTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyStobbSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(StobbSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyStobbSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(StobbSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyStobbSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
