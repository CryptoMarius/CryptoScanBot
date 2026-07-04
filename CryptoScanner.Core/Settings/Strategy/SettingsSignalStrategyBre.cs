namespace CryptoScanner.Core.Settings.Strategy;

// "bre" — Buddy Reversion Engine (BRE): Donchian-based outer bands (the gray "plateaus") with an
// EMA+ATR middle cloud (DIDO). A long alert fires when the Low breaks the macro LOWER band; a short
// when the High breaks the macro UPPER band, with optional HMA-trend / RSI / Stochastic-RSI filters.
// These parameters drive BOTH the chart drawer (BreBands) and the bre signal (BreBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class SettingsSignalStrategyBre : SettingsSignalStrategyBase
{
    // Donchian lookback for the outer bands, computed over the PREVIOUS BandLength candles
    // (Pine: ta.highest(high[1], len) / ta.lowest(low[1], len); default 20).
    public int BandLength { get; set; } = 20;

    // Outer band multiplier: middle ± halfRange * (OuterMult / 2.5) (Pine default 3.2).
    public double OuterMult { get; set; } = 3.2;

    // DIDO cloud: EMA(DidoLength) basis ± ATR(DidoLength) * DidoMult (chart only, Pine defaults 20 / 1.0).
    public int DidoLength { get; set; } = 20;
    public double DidoMult { get; set; } = 1.0;

    // Optional WGHM (Hull MA) trend filter: a long also requires close > HMA(HmaLength),
    // a short requires close < HMA(HmaLength).
    public bool UseTrendFilter { get; set; } = false;
    public int HmaLength { get; set; } = 55;

    // Optional RSI filter: a long requires RSI (on this or the previous candle) <= RsiOversold,
    // a short requires RSI >= RsiOverbought.
    public bool UseRsiFilter { get; set; } = false;
    public int RsiLength { get; set; } = 14;
    public int RsiOverbought { get; set; } = 70;
    public int RsiOversold { get; set; } = 30;

    // Optional Stochastic-RSI filter: a long requires %K or %D <= StochOversold,
    // a short requires %K or %D >= StochOverbought.
    public bool UseStochFilter { get; set; } = false;
    public int StochLength { get; set; } = 14;
    public int StochKLength { get; set; } = 3;
    public int StochDLength { get; set; } = 3;
    public int StochOverbought { get; set; } = 80;
    public int StochOversold { get; set; } = 20;

    // Allow consecutive signals while price stretches further beyond the band within one break run
    // (Pine "HYPE-stijl": a new label on every higher High / lower Low). When off only the first
    // candle of a break run fires.
    public bool AllowStack { get; set; } = true;

    // When true the signal hands its own stop-loss percentage (the band-width % printed in the
    // chart label) to the trader via OverrideSlPercentage. When false the signal returns null,
    // so the trader falls back to the default percentage stop-loss from the trading settings.
    public bool UseStopLoss { get; set; } = true;

    public SettingsSignalStrategyBre() : base()
    {
    }
}
