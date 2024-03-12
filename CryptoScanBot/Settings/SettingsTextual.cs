namespace CryptoScanBot.Settings;

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
    }

    // Op welke interval
    public List<string> Interval { get; set; } = [];

    // Op welke strategie
    public List<string> Strategy { get; set; } = [];

    // Op welk interval moet de trend bull of bear zijn
    public SettingsTextualIntervalTrend IntervalTrend = new();

    // Via interval + Value (range needed?)
    public SettingsTextualBarometer Barometer = new();

    // Market trend percentage
    public SettingsTextualMarketTrend MarketTrend = new();
}


[Serializable]
public class SettingsTextualBarometer
{
    public Dictionary<string, (decimal minValue, decimal maxValue)> List { get; set; } = [];
    public bool Log = true;
}

[Serializable]
public class SettingsTextualMarketTrend
{
    public List<(decimal minValue, decimal maxValue)> List { get; set; } = [];
    public bool Log = true;
}

[Serializable]
public class SettingsTextualIntervalTrend
{
    public List<string> List { get; set; } = [];
    public bool Log = true;
}
