using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlExchangeApi : UserControl
{
    public UserControlExchangeApi()
    {
        InitializeComponent();
    }

    public void LoadConfig(SettingsExchangeApi settings)
    {
        EditKey.Text = settings.Key;
        EditSecret.Text = settings.Secret;
    }

    public void SaveConfig(SettingsExchangeApi settings)
    {
        settings.Key = EditKey.Text;
        settings.Secret = EditSecret.Text;
    }

}
