using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyCandlePatternSettingsViewModel _strategyCandlePatternSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyCandlePatternTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyCandlePatternSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(CandlePatternStrategySettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyCandlePatternSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(CandlePatternStrategySettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyCandlePatternSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
