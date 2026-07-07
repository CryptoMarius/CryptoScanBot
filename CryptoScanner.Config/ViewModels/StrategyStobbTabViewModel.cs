using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

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


    internal void LoadConfig(string caption, SettingsSignalStrategyStobb settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyStobbSettingsViewModel.LoadConfig(caption, settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyStobb settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyStobbSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}