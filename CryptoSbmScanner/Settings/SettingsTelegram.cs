namespace CryptoSbmScanner.Settings;

[Serializable]
public class SettingsTelegram
{
    //public bool Active { get; set; } = false;
    public string Token { get; set; } = "";
    public string ChatId { get; set; } = "";

    public bool SendSignalsToTelegram { get; set; } = false;
    public bool UseEmojiInTrend { get; set; } = true;
}