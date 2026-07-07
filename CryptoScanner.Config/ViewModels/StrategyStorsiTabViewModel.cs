using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

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


    internal void LoadConfig(string caption, SettingsSignalStrategyStoRsi settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyStorsiSettingsViewModel.LoadConfig(caption, settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyStoRsi settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyStorsiSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}