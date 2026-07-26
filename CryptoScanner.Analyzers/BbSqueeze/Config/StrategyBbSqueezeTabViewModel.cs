using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.BbSqueeze.Config;

public partial class StrategyBbSqueezeTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyBbSqueezeSettingsViewModel _strategyBbSqueezeSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyBbSqueezeTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyBbSqueezeSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(BbSqueezeSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig("BBSQUEEZE", settings);
        StrategyBbSqueezeSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(BbSqueezeSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyBbSqueezeSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}
