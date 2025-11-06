using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange;

public class CryptoExternalUrl
{
    // Alleen HyperTrader gebruikt een execute link
    public CryptoExternalUrlType Execute { get; set; } = CryptoExternalUrlType.External;
    public string Url { get; set; } = "";
    public string? Telegram { get; set; }
}

public class CryptoExternalUrlAltrady : CryptoExternalUrl
{
    public string? Code { get; set; }
}

public class CryptoExternalUrls
{
    public CryptoExternalUrlAltrady? Altrady { get; set; }
    public CryptoExternalUrl? HyperTrader { get; set; }
    public CryptoExternalUrl? TradingView { get; set; }
    public CryptoExternalUrl? ExchangeUrl { get; set; }
}
