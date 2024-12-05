using CryptoScanBot.Core.Settings;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlAltradyApi : UserControl
{
    public UserControlAltradyApi()
    {
        InitializeComponent();

        EditKey.TextChanged += EditApiKeyChanged;
        EditSecret.TextChanged += EditApiSecretChanged;
    }

    public void LoadConfig(SettingsAltradyApi settings)
    {
        EditKey.Text = settings.Key;
        EditSecret.Text = settings.Secret;

        EditApiKeyChanged(null, EventArgs.Empty);
        EditApiSecretChanged(null, EventArgs.Empty);
    }

    public void SaveConfig(SettingsAltradyApi settings)
    {
        settings.Key = EditKey.Text;
        settings.Secret = EditSecret.Text;
    }

    private static string GetDisplayApiKey(string text)
    {
        return text.Length < 4 ? "" : $"{text[..3]}.. {text[^3..]}";
    }

    private void EditApiKeyChanged(object? sender, EventArgs e)
    {
        LabelApiKeyDisplay.Text = GetDisplayApiKey(EditKey.Text);
    }

    private void EditApiSecretChanged(object? sender, EventArgs e)
    {
        LabelApiSecretDisplay.Text = GetDisplayApiKey(EditSecret.Text);
    }

}
