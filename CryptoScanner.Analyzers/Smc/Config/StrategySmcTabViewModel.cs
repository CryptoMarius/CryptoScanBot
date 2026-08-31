using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Smc.Config;

public partial class StrategySmcTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategySmcSettingsViewModel _strategySmcSettingsViewModel;

    [ObservableProperty]
    private IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategySmcTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySmcSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(SettingsSignalStrategySmc settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategySmcSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList, CryptoIntervalPeriod.interval10m);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategySmc settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySmcSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
