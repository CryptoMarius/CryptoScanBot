using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Settings;

static public class Constants
{
    public const string SymbolNameBarometerPrice = "$BMP";
}



//De instellingen die het analyse gedeelte nodig heeft
[Serializable]
public class SettingsBasic
{
    //Welke basis munten willen we gebruiken
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; set; } = new();

    /// <summary>
    ///  Standaard instellingen
    /// </summary>
    public SettingsGeneral General { get; set; } = new();

    /// <summary>
    ///  Signal gerelateerde instellingen
    /// </summary>
    public SettingsSignal Signal { get; set; } = new();

    /// <summary>
    ///  Bot gerelateerde instellingen
    /// </summary>
    public SettingsTradeBot Bot { get; set; } = new();


    /// <summary>
    ///  Balance Bot instellingen
    /// </summary>
    //public SettingsBalanceBot BalanceBot { get; set; } = new SettingsBalanceBot();



    // Als dit aan staat moet de symbol staat in de whitelist dan wordt het toegestaan
    public bool UseWhiteListOversold { get; set; } = false;
    public List<string> WhiteListOversold = new();

    // Als dit aan en de symbol staat in de blacklist dan wordt de symbol overgeslagen
    public bool UseBlackListOversold { get; set; } = false;
    public List<string> BlackListOversold = new();



    // Als dit aan staat moet de symbol staat in de whitelist dan wordt het toegestaan
    public bool UseWhiteListOverbought { get; set; } = false;
    public List<string> WhiteListOverbought = new();

    // Als dit aan en de symbol staat in de blacklist dan wordt de symbol overgeslagen
    public bool UseBlackListOverbought { get; set; } = false;
    public List<string> BlackListOverbought = new();



    public string BackTestSymbol { get; set; } = "BTCUSDT";
    public string BackTestInterval { get; set; } = "1M";
    public DateTime BackTestTime { get; set; } = DateTime.Now;
    public SignalStrategy BackTestAlgoritm { get; set; } = SignalStrategy.sbm1Oversold;

    /// <summary>
    /// De basis instellingen voor de Settings
    /// </summary>
    public SettingsBasic()
    {
    }

}
