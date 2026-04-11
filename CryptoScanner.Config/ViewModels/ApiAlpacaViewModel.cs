using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class ApiAlpacaViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    private string _secret = "";

    [ObservableProperty]
    private string _keyDisplay = "";

    [ObservableProperty]
    private string _secretDisplay = "";

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

    public void LoadConfig(SettingsExchangeApi settings)
    {
        Key = settings.Key;
        Secret = settings.Secret;
    }

    public void SaveConfig(SettingsExchangeApi settings)
    {
        settings.Key = Key;
        settings.Secret = Secret;
    }
}
