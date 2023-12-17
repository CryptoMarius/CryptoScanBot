using CryptoSbmScanner.Settings.Strategy;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlSettingsPlaySoundAndColors : UserControl
{
    public UserControlSettingsPlaySoundAndColors()
    {
        InitializeComponent();
    }

    public void LoadConfig(string caption, SettingsSignalStrategyBase settings)
    {
        EditPlaySound.Checked = settings.PlaySound;
        EditPlaySpeech.Checked = settings.PlaySpeech;

        UserControlLong.LoadConfig(caption + " long", settings.ColorLong, settings.SoundFileLong);
        UserControlShort.LoadConfig(caption + " short", settings.ColorShort, settings.SoundFileShort);
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        settings.PlaySound = EditPlaySound.Checked;
        settings.PlaySpeech = EditPlaySpeech.Checked;

        settings.ColorLong = UserControlLong.GetColor();
        settings.SoundFileLong = UserControlLong.GetSoundFile();
        settings.ColorShort = UserControlShort.GetColor();
        settings.SoundFileShort = UserControlShort.GetSoundFile();
    }

}
