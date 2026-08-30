using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbRsiEngulfing;

// "dbr" — Donchian Breakout Reversion (DBR): Donchian-based outer bands (the gray "plateaus") with an
// EMA+ATR middle cloud (DIDO). A long alert fires when the Low breaks the macro LOWER band; a short
// when the High breaks the macro UPPER band, with optional HMA-trend / RSI / Stochastic-RSI filters.
// These parameters drive BOTH the chart drawer (DbrBands) and the dbr signal (DbrBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class BbRsiEngulfingSettings : SettingsSignalStrategyBase
{

    // The fourth check of this strategy is called "engulfing" but tests whether the candle closes
    // above the HIGH of the previous one (below the LOW for a short). That is a breakout condition,
    // not an engulfing: it says nothing about where this candle OPENED, nor about the colour or the
    // size of either body. Measured on three symbols and 75 700 candles of 15m on 29-08-2026: of the
    // 16 296 candles the rule fires on, 12 514 (77%) are not an engulfing at all, while 2 937 (44%)
    // of the real engulfings are missed because they close between the previous open and its high.
    //
    // Left as it was, because that is what all the runs so far measured. Switching this on uses the
    // classic definition instead - body covers body, opposite colours - through
    // CandlePatternHelper, the same code the candlepattern strategy uses.
    public bool UseStrictEngulfing { get; set; } = false;

    public BbRsiEngulfingSettings() : base()
    {
    }
}
