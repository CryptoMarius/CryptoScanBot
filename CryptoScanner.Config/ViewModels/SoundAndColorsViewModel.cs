using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class SoundAndColorsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _playSound;

    [ObservableProperty]
    private bool _playSpeech;

    [ObservableProperty]
    private ColorAndSoundViewModel _longSettings;

    [ObservableProperty]
    private ColorAndSoundViewModel _shortSettings;

    public SoundAndColorsViewModel()
    {
        _longSettings = new ColorAndSoundViewModel();
        _shortSettings = new ColorAndSoundViewModel();
    }

    public void LoadConfig(string caption, SettingsSignalStrategyBase settings)
    {
        PlaySound = settings.PlaySound;
        PlaySpeech = settings.PlaySpeech;

        LongSettings.LoadConfig($"{caption} long", settings.ColorLong, settings.SoundFileLong);
        ShortSettings.LoadConfig($"{caption} short", settings.ColorShort, settings.SoundFileShort);
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        settings.PlaySound = PlaySound;
        settings.PlaySpeech = PlaySpeech;

        settings.ColorLong = LongSettings.SelectedColor;
        settings.SoundFileLong = LongSettings.SoundFile;

        settings.ColorShort = ShortSettings.SelectedColor;
        settings.SoundFileShort = ShortSettings.SoundFile;
    }
}
