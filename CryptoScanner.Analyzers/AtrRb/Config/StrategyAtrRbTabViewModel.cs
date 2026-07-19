using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.AtrRb.Config;

public partial class StrategyAtrRbTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyAtrRbSettingsViewModel _strategyAtrRbSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyAtrRbTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyAtrRbSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(AtrRbSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("AtrRb", settings);
        StrategyAtrRbSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(AtrRbSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyAtrRbSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
