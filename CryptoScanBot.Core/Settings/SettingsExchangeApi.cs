using CryptoScanBot.Core.Json;

using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings;

[Serializable]
public class SettingsExchangeApi
{
    // TODO: Iedere exchange heeft 0 of meer key/secret's
    [JsonConverter(typeof(SecureStringConverter))]
    public string Key { get; set; } = "";
    [JsonConverter(typeof(SecureStringConverter))]
    public string Secret { get; set; } = "";
    [JsonConverter(typeof(SecureStringConverter))]
    public string PassPhrase { get; set; } = ""; // Kucoin
}