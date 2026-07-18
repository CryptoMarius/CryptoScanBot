using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Config.ViewModels;

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
}
