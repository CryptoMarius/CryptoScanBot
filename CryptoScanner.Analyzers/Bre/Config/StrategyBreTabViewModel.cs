using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Bre.Config;

public partial class StrategyBreTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBreSettingsViewModel _strategyBreSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBreTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBreSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(BreSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("Bre", settings);
        StrategyBreSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(BreSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBreSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
