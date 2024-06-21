using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlAltradyApi : UserControl
{
    public UserControlAltradyApi()
    {
        InitializeComponent();
    }

    public void LoadConfig(SettingsAltradyApi settings)
    {
        EditKey.Text = settings.Key;
        EditSecret.Text = settings.Secret;
    }

    public void SaveConfig(SettingsAltradyApi settings)
    {
        settings.Key = EditKey.Text;
        settings.Secret = EditSecret.Text;
    }

}
