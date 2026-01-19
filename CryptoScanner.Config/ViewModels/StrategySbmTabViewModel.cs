using CommunityToolkit.Mvvm.ComponentModel;


using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategySbmTabViewModel : ObservableObject
{
    [ObservableProperty]
    SoundAndColorsViewModel _soundAndColorsViewModel;

    [ObservableProperty]
    StrategySbmSettingsViewModel _strategySbmSettingsViewModel;

    [ObservableProperty]
    StrategySbmSettingsMethodsViewModel _strategySbmSettingsMethodsViewModel;

    public StrategySbmTabViewModel()
    {
        _soundAndColorsViewModel = new();
        _strategySbmSettingsViewModel = new();
        _strategySbmSettingsMethodsViewModel = new();
    }


    internal void LoadConfig(string caption, SettingsSignalStrategySbm settings)
    {
        SoundAndColorsViewModel.LoadConfig(caption, settings);
        StrategySbmSettingsViewModel.LoadConfig(settings);
        StrategySbmSettingsMethodsViewModel.LoadConfig(settings);
    }

    internal void SaveConfig(SettingsSignalStrategySbm settings)
    {
        SoundAndColorsViewModel.SaveConfig(settings);
        StrategySbmSettingsViewModel.SaveConfig(settings);
        StrategySbmSettingsMethodsViewModel.SaveConfig(settings);
    }
}