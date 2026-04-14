using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyBbmaTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    public StrategyBbmaTabViewModel()
    {
        _soundAndColorsViewModel = new();
    }

    internal void LoadConfig(string caption, SettingsSignalStrategyBbma settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
    }

    internal void SaveConfig(SettingsSignalStrategyBbma settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
    }
}
