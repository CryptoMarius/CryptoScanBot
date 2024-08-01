using System.Text.Json.Serialization;
using System.Xml.Serialization;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Settings;

[Serializable]
public class SettingsBasic
{
    /// <summary>
    /// Standaard instellingen
    /// </summary>
    public SettingsGeneral General { get; set; } = new();

    /// <summary>
    /// Scanner gerelateerde instellingen
    /// </summary>
    public SettingsSignal Signal { get; set; } = new();

    /// <summary>
    /// Trading gerelateerde instellingen
    /// </summary>
    public SettingsTrading Trading { get; set; } = new();

    /// <summary>
    /// Balanceer instellingen (restant van oude tool)
    /// </summary>
    //public SettingsBalanceBot BalanceBot { get; set; } = new();

    /// <summary>
    /// Welke basis munten willen we gebruiken
    /// </summary>
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; set; } = [];

    // Als de whitelist gevuld is dan moet de symbol voorkomen in de lijst (anders geen meldingen)
    public List<string> WhiteListOversold { get; set; } = [];

    // Als de symbol in de blacklist voorkomt worden er geen meldingen voor deze munt gemaakt
    public List<string> BlackListOversold { get; set; } = [];

    // Als de whitelist gevuld is dan moet de symbol voorkomen in de lijst (anders geen meldingen)
    public List<string> WhiteListOverbought { get; set; } = [];

    // Als de symbol in de blacklist voorkomt worden er geen meldingen voor deze munt gemaakt
    public List<string> BlackListOverbought { get; set; } = [];

    /// Instellingen voor het uitvoeren backtest (work in progres)
    public SettingsBackTest BackTest { get; set; } = new();

    // What symbols to show
    public List<string> ShowSymbolInformation { get; set; } = new(["BTC", "PAXG", "ETH", "XRP", "SOL", "ADA"]);

    public SettingsBasic()
    {
        // Als default toch iets inschieten
        CryptoQuoteData quoteData = new()
        {
            Name = "USDT",
            FetchCandles = true,
            MinimalVolume = 4500000,
        };

        QuoteCoins.Add(quoteData.Name, quoteData);
    }
}