namespace CryptoScanner.Core.Settings.Strategy;

// wtlb.stoch — combined WaveTrend (LazyBear) and Stochastic %K mid-cross.
//
// Long  : WT1 crosses up through WtCrossLongLevel (default -40) after WT1 has spent
//         WtConsecutiveBarsBelowAboveZero bars uninterruptedly below 0, while Stoch %K
//         crossed up through StochCenterLevel (50) within the last StochCrossLookback bars.
// Short : mirror — WT1 crosses down through WtCrossShortLevel (default +60) after WT1
//         has been uninterruptedly above 0, while Stoch %K crossed down through 50.
[Serializable]
public class SettingsSignalStrategyWtLbStoch : SettingsSignalStrategyBase
{
    // WT_LB indicator parameters (LazyBear defaults).
    public int ChannelLength { get; set; } = 10;
    public int AverageLength { get; set; } = 21;

    // Cross trigger levels for WT1. Intentionally asymmetric per user spec:
    // long fires earlier (further from neutral) than short.
    public decimal WtCrossLongLevel { get; set; } = -40m;
    public decimal WtCrossShortLevel { get; set; } = 60m;

    // WT1 must have been UNINTERRUPTEDLY below 0 (long) or above 0 (short) for at least
    // this many bars ending at the bar just before the cross. Single bar on the wrong
    // side of zero breaks the streak.
    public int WtConsecutiveBarsBelowAboveZero { get; set; } = 3;

    // Stoch %K centerline. The cross trigger.
    public decimal StochCenterLevel { get; set; } = 50m;

    // How many recent candles to scan for the Stoch %K cross of StochCenterLevel.
    // 2 means the cross must have occurred on the current bar or the one before it.
    public int StochCrossLookback { get; set; } = 2;

    // Optional trend filter — long only when close > SMA200, short only when close < SMA200.
    // Off by default (experimental strategy; let the user opt-in once tuned).
    public bool RequireTrendFilter { get; set; } = false;

    public SettingsSignalStrategyWtLbStoch() : base()
    {
    }
}
