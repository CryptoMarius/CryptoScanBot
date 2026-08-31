using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Jump.Config;

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


    internal void LoadConfig(JumpSettings settings)
    {
        SoundAndColorsViewModel.LoadConfig(settings);
        StrategyJumpSettingsViewModel.LoadConfig(settings);
        StrategyEntryConditionsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(JumpSettings settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategyJumpSettingsViewModel.SaveConfig(settings);
        StrategyEntryConditionsViewModel.SaveConfig(settings);
    }
}