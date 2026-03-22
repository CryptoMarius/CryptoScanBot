namespace CryptoScanner.Core.Settings;

/// <summary>
/// Adaptive performance feedback settings.
///
/// The performance monitor periodically queries the signal database and calculates the win rate
/// per (strategy, side) over a rolling time window. When a strategy falls below the configured
/// threshold it is temporarily blocked from generating new signals.
///
/// Key design decisions:
///   - Filter is time-based (MaxLookbackDays), not count-based, to keep the query simple.
///   - MinSignals prevents premature blocking when there is too little data.
///   - Blocked strategies are automatically re-enabled after ReEnableHours, after which the
///     next refresh re-evaluates whether to block again.
///   - Applies to ALL strategies (including DLZ/FVG), unlike barometer/volume which are skipped
///     for zone strategies.
///
/// Typical settings:
///   MaxLookbackDays  = 7    → only use signals from the last 7 days
///   MinSignals       = 5    → need at least 5 closed signals before blocking
///   BlockThreshold   = 40   → block when win rate drops below 40%
///   ReEnableHours    = 24   → automatically re-enable after 24 hours
/// </summary>
[Serializable]
public class SettingsTextualFeedback
{
    /// <summary>When false the entire feedback system is skipped.</summary>
    public bool IsActive { get; set; } = false;

    /// <summary>Only consider signals that opened within the last N days.</summary>
    public int MaxLookbackDays { get; set; } = 7;

    /// <summary>Minimum number of closed signals required before a strategy can be blocked.</summary>
    public int MinSignals { get; set; } = 5;

    /// <summary>Win rate percentage below which a strategy is blocked (0–100).</summary>
    public decimal BlockThresholdPercent { get; set; } = 40m;

    /// <summary>Hours before a blocked strategy is automatically re-enabled for evaluation.</summary>
    public int ReEnableHours { get; set; } = 24;

    /// <summary>When true, log lines are written when a strategy is blocked or unblocked.</summary>
    public bool Log { get; set; } = true;
}


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

    // Market trend percentage
    public SettingsTextualMarketTrend MarketTrend = new();

    // Relative volume filter
    public SettingsTextualVolume Volume = new();

    // Adaptive strategy feedback filter
    public SettingsTextualFeedback Feedback = new();
}


[Serializable]
public class SettingsTextualBarometer
{
    public Dictionary<string, (decimal minValue, decimal maxValue)> List { get; set; } = [];
    public bool Log = true;
    // Minimum number of higher-timeframe barometers that must align with the signal direction (0 = disabled)
    public int MinConsensus { get; set; } = 0;
}

[Serializable]
public class SettingsTextualMarketTrend
{
    public List<(decimal minValue, decimal maxValue)> List { get; set; } = [];
    public bool Log = true;
}

/// <summary>
/// Relative volume filter settings.
///
/// Relative volume (RelVol) compares the current candle's volume against the average volume
/// of the last <see cref="Lookback"/> candles:
///   RelVol = current_candle_volume / SMA(volume, Lookback)
///
/// A RelVol of 1.0 means exactly average participation.
/// Values below 1.0 indicate below-average volume (possible false signals on thin markets).
/// Values above 1.0 indicate above-average participation (stronger confirmation).
///
/// Typical use:
///   - MinRelVol = 0.8  → filter out candles with less than 80% of average volume
///   - MaxRelVol = 999  → no upper limit (very high volume is still valid)
///   - Lookback  = 20   → rolling average over the last 20 candles
/// </summary>
[Serializable]
public class SettingsTextualVolume
{
    /// <summary>When false the filter is skipped entirely.</summary>
    public bool IsActive { get; set; } = false;

    /// <summary>Minimum relative volume required (ratio vs SMA). Default 0.8 = 80% of average.</summary>
    public decimal MinRelVol { get; set; } = 0.8m;

    /// <summary>Maximum relative volume allowed (ratio vs SMA). Default 999 = no upper limit.</summary>
    public decimal MaxRelVol { get; set; } = 999m;

    /// <summary>Number of candles used for the SMA baseline. Default 20.</summary>
    public int Lookback { get; set; } = 20;

    /// <summary>When true, a log line is written when a signal is blocked by this filter.</summary>
    public bool Log { get; set; } = true;
}


[Serializable]
public class SettingsTextualIntervalTrend
{
    public List<string> List { get; set; } = [];
    public bool Log = true;
}
