using System.Xml.Serialization;

using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Settings;

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
    /// Telegram gerelateerde instellingen
    /// </summary>
    public SettingsTelegram Telegram { get; set; } = new();

    /// <summary>
    /// Balanceer instellingen (restant uit een oude tool)
    /// </summary>
    public SettingsBalanceBot BalanceBot { get; set; } = new();

    /// <summary>
    /// Welke basis munten willen we gebruiken
    /// </summary>
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; set; } = new();

    // Als de whitelist gevuld is dan moet de symbol voorkomen in de lijst (anders geen meldingen)
    public List<string> WhiteListOversold { get; set; } = new();

    // Als de symbol in de blacklist voorkomt worden er geen meldingen voor deze munt gemaakt
    public List<string> BlackListOversold { get; set; } = new();

    // Als de whitelist gevuld is dan moet de symbol voorkomen in de lijst (anders geen meldingen)
    public List<string> WhiteListOverbought { get; set; } = new();

    // Als de symbol in de blacklist voorkomt worden er geen meldingen voor deze munt gemaakt
    public List<string> BlackListOverbought { get; set; } = new();


    /// <summary>
    /// Instellingen voor het uitvoeren backtest (work in progres)
    /// </summary>
    public SettingsBackTest BackTest { get; set; } = new();

    public List<string> HiddenSignalColumns { get; set; } = new();
}