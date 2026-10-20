using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.AtrRb;

// "atrrb" — AtrRb Bands & Ribbon: a Keltner-style EMA basis with ATR-based bands.
// A long alert fires when price breaks the macro LOWER band; a short on the macro UPPER band.
// These parameters drive BOTH the chart drawer (AtrRbBands) and the atrrb signal (AtrRbBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class AtrRbSettings : SettingsSignalStrategyBase
{
    private const string GroupCandleLimits = "Candle limits";

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
    [SettingCaption("Filter on BB%",
        Tooltip = "Minimum Bollinger-band width (BB% = 100 × (upper/lower − 1)) for a break to be flagged, followed by the maximum. A bound of 0 disables that side. Applies to both the signal and the chart labels.")]
    public double BBMinPercentage { get; set; } = 1.50;

    [SettingCaption("", SameRowAs = nameof(BBMinPercentage),
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

    // --- Candle limits (the same rule as dbr, Common.BandCandleLimits; added 17-10-2026) ---

    // Upper limit on the size of the candle that breaks the band, as a multiple of the average
    // candle size over the previous CandleAverageLength candles. 0 = no limit. On dbr a candle five
    // times the average walked the whole DCA ladder in 73,8% of the cases (DbrSettings).
    [SettingCaption("Maximum candle size (times the average)", Group = GroupCandleLimits,
        Tooltip = "Skip the signal when the candle that breaks the band is more than N times as tall (high minus low) as the average of the previous candles. 0 = no limit.")]
    public double MaxCandleSizeRatio { get; set; } = 0.0;

    // Upper limit on the volume of the candle that breaks the band, as a multiple of the average
    // volume over the same window. 0 = no limit.
    [SettingCaption("Maximum candle volume (times the average)", Group = GroupCandleLimits,
        Tooltip = "Skip the signal when the candle that breaks the band has more than N times the average volume of the previous candles. 0 = no limit.")]
    public double MaxCandleVolumeRatio { get; set; } = 0.0;

    // Window the two limits above measure "normal" over: the candles BEFORE the breaking one.
    [SettingCaption("Average over N candles", Group = GroupCandleLimits,
        Tooltip = "The number of candles before the breaking one that the maximum size and volume are measured against. 20 is the default measured on dbr.")]
    public int CandleAverageLength { get; set; } = 20;

    // What to do with a signal on a candle over MaxCandleSizeRatio: 0 drops it, a value above 0
    // keeps it and moves the entry that fraction of the candle's own height beyond its close. Only
    // does something with a LIMIT entry (Settings.Trading.EntryOrderType).
    [SettingCaption("Entry retracement on a large candle", Group = GroupCandleLimits,
        Tooltip = "What to do when the candle is over the maximum size: 0 skips the signal, 1.0 keeps it and places the entry one whole candle height beyond the close (a long buys lower, a short sells higher). Needs the entry order type to be Limit.")]
    public double LargeCandleRetracementPart { get; set; } = 0.0;

    public AtrRbSettings() : base()
    {
    }
}
