using CryptoScanBot.Core.Json;

using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings;

[Serializable]
public class SettingsTelegram
{
    [JsonConverter(typeof(SecureStringConverter))]
    public string Token { get; set; } = "";
    [JsonConverter(typeof(SecureStringConverter))]
    public string ChatId { get; set; } = "";

    public bool EmojiInTrend { get; set; } = true;
    public bool SendSignalsToTelegram { get; set; } = false;
}