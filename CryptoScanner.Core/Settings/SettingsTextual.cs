namespace CryptoScanner.Core.Settings;

// Common storage for signal (long/short) and trading (long/short)
[Serializable]
public class SettingsTextual
{
    public SettingsTextual()
    {
        Interval.Add("1m");
        Interval.Add("2m");
        Interval.Add("3m");

        Strategy.Add("sbm1");
        Strategy.Add("sbm2");
        Strategy.Add("sbm3");
        Strategy.Add("stobb");
        Strategy.Add("storsi");
    }

    // Op welke interval
    public List<string> Interval { get; set; } = [];

    // Op welke strategie
    public List<string> Strategy { get; set; } = [];

    // Op welk interval moet de trend bull of bear zijn
    public SettingsTextualIntervalTrend IntervalTrend = new();

    // Via interval + Value (range needed?)
    public SettingsTextualBarometer Barometer = new();

    // The trend of the coin ITSELF, as a weighted percentage over its intervals (primary zigzag
    // setting). Not the market trend of the dashboard: that one averages this same figure over
    // every coin of the quote - see Trend.MarketTrend.
    public SettingsTextualSymbolTrend SymbolTrend = new();

    // The same for the secondary zigzag setting.
    public SettingsTextualSymbolTrend SymbolTrendSecondary = new();
}


[Serializable]
public class SettingsTextualBarometer
{
    public Dictionary<string, (decimal minValue, decimal maxValue)> List { get; set; } = [];
    public bool Log = false;
    // When false the consensus check is skipped entirely (same pattern as Volume.IsActive)
    public bool ConsensusActive { get; set; } = false;
    // Minimum number of higher-timeframe barometers that must align with the signal direction (0 = disabled)
    public int MinConsensus { get; set; } = 0;
}


[Serializable]
public class SettingsTextualSymbolTrend
{
    public List<(decimal minValue, decimal maxValue)> List { get; set; } = [];
    public bool Log = false;
}


[Serializable]
public class SettingsTextualIntervalTrend
{
    public List<string> List { get; set; } = [];
    public bool Log = false;
}
