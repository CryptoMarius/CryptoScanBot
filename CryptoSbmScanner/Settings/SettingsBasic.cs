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
    }

}
