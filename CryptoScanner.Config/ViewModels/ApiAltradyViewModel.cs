using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Exchange.Altrady;
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

    [ObservableProperty]
    private string _testResult = ""; // Outcome of the last connection test

    [ObservableProperty]
    private bool _isTesting = false;

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

    /// <summary>
    /// Send a test signal to Altrady with the key and secret as they are in the edit fields (saved or
    /// not), on the bitcoin pair of the active exchange.
    /// </summary>
    [RelayCommand]
    private async Task TestAltrady()
    {
        if (IsTesting)
            return;
        IsTesting = true;
        TestResult = "Testing...";
        try
        {
            (bool ok, string message) = await AltradyWebhook.TestConnectionAsync(Key, Secret);
            TestResult = (ok ? "" : "Failed: ") + message;
        }
        finally
        {
            IsTesting = false;
        }
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
