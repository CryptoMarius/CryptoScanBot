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
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; } = new SortedList<string, CryptoQuoteData>();

    /// <summary>
    ///  Standaard instellingen
    /// </summary>
    public SettingsGeneral General { get; set; } = new SettingsGeneral();

    /// <summary>
    ///  Signal gerelateerde instellingen
    /// </summary>
    public SettingsSignal Signal { get; set; } = new SettingsSignal();


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


    /// <summary>
    /// De basis instellingen voor de Settings
    /// </summary>
    public SettingsBasic()
    {
    }

}
