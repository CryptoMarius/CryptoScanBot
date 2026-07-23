using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyVbsSettingsViewModel _strategyVbsSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyVbsTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyVbsSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(VbsSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("Vbs", settings);
        StrategyVbsSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(VbsSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyVbsSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
