using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyJumpTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategyJumpSettingsViewModel _strategyJumpSettingsViewModel;

    public StrategyJumpTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategyJumpSettingsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategyJump settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategyJumpSettingsViewModel.LoadConfig(caption, settings);
    }

    internal void SaveConfig(SettingsSignalStrategyJump settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyJumpSettingsViewModel.SaveConfig(settings);
    }
}