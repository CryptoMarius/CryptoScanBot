using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Config.ViewModels;

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
}
