using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Sbm.Config;

public partial class StrategySbmTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategySbmSettingsViewModel _strategySbmSettingsViewModel;

    [ObservableProperty]
    StrategySbmSettingsMethodsViewModel _strategySbmSettingsMethodsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategySbmTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySbmSettingsViewModel = new();
        _strategySbmSettingsMethodsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(SbmSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("SBM", settings);
        StrategySbmSettingsViewModel.LoadConfig(settings);
        StrategySbmSettingsMethodsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SbmSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySbmSettingsViewModel.SaveConfig(settings);
        StrategySbmSettingsMethodsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}