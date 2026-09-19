using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.BbRsiEngulfing.Config;

public partial class StrategyBbRsiEngulfingTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBbRsiEngulfingSettingsViewModel _strategyBbRsiEngulfingSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBbRsiEngulfingTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBbRsiEngulfingSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(BbRsiEngulfingSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyBbRsiEngulfingSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(BbRsiEngulfingSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBbRsiEngulfingSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
