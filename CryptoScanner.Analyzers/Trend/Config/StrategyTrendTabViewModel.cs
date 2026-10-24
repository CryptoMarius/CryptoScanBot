using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Trend.Config;

public partial class StrategyTrendTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyTrendSettingsViewModel _strategyTrendSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyTrendTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyTrendSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }

    public void LoadConfig(TrendSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyTrendSettingsViewModel.LoadConfig(settings);
        // Intervals below MinimumInterval grey out, as on the Photino tab
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList, settings.MinimumInterval);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    public void SaveConfig(TrendSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyTrendSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
