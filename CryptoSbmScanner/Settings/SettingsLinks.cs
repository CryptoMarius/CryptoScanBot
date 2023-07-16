namespace CryptoSbmScanner.Settings;

public class CryptoExternalUrl
{
    public bool Execute { get; set; }
    public string Url { get; set; }
    public string Telegram { get; set; }
}

public class CryptoExternalUrls
{
    // Alleen HyperTrader heeft een execute link
    public CryptoExternalUrl Altrady { get; set; }
    public CryptoExternalUrl HyperTrader { get; set; }
    public CryptoExternalUrl TradingView { get; set; }
}
