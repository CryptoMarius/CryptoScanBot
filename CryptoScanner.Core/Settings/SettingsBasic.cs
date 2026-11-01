using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsBasic
{
    /// <summary>
    /// Scanner settings
    /// </summary>
    public SettingsGeneral General { get; set; } = new();

    /// <summary>
    /// Signal settings
    /// </summary>
    public SettingsSignal Signal { get; set; } = new();

    /// <summary>
    /// Trend settings
    /// </summary>
    public SettingsTrend Trend { get; set; } = new();

    /// <summary>
    /// Trading settings
    /// </summary>
    public SettingsTrading Trading { get; set; } = new();

    /// <summary>
    /// Base coins
    /// <para>
    /// Since 31-10-2026 the quotes of the active exchange, kept per exchange in the Quote table and
    /// filled from there by ExchangeSettingsStore. The settings file no longer holds them; an emulator
    /// run still stores them in its settings snapshot.
    /// </para>
    /// </summary>
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; set; } = [];

    /// <summary>
    /// Products (the code behind the dot in a symbol name), each of which can be switched off as a
    /// whole. Filled as products come by, like the quote coins - see <see cref="CryptoProductData"/>.
    /// <para>
    /// Since 31-10-2026 the products of the active exchange, kept per exchange in the Product table
    /// (ExchangeSettingsStore), like the quote coins.
    /// </para>
    /// </summary>
    public SortedList<string, CryptoProductData> Products { get; set; } = [];

    // White and blacklist settings
    public List<string> WhiteListOversold { get; set; } = [];
    public List<string> BlackListOversold { get; set; } = [];
    public List<string> WhiteListOverbought { get; set; } = [];
    public List<string> BlackListOverbought { get; set; } = [];

    // What symbols to show in the information dashboard
    public List<string> ShowSymbolInformation { get; set; } =
        new(["BTC", "PAXG", "ETH", "XRP", "SOL", "DOGE", "ADA", "ZEC", "BNB"]);
}