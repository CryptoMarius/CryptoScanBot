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

    // Skip the signal while the market RSI (the 1h RSI of every coin of the quote, averaged) lies
    // in a band - see Barometer.MarketRsi.
    public SettingsTextualMarketRsi MarketRsi = new();

    // Only step in while the market state of the quote (a score from -100 to +100) lies in a band -
    // see Barometer.MarketState.
    public SettingsTextualMarketState MarketState = new();

    // Trader only: close a position once the whole market moved this far against it since the
    // entry - see Barometer.MarketMove.
    public SettingsTextualMarketExit MarketExit = new();

    // Entry filters from the market-regime study (16-10-2026), see Signal.EntryFilters
    public SettingsTextualBtcMove BtcMove = new();
    public SettingsTextualWeekdays Weekdays = new();
    public SettingsTextualMarketRsiCross MarketRsiCross = new();
    public SettingsTextualHigherInterval HigherInterval = new();

    // Trader only: at most this many new positions on this side within an hour
    public SettingsTextualEntryLimit EntryLimit = new();
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
public class SettingsTextualMarketRsi
{
    // Off by default, so every run and setting so far keeps its result. Measured on Session2
    // runs 2, 3 (dbr) and 10, 11 (mac) on 12-10-2026: longs entered while the market RSI was
    // between 45 and 55 lost money in all four runs while the rest was positive.
    public bool Active = false;
    // Skip when Minimum <= market RSI <= Maximum (0..100)
    public decimal Minimum = 45m;
    public decimal Maximum = 55m;
    public bool Log = false;
}


[Serializable]
public class SettingsTextualMarketState
{
    // Off by default, so every run and setting so far keeps its result. Measured on Session2 on
    // 13/14-10-2026 (study market-regime): the long profit of dbr, failedbreakout, candlepattern and
    // mac came from entries at a score of +60 and higher.
    public bool Active = false;
    // Step in when Minimum <= score <= Maximum (-100..+100)
    public decimal Minimum = -100m;
    public decimal Maximum = 100m;
    // The interval of the BTC part of the score. 4h told rising from falling best in the study;
    // 1h, 2h and 3h react earlier but predict less.
    public string Interval = "4h";
    public bool Log = false;
}


[Serializable]
public class SettingsTextualMarketExit
{
    // Off by default, so every run and setting so far keeps its result. Measured on Session2 on
    // 14-10-2026: positions during which the market moved 3% against them lost money in every
    // strategy, while all the other positions earned.
    public bool Active = false;
    // How far the market (all coins of the quote, averaged) may move against the position since the
    // entry before it is closed, in percent
    public decimal Percentage = 3m;
    // Watch BTC of the quote instead of all coins
    public bool UseBtc = false;
}


[Serializable]
public class SettingsTextualBtcMove
{
    // Only step in while the % change of BTC over the last Hours whole hours lies between Minimum and
    // Maximum. Off by default. Measured 12-10-2026: dbr long after BTC fell more than 3% in 24 hours
    // lost money, and mac long after a BTC drop in the last 4 hours.
    public bool Active = false;
    public int Hours = 24;
    public decimal Minimum = -100m;
    public decimal Maximum = 100m;
}


[Serializable]
public class SettingsTextualWeekdays
{
    // Only step in on these days of the week (UTC, 0 = Sunday .. 6 = Saturday). Off by default.
    // Measured: dbr long earned in the weekend and lost during the week (runs 2, 3 and 55).
    public bool Active = false;
    public List<int> Days { get; set; } = [0, 1, 2, 3, 4, 5, 6];
}


[Serializable]
public class SettingsTextualMarketRsiCross
{
    // Only step in when the market RSI came out of the zone within the last Hours whole hours: long
    // when one of those hours was below Level and the newest is at or above it, short the mirror
    // image (above Level, now at or below it - so a short side wants something like 65). Off by default.
    public bool Active = false;
    public decimal Level = 35m;
    public int Hours = 12;
}


[Serializable]
public class SettingsTextualHigherInterval
{
    // Conditions on the coin itself on a higher interval: its RSI 14 between RsiMinimum and
    // RsiMaximum, and its close against the EMA200 of that interval (1 above, -1 below, 0 either).
    // Off by default. Measured 15-10-2026: a short on a weak coin (low 1h RSI, below the EMA200) did
    // better in all six runs measured.
    public bool Active = false;
    public string Interval = "1h";
    public decimal RsiMinimum = 0m;
    public decimal RsiMaximum = 100m;
    public int Ema200 = 0;
}


[Serializable]
public class SettingsTextualEntryLimit
{
    // Trader only: at most MaxPerHour new positions on this side within an hour. Off by default.
    // Measured: failedbreakout longs entered together with 6-20 others within the hour lost 4.904.
    public bool Active = false;
    public int MaxPerHour = 10;
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
