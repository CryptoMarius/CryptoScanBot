namespace CryptoScanner.Core.Settings.Strategy;

// WaveTrend Oscillator [LazyBear] — WT_LB.
// Long  : after WT1 has spent ≥ MinBarsBeyondOsOb bars below OsLevel inside the lookback,
//         fire when WT1 crosses up through −RecoveryLevel (returning toward neutral).
// Short : mirror — bars above ObLevel, cross down through +RecoveryLevel.
[Serializable]
public class SettingsSignalStrategyWaveTrend : SettingsSignalStrategyBase
{
    // Channel length (n1) — default 10 per LazyBear
    public int ChannelLength { get; set; } = 10;

    // Average length (n2) — default 21 per LazyBear
    public int AverageLength { get; set; } = 21;

    // Deep overbought / oversold levels (LazyBear: ±60). WT1 must have been beyond these
    // inside the lookback window for the qualifier to pass.
    public decimal OsLevel { get; set; } = -60m;
    public decimal ObLevel { get; set; } = 60m;

    // Recovery levels — the actual cross trigger. The signal fires when WT1 crosses up
    // through OsRecoveryLevel (long) or down through ObRecoveryLevel (short), i.e. when
    // WT1 has recovered from the extreme back toward neutral.
    public decimal OsRecoveryLevel { get; set; } = -50m;
    public decimal ObRecoveryLevel { get; set; } = 50m;

    // Trend filter: long only when close > SMA200, short only when close < SMA200.
    // (LazyBear's recipe specifies EMA200; SMA200 is a close-enough substitute.)
    public bool RequireTrendFilter { get; set; } = true;

    // Lookback window — number of bars ending at the candle before the recovery cross over
    // which the OS/OB-visit qualifier is evaluated.
    public int LookbackBars { get; set; } = 10;

    // Minimum number of bars within the lookback where WT1 was beyond the OS/OB level.
    // Need not be consecutive. Filters out "barely touched" or wiggling-around-the-line
    // excursions that never genuinely committed to the extreme zone.
    public int MinBarsBeyondOsOb { get; set; } = 3;

    public SettingsSignalStrategyWaveTrend() : base()
    {
    }
}
