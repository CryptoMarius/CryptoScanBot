using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Storsi.Config;

public partial class StrategyStorsiTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyStorsiSettingsViewModel _strategyStorsiSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyStorsiTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyStorsiSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    public void LoadConfig(SettingsSignalStrategyStoRsi settings)
    {
        SoundAndColorsViewModel.LoadConfig("StoRsi", settings);
        StrategyStorsiSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(SettingsSignalStrategyStoRsi settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyStorsiSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}