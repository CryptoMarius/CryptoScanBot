using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Baba.Config;

public partial class StrategyBabaTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBabaSettingsViewModel _strategyBabaSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBabaTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBabaSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(BabaSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("Baba", settings);
        StrategyBabaSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(BabaSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBabaSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
