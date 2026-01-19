using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class ApiAltradyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = ""; // string (EXACT match)

    [ObservableProperty]
    private string _secret = ""; // string (EXACT match)

    [ObservableProperty]
    private string _keyDisplay = ""; // Display version (first 3 + last 3 chars)

    [ObservableProperty]
    private string _secretDisplay = ""; // Display version (first 3 + last 3 chars)

    partial void OnKeyChanged(string value)
    {
        KeyDisplay = GetDisplayApiKey(value);
    }

    partial void OnSecretChanged(string value)
    {
        SecretDisplay = GetDisplayApiKey(value);
    }

    private static string GetDisplayApiKey(string text)
    {
        return text.Length < 4 ? "" : $"{text[..3]}.. {text[^3..]}";
    }

    public void LoadConfig(SettingsAltradyApi settings)
    {
        Key = settings.Key;
        Secret = settings.Secret;
    }

    public void SaveConfig(SettingsAltradyApi settings)
    {
        settings.Key = Key;
        settings.Secret = Secret;
    }
}
