using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.MacdCrossBand.Config;

public partial class StrategyMacdCrossBandTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyMacdCrossBandSettingsViewModel _strategyMacdCrossBandSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyMacdCrossBandTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyMacdCrossBandSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(MacdCrossBandSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyMacdCrossBandSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(MacdCrossBandSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyMacdCrossBandSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
