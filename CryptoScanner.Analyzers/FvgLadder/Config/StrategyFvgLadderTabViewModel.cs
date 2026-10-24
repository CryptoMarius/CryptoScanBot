using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.FvgLadder.Config;

public partial class StrategyFvgLadderTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyFvgLadderSettingsViewModel _strategyFvgLadderSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyFvgLadderTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyFvgLadderSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(FvgLadderSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyFvgLadderSettingsViewModel.LoadConfig(settings);
        // A zone strategy: no "Use custom intervals" switch, the same as dlz/fvg/smc and the Photino
        // tab (HasIntervalSwitch). The list holds the interval of the Kalman trend, not the side's.
        IntervalViewModel.LoadConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(FvgLadderSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyFvgLadderSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
