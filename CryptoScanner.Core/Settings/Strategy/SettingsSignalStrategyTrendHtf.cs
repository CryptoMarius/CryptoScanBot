namespace CryptoScanner.Core.Settings.Strategy;

// Settings for the combined-trend strategy (SignalTrendHtfLong/Short).
// Composes: optional HTF bias filter + optional ADX regime filter + BOS/CHoCH
// structural trigger + pullback-pivot entry. Filters are independently toggleable
// so enabling the strategy doesn't silently change behavior — they default off
// and are turned on per backtest.
[Serializable]
public class SettingsSignalStrategyTrendHtf : SettingsSignalStrategyBase
{
    // -- HTF (Higher Timeframe) bias filter --
    // When enabled, refuses Long signals while the HTF Primary trend is Bearish
    // (and the mirror for Short).
    public bool HtfFilterEnabled { get; set; } = true;

    // How many higher timeframes to combine for the bias. 1 = "next HTF only",
    // 2 = "next two HTFs combined" (more conservative, also more sideways verdicts).
    public int HtfLevels { get; set; } = 2;


    // -- ADX regime filter --
    // When enabled, the strategy only fires when ADX on the trading interval is
    // above AdxMinValue — i.e. the market is actually trending, not chopping.
    public bool AdxFilterEnabled { get; set; } = false;

    // Threshold below which the strategy treats the market as ranging.
    // Wilder's classic cutoff is 20-25; crypto often warrants a slightly higher value.
    public double AdxMinValue { get; set; } = 22.0;


    // -- Anti-stale / give-up --
    // Maximum age (in candles) of the CHoCH event that can still result in a fire
    // when the pullback break finally happens. Anything older is treated as stale.
    public int MaxEventAgeCandles { get; set; } = 35;

    // After IsSignal fires, the trader has this many candles to actually open a position
    // before GiveUp returns true.
    public int GiveUpCandles { get; set; } = 20;


    public SettingsSignalStrategyTrendHtf() : base()
    {
    }
}
