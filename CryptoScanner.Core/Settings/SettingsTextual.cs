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

    // Optional condition on the market breadth of the 1h measurement: the percentage of the coins of
    // the quote that rose over the last hour must lie between these two (0..100). Off by default,
    // and with 0..100 it lets everything through anyway (open point 11, phase 3).
    public bool BreadthActive { get; set; } = false;
    public decimal BreadthMinimum { get; set; } = 0m;
    public decimal BreadthMaximum { get; set; } = 100m;
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

    // Demand the trend AGAINST the trade direction: bearish for a long, bullish for a short. Built
    // for dbr, which buys the lower band: over six runs on the current code every bit of its long
    // profit came while the 1h trend was falling (+936,09 over 1536 trades) and the longs in a rising
    // 1h trend lost (-30,48 over 4393), and no setting could express that (open point 51). Off is
    // what every run so far measured.
    public bool Inverted = false;
}
