using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Bre;

// "bre" — Buddy Reversion Engine (BRE): Donchian-based outer bands (the gray "plateaus") with an
// EMA+ATR middle cloud (DIDO). A long alert fires when the Low breaks the macro LOWER band; a short
// when the High breaks the macro UPPER band, with optional HMA-trend / RSI / Stochastic-RSI filters.
// These parameters drive BOTH the chart drawer (BreBands) and the bre signal (BreBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class BreSettings : SettingsSignalStrategyBase
{
    // Donchian lookback for the outer bands, computed over the PREVIOUS BandLength candles
    // (Pine: ta.highest(high[1], len) / ta.lowest(low[1], len); default 20).
    public int BandLength { get; set; } = 20;

    // Outer band multiplier: middle ± halfRange * (OuterMult / 2.5) (Pine default 3.2).
    public double OuterMult { get; set; } = 3.2;

    // When true a long signal also requires RSI to be oversold, and a short signal requires RSI
    // to be overbought (uses the global RSI OS/OB thresholds from SettingsRsi).
    public bool UseRsiFilter { get; set; } = true;

    // When true a long signal also requires Stochastic to be oversold, and a short signal requires
    // Stochastic to be overbought (uses the global Stoch OS/OB thresholds from SettingsStoch).
    public bool RequireStochOsOb { get; set; } = false;

    // Allow consecutive signals while price stretches further beyond the band within one break run
    // (Pine "HYPE-stijl": a new label on every higher High / lower Low). When off only the first
    // candle of a break run fires.
    public bool AllowStack { get; set; } = true;

    // When true the signal hands its own stop-loss percentage (the band-width % printed in the
    // chart label) to the trader via OverrideSlPercentage. When false the signal returns null,
    // so the trader falls back to the default percentage stop-loss from the trading settings.
    public bool UseStopLoss { get; set; } = false;

    // Multi-timeframe consensus: when > 0 the signal also requires this many consecutive higher
    // timeframes to confirm the same band break condition. 0 = single-timeframe (normal behavior).
    public int TimeframeConsensusCount { get; set; } = 0;

    public BreSettings() : base()
    {
    }
}
