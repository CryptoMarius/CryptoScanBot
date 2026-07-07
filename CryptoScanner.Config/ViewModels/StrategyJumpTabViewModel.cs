using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyJumpTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyJumpSettingsViewModel _strategyJumpSettingsViewModel;

    [ObservableProperty]
    StrategyEntryConditionsViewModel _strategyEntryConditionsViewModel;

    public StrategyJumpTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyJumpSettingsViewModel = new();
        _strategyEntryConditionsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyJump settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyJumpSettingsViewModel.LoadConfig(caption, settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategyJump settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyJumpSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}