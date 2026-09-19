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

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategySbmTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySbmSettingsViewModel = new();
        _strategySbmSettingsMethodsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(SbmSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategySbmSettingsViewModel.LoadConfig(settings);
        StrategySbmSettingsMethodsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SbmSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySbmSettingsViewModel.SaveConfig(settings);
        StrategySbmSettingsMethodsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}