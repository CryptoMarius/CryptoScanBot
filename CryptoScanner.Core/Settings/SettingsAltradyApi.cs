using CryptoScanner.Core.Json;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsAltradyApi
{
    [JsonConverter(typeof(SecureStringConverter))]
    public string Key { get; set; } = "";
    [JsonConverter(typeof(SecureStringConverter))]
    public string Secret { get; set; } = "";
}