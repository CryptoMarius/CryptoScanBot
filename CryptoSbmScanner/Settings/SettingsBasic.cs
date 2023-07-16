using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Settings;


//De instellingen die het analyse gedeelte nodig heeft
[Serializable]
public class SettingsBasic
{
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";

    /// <summary>
    /// Standaard instellingen
    /// </summary>
    public SettingsGeneral General { get; set; } = new();

    /// <summary>
    /// Signal gerelateerde instellingen
    /// </summary>
    public SettingsSignal Signal { get; set; } = new();

    /// <summary>
    /// Trading gerelateerde instellingen
    /// </summary>
    public SettingsTrading Trading { get; set; } = new();

    /// <summary>
    /// Telegram gerelateerde instellingen
    /// </summary>
    public SettingsTelegram Telegram { get; set; } = new();

    /// <summary>
    /// Balanceer instellingen
    /// </summary>
    public SettingsBalanceBot BalanceBot { get; set; } = new();


    /// <summary>
    /// De url's van de exchanges en/of tradingapps
    /// </summary>
    public SortedList<string, CryptoExternalUrls> ExternalUrls { get; set; } = new();

    /// <summary>
    /// Welke basis munten willen we gebruiken
    /// </summary>
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; set; } = new();


    // Als dit aan staat moet de symbol staat in de whitelist dan wordt het toegestaan
    //public bool UseWhiteListOversold { get; set; } = false;
    public List<string> WhiteListOversold { get; set; } = new();

    // Als dit aan en de symbol staat in de blacklist dan wordt de symbol overgeslagen
    //public bool UseBlackListOversold { get; set; } = false;
    public List<string> BlackListOversold { get; set; } = new();

    // Als dit aan staat moet de symbol staat in de whitelist dan wordt het toegestaan
    //public bool UseWhiteListOverbought { get; set; } = false;
    public List<string> WhiteListOverbought { get; set; } = new();

    // Als dit aan en de symbol staat in de blacklist dan wordt de symbol overgeslagen
    //public bool UseBlackListOverbought { get; set; } = false;
    public List<string> BlackListOverbought { get; set; } = new();


    /// <summary>
    /// Instellingen voor uitvoeren backtest
    /// </summary>
    public SettingsBackTest BackTest { get; set; } = new();


    /// <summary>
    /// De basis instellingen voor de Settings
    /// </summary>
    public SettingsBasic()
    {
        if (ExternalUrls.Count == 0)
        {
            ExternalUrls.Add("Binance",
                new()
                {
                    Altrady = new()
                    {
                        Execute = false,
                        Url = "https://app.altrady.com/d/BINA_{quote}_{base}:{interval}",
                    },
                    HyperTrader = new()
                    {
                        Execute = true,
                        Url = "hypertrader://binance/{base}-{quote}/{interval}",
                        Telegram = "http://www.ccscanner.nl/hypertrader/?e=binance&a={base}&b={quote}&i={interval}",
                    },
                    TradingView = new()
                    {
                        Execute = false,
                        Url = "https://www.tradingview.com/chart/?symbol=BINANCE:{base}{quote}&interval={interval}"
                    }
                }
            );

            ExternalUrls.Add("Bybit Futures",
                new()
                {
                    Altrady = new()
                    {
                        Url = "https://app.altrady.com/d/BYBIF_{quote}_{base}:{interval}",
                    },
                    HyperTrader = new()
                    {
                        Execute = true,
                        Url = "hypertrader://binance/{base}-{quote}/{interval}",
                        Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit&a={base}&b={quote}&i={interval}",
                    },
                    TradingView = new()
                    {
                        Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{base}{quote}&interval={interval}",
                    }
                }
            );

            ExternalUrls.Add("Bybit Spot",
                new()
                {
                    Altrady = new()
                    {
                        Url = "https://app.altrady.com/d/BYBI_{quote}_{base}:{interval}",
                    },
                    HyperTrader = new()
                    {
                        Execute = true,
                        Url = "hypertrader://binance/{base}-{quote}/{interval}",
                        Telegram = "http://www.ccscanner.nl/hypertrader/?e=bybit&a={base}&b={quote}&i={interval}",
                    },
                    TradingView = new()
                    {
                        Url = "https://www.tradingview.com/chart/?symbol=BYBIT:{base}{quote}&interval={interval}",
                    },
                }
            );

            ExternalUrls.Add("Kucoin",
                new()
                {
                    Altrady = new()
                    {
                        Url = "https://app.altrady.com/d/KUCN_{quote}_{base}:{interval}",
                    },
                    HyperTrader = new()
                    {
                        Execute = true,
                        Url = "hypertrader://binance/{base}-{quote}/{interval}",
                        Telegram = "http://www.ccscanner.nl/hypertrader/?e=kucoin&a={base}&b={quote}&i={interval}",
                    },
                    TradingView = new()
                    {
                        Url = "https://www.tradingview.com/chart/?symbol=KUCOIN:{base}{quote}&interval={interval}",
                    }
                }
            );
        }
    }

}