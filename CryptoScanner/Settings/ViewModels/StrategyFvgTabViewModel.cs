using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Settings.ViewModels;

public partial class StrategyFvgTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyFvgSettingsViewModel _strategyFvgSettingsViewModel;

    [ObservableProperty]
    private IntervalViewModel _intervalViewModel;

    public StrategyFvgTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyFvgSettingsViewModel = new();
        _intervalViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyFvg settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyFvgSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList, true);
    }

    internal void SaveConfig(SettingsSignalStrategyFvg settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyFvgSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList);
    }
}