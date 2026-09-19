using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public partial class StrategyKumoSqueezeTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyKumoSqueezeSettingsViewModel _strategyKumoSqueezeSettingsViewModel;

    // Which intervals this strategy runs on. Empty means "the same as the side".
    [ObservableProperty]
    IntervalViewModel _intervalViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyKumoSqueezeTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyKumoSqueezeSettingsViewModel = new();
        _intervalViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(KumoSqueezeSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyKumoSqueezeSettingsViewModel.LoadConfig(settings);
        IntervalViewModel.LoadStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(KumoSqueezeSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyKumoSqueezeSettingsViewModel.SaveConfig(settings);
        IntervalViewModel.SaveStrategyConfig(settings.IntervalList);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
