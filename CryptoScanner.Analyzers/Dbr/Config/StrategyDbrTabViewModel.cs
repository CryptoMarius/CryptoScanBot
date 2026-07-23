using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Dbr.Config;

public partial class StrategyDbrTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyDbrSettingsViewModel _strategyDbrSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyDbrTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyDbrSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(DbrSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("Dbr", settings);
        StrategyDbrSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(DbrSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyDbrSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
