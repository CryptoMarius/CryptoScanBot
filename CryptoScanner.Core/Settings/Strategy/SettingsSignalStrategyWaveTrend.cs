namespace CryptoScanner.Core.Settings.Strategy;

// WaveTrend Oscillator [LazyBear] — WT_LB.
// Long  : WT1 crosses up through the OS level after a sufficient excursion below it.
// Short : mirror — cross down through OB after a sufficient excursion above it.
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

    // Excursion window — number of bars ending at the candle before the cross over which
    // both area and peak depth are evaluated.
    public int LookbackBars { get; set; } = 10;

    // Minimum total area below OS (long) or above OB (short) inside the lookback window.
    // Units: WT1-points · bars. E.g. 30 = average depth of 3 over 10 bars, or depth of 10
    // over 3 bars. Filters out WT1 wiggling around the OS/OB line where dwell would pass
    // but the excursion has almost no substance.
    public decimal MinAreaInZone { get; set; } = 30m;

    // How far past OS/OB the WT1 extreme must have reached inside the lookback window.
    // Long  : min(WT1) must be ≤ (OsLevel - DeepLevelOffset).
    // Short : max(WT1) must be ≥ (ObLevel + DeepLevelOffset).
    // Guarantees the excursion was genuinely extreme, not just prolonged at marginal depth.
    public decimal DeepLevelOffset { get; set; } = 15m;

    public SettingsSignalStrategyWaveTrend() : base()
    {
    }
}
