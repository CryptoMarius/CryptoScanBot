using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Config.ViewModels;

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
}
