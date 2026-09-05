using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.MacdCrossBand.Config;

public partial class StrategyMacdCrossBandTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyMacdCrossBandSettingsViewModel _strategyMacdCrossBandSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyMacdCrossBandTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyMacdCrossBandSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(MacdCrossBandSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyMacdCrossBandSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(MacdCrossBandSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyMacdCrossBandSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
