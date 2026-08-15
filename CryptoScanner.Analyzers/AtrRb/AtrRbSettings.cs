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
    [SettingCaption("Length (EMA/ATR)",
        Tooltip = "EMA/ATR length used for the band basis (Pine default 20).")]
    public int Length { get; set; } = 20;

    // Macro outer-band multiplier: basis +/- ATR * OuterMult (the wide cloud; Pine tuned to 4.2).
    [SettingCaption("Outer mult",
        Tooltip = "Macro outer-band multiplier: basis ± ATR × OuterMult (the wide cloud; Pine ≈ 4.2). Higher = wider bands = fewer signals.")]
    public double OuterMult { get; set; } = 4.2;

    // The break must be the lowest Low (long) / highest High (short) within this trailing window
    // of bars (mirrors the Pine ta.lowest / ta.highest filter).
    [SettingCaption("Break lookback (bars)",
        Tooltip = "The break must be the lowest Low (long) / highest High (short) within this trailing number of bars (avoids label/signal spam). Pine default 5.")]
    public int BreakLookback { get; set; } = 5;

    // When true the signal hands its own (percentage based) stop-loss to the trader via
    // OverrideSlPrice. When false the signal returns null for the SL, so the trader falls back
    // to the default percentage stop-loss from the trading settings.
    [SettingCaption("Use stop-loss",
        Tooltip = "When on, the signal passes its own percentage based stop-loss to the trader. When off, no stop-loss is handed over (null) and the trader uses its default percentage stop-loss.")]
    public bool UseStopLoss { get; set; } = false;

    // Multiplier applied to the ATR when deriving the stop-loss distance: SL distance = factor * ATR(Length).
    // Default 2.0 (the original hard-coded 2x ATR). Typical values: 1, 2, 2.5, 3.
    [SettingCaption("Stop-loss ATR factor",
        Tooltip = "Multiplier on the ATR for the stop-loss distance: SL distance = factor × ATR(Length). Default 2.0 (the original 2× ATR). Typical values: 1, 2, 2.5, 3.")]
    public double StopLossAtrFactor { get; set; } = 2.0;

    // Bollinger-band width gate, applied to BollingerBandsPercentage = 100 * (upper/lower - 1).
    // A break is only flagged (signal fires / chart prints a label) when the BB width is inside
    // [BBMinPercentage, BBMaxPercentage]. A bound of 0 disables that side (so the default 0 max =
    // no upper limit). Both the atrrb signal and the chart drawer read these, so they stay in sync.
    [SettingCaption("BB width min %",
        Tooltip = "Minimum Bollinger-band width (BB% = 100 × (upper/lower − 1)) for a break to be flagged. 0 disables the lower bound. Applies to both the signal and the chart labels.")]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("BB width max %",
        Tooltip = "Maximum Bollinger-band width (BB% = 100 × (upper/lower − 1)) for a break to be flagged. 0 disables the upper bound. Applies to both the signal and the chart labels.")]
    public double BBMaxPercentage { get; set; } = 0.0;

    // When true a long signal also requires RSI to be oversold, and a short signal requires RSI
    // to be overbought (uses the global RSI OS/OB thresholds from SettingsRsi).
    [SettingCaption("Require RSI oversold/overbought",
        Tooltip = "When on, a long signal also requires RSI to be oversold and a short signal requires RSI to be overbought (uses the global RSI thresholds).")]
    public bool RequireRsiOsOb { get; set; } = false;

    // When true a long signal also requires Stochastic to be oversold, and a short signal requires
    // Stochastic to be overbought (uses the global Stoch OS/OB thresholds from SettingsStoch).
    [SettingCaption("Require Stochastic oversold/overbought",
        Tooltip = "When on, a long signal also requires Stochastic to be oversold and a short signal requires Stochastic to be overbought (uses the global Stoch thresholds).")]
    public bool RequireStochOsOb { get; set; } = false;

    // Number of consecutive higher timeframes that must show the same band break before the signal
    // fires. 0 = this timeframe only (normal behaviour). Lives here rather than in the global entry
    // conditions because only a band strategy has a band break to confirm.
    [SettingCaption("Band break confirmation on higher timeframes",
        Tooltip = "Number of consecutive higher timeframes that must show the same band break. 0 = this timeframe only. Example: 1 means the next higher timeframe has to break its band as well. Missing indicator data on a higher timeframe counts as no confirmation.")]
    public int BandBreakConfirmationCount { get; set; } = 0;

    public AtrRbSettings() : base()
    {
    }
}
