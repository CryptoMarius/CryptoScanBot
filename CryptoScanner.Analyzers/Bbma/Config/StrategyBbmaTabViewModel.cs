using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Bbma.Config;

public partial class StrategyBbmaTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBbmaTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(BbmaSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("Bbma", settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(BbmaSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
