using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyCandlePatternSettingsViewModel _strategyCandlePatternSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyCandlePatternTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyCandlePatternSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(CandlePatternStrategySettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyCandlePatternSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(CandlePatternStrategySettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyCandlePatternSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
