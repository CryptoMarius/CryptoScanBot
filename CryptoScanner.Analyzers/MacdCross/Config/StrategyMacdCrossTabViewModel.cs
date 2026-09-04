using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.MacdCross.Config;

public partial class StrategyMacdCrossTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyMacdCrossSettingsViewModel _strategyMacdCrossSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyMacdCrossTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyMacdCrossSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(MacdCrossSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyMacdCrossSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(MacdCrossSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyMacdCrossSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
