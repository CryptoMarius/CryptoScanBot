namespace CryptoSbmScanner.Settings;

[Serializable]
public class SettingsTelegram
{
    public bool Active { get; set; } = true;
    public string Token { get; set; } = "5432748238:AAHHSQ4iUCtCAYw3BwoguIfmmFjIVNSAmm0";
    public string ChatId { get; set; } = "-1001981192601";

    public bool SendSignalsToTelegram { get; set; } = false;
    
}