using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyStorsiTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyStorsiSettingsViewModel _strategyStorsiSettingsViewModel;

    public StrategyStorsiTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyStorsiSettingsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyStoRsi settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyStorsiSettingsViewModel.LoadConfig(caption, settings);
    }

    internal void SaveConfig(SettingsSignalStrategyStoRsi settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyStorsiSettingsViewModel.SaveConfig(settings);
    }
}