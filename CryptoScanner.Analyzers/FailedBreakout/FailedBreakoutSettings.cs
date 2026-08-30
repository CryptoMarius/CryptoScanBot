using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FailedBreakout;

/// <summary>
/// Price sets a new high or low over a lookback window and then closes back inside it. The break
/// that did not hold - an upthrust going up, a spring going down.
/// <para>
/// Built as a strategy of its own rather than as a filter on purpose. Everything we added as a
/// filter this month cost money (waiting, the adverse-move limit, the band range index), while the
/// candlestick shapes measured as strategies in their own right did make money - four of seven, at
/// full trade volume. So this competes, it does not filter.
/// </para>
/// </summary>
[Serializable]
public class FailedBreakoutSettings : SettingsSignalStrategyBase
{
    /// <summary>
    /// How many candles the level is taken from: the highest high (or lowest low) over this many
    /// candles before the break window. Longer means a level fewer people would argue with, and
    /// fewer signals.
    /// </summary>
    public int LookbackCandles { get; set; } = 20;

    /// <summary>
    /// How recently the break must have happened, counted back from the candle being evaluated and
    /// including it. One means the break and the close back inside are the same candle, which is the
    /// classic single-candle upthrust.
    /// </summary>
    public int BreakWithinCandles { get; set; } = 3;

    /// <summary>
    /// How far beyond the level the break has to have gone, as a percentage OF THE LEVEL. Zero
    /// accepts a break by a single tick. A percentage rather than an amount for the same reason the
    /// candle patterns use one: an absolute threshold measures the price of the coin instead of the
    /// move (see CandlePatternHelper and Tools/PatternScan/README.md).
    /// </summary>
    public decimal MinimumBreakPercentage { get; set; } = 0m;

    public FailedBreakoutSettings() : base()
    {
    }
}
