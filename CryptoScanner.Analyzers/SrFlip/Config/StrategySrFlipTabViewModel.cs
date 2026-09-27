using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.SrFlip.Config;

public partial class StrategySrFlipTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategySrFlipSettingsViewModel _strategySrFlipSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategySrFlipTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySrFlipSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(SrFlipSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategySrFlipSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SrFlipSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySrFlipSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
