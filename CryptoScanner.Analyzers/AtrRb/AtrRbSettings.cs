using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.AtrRb;

// "atrrb" — AtrRb Bands & Ribbon: a Keltner-style EMA basis with ATR-based bands.
// A long alert fires when price breaks the macro LOWER band; a short on the macro UPPER band.
// These parameters drive BOTH the chart drawer (AtrRbBands) and the atrrb signal (AtrRbBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class AtrRbSettings : SettingsSignalStrategyBase
{
    // EMA/ATR length used for the band basis (Pine default 20).
    public int Length { get; set; } = 20;

    // Macro outer-band multiplier: basis +/- ATR * OuterMult (the wide cloud; Pine tuned to 4.2).
    public double OuterMult { get; set; } = 4.2;

    // Inner ribbon multiplier: basis +/- ATR * InnerMult (chart ribbon only; default 1.0).
    public double InnerMult { get; set; } = 1.0;

    // The break must be the lowest Low (long) / highest High (short) within this trailing window
    // of bars (mirrors the Pine ta.lowest / ta.highest filter).
    public int BreakLookback { get; set; } = 5;

    // When true the signal hands its own (percentage based) stop-loss to the trader via
    // OverrideSlPrice. When false the signal returns null for the SL, so the trader falls back
    // to the default percentage stop-loss from the trading settings.
    public bool UseStopLoss { get; set; } = false;

    // Multiplier applied to the ATR when deriving the stop-loss distance: SL distance = factor * ATR(Length).
    // Default 2.0 (the original hard-coded 2x ATR). Typical values: 1, 2, 2.5, 3.
    public double StopLossAtrFactor { get; set; } = 2.0;

    // Bollinger-band width gate, applied to BollingerBandsPercentage = 100 * (upper/lower - 1).
    // A break is only flagged (signal fires / chart prints a label) when the BB width is inside
    // [BBMinPercentage, BBMaxPercentage]. A bound of 0 disables that side (so the default 0 max =
    // no upper limit). Both the atrrb signal and the chart drawer read these, so they stay in sync.
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 0.0;

    // When true a long signal also requires RSI to be oversold, and a short signal requires RSI
    // to be overbought (uses the global RSI OS/OB thresholds from SettingsRsi).
    public bool RequireRsiOsOb { get; set; } = false;

    // When true a long signal also requires Stochastic to be oversold, and a short signal requires
    // Stochastic to be overbought (uses the global Stoch OS/OB thresholds from SettingsStoch).
    public bool RequireStochOsOb { get; set; } = false;

    // Multi-timeframe consensus: when > 0 the signal also requires this many consecutive higher
    // timeframes to confirm the same band break condition. 0 = single-timeframe (normal behavior).
    public int TimeframeConsensusCount { get; set; } = 0;

    public bool OnlyIfLux5m { get; set; } = false;
    public int Lux5mPercentage { get; set; } = 50;

    public bool CheckTrendPrimaryDirection { get; set; } = false;
    public int TrendPrimaryDirectionCount { get; set; } = 2;
    public bool CheckTrendSecondaryDirection { get; set; } = false;
    public int TrendSecondaryDirectionCount { get; set; } = 2;

    public bool CheckPriceAboveMa200 { get; set; } = false;
    public decimal Ma200MinDistancePercentage { get; set; } = 0m;
    public int Ma200ConfirmationCandles { get; set; } = 0;

    // Zone confirmations — when any of these is enabled, at least one of the enabled
    // zone rejections must match (OR). All disabled = no zone filter.
    public bool UseDlzZone { get; set; } = false;
    public bool UseFvgZone { get; set; } = false;
    public bool UseSmcZone { get; set; } = false;

    public AtrRbSettings() : base()
    {
    }
}
