using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public partial class StrategyKumoSqueezeTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyKumoSqueezeSettingsViewModel _strategyKumoSqueezeSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyKumoSqueezeTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyKumoSqueezeSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(KumoSqueezeSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyKumoSqueezeSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(KumoSqueezeSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyKumoSqueezeSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
