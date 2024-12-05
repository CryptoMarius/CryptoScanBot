using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlExchangeApi : UserControl
{
    public UserControlExchangeApi()
    {
        InitializeComponent();
        EditApiKey.TextChanged += EditApiKeyChanged;
        EditApiSecret.TextChanged += EditApiSecretChanged;
    }

    public void LoadConfig(SettingsExchangeApi settings)
    {
        EditApiKey.Text = settings.Key;
        EditApiSecret.Text = settings.Secret;

        EditApiKeyChanged(null, EventArgs.Empty);
        EditApiSecretChanged(null, EventArgs.Empty);
    }

    public void SaveConfig(SettingsExchangeApi settings)
    {
        settings.Key = EditApiKey.Text;
        settings.Secret = EditApiSecret.Text;
    }

    private static string GetDisplayApiKey(string text)
    {
        return text.Length < 4 ? "" : $"{text[..3]}.. {text[^3..]}";
    }

    private void EditApiKeyChanged(object? sender, EventArgs e)
    {
        LabelApiKeyDisplay.Text = GetDisplayApiKey(EditApiKey.Text);
    }

    private void EditApiSecretChanged(object? sender, EventArgs e)
    {
        LabelApiSecretDisplay.Text = GetDisplayApiKey(EditApiSecret.Text);
    }

}
