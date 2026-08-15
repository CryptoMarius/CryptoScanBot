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
    /// Balance settings (a tool from the past)
    /// </summary>
    //public SettingsBalanceBot BalanceBot { get; set; } = new();

    /// <summary>
    /// Base coins
    /// </summary>
    public SortedList<string, CryptoQuoteData> QuoteCoins { get; set; } = [];

    // White and blacklist settings
    public List<string> WhiteListOversold { get; set; } = [];
    public List<string> BlackListOversold { get; set; } = [];
    public List<string> WhiteListOverbought { get; set; } = [];
    public List<string> BlackListOverbought { get; set; } = [];

    // What symbols to show in the information dashboard
    public List<string> ShowSymbolInformation { get; set; } =
        new(["BTC", "PAXG", "ETH", "XRP", "SOL", "DOGE", "ADA", "ZEC", "BNB"]);
}