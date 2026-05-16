namespace CryptoScanner.Core.Settings.Strategy;

// WaveTrend Oscillator [LazyBear] — WT_LB.
// Long  : WT1 crosses above WT2 while WT was in the oversold zone, plus an optional trend filter.
// Short : mirror — cross down in the overbought zone.
[Serializable]
public class SettingsSignalStrategyWaveTrend : SettingsSignalStrategyBase
{
    // Channel length (n1) — default 10 per LazyBear
    public int ChannelLength { get; set; } = 10;

    // Average length (n2) — default 21 per LazyBear
    public int AverageLength { get; set; } = 21;

    // Hard overbought / oversold levels (LazyBear: ±60).
    // The softer ±53 levels from the original Pine script are not used.
    public decimal OsLevel { get; set; } = -60m;
    public decimal ObLevel { get; set; } = 60m;

    // Trend filter: long only when close > SMA200, short only when close < SMA200.
    // (LazyBear's recipe specifies EMA200; SMA200 is a close-enough substitute for our purposes.)
    public bool RequireTrendFilter { get; set; } = true;

    // Minimum number of consecutive bars (ending at the candle before the cross) on which
    // WT1 must have remained inside the OS/OB zone. Filters out signals where WT1 just
    // wiggles around the OS/OB line on every candle. 1 = no dwell, 3 = sane default.
    public int MinBarsInZone { get; set; } = 3;

    public SettingsSignalStrategyWaveTrend() : base()
    {
    }
}
