namespace CryptoSbmScanner.Settings;

// Storage used for signal (long/short) and trading (long/short)
[Serializable]
public class SettingsTextual
{
    public List<string> Interval { get; set; } = new()
    {
        "1m",
        "2m"
    };

    public List<string> Strategy { get; set; } = new()
    {
        "sbm1",
        "sbm2",
        "sbm3"
    };

    // Welk interval moet geschikt zijn?
    public List<string> Trend { get; set; } = new();

    // Via interval + Value (range needed?)
    public Dictionary<string, decimal> Barometer { get; set; } = new();

    // Wel of niet?
    // The black- and whitelist
    //public SortedList<string, bool> BlackList { get; } = new();
    //public SortedList<string, bool> WhiteList { get; } = new();
}

