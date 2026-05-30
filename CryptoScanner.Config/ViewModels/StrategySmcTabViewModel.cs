using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategySmcTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategySmcSettingsViewModel _strategySmcSettingsViewModel;

    [ObservableProperty]
    private IntervalViewModel _intervalViewModel;

    public StrategySmcTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySmcSettingsViewModel = new();
        _intervalViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategySmc settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategySmcSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList, true);
    }

    internal void SaveConfig(SettingsSignalStrategySmc settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySmcSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList);
    }
}
