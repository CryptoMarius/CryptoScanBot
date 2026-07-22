using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Baba;

// "baba" — Mean Reversion Bands (volume-weighted VWAP bands), reverse-engineered from the trading-course
// chart. The band is a rolling VWAP basis with a volume-weighted stdev envelope, plus an optional fast-ATR
// term: VWMA(hlc3, Length) +/- (Mult * vwStdev(hlc3, Length) + AtrMult * ATR(AtrLength)). It is NOT a
// Bollinger band (no SMA of close, no plain stdev). A long alert fires when price breaks the LOWER band
// while RSI is oversold; a short on the UPPER band while RSI is overbought. These parameters drive BOTH the
// chart drawer (BabaBands) and the signal (BabaBandsHelper), so the chart and the alert always stay in sync.
[Serializable]
public class BabaSettings : SettingsSignalStrategyBase
{
    // VWMA / volume-weighted-stdev window for the VWAP basis (fit against the reference: 50).
    public int Length { get; set; } = 50;

    // Volume-weighted-stdev multiplier: basis +/- Mult * vwStdev (the VWAP-band part; fit ~2.5).
    public double Mult { get; set; } = 2.5;

    // RSI confluence: only fire a sell on an upper-band break when RSI is overbought, and a buy on a
    // lower-band break when RSI is oversold. The overbought/oversold LEVELS are taken from the general
    // RSI settings (Indicators tab: GlobalData.Settings.General.SettingsRsi), so all strategies share them.
    // TODO: Rename to RequireRsiOsOb
    public bool UseRsiFilter { get; set; } = true;

    // Cooldown: after a signal fires, wait CooldownBars candles before a new one may appear on the same
    // symbol+interval (shared across long & short, like the Pine script). Counted from the last signal.
    //public bool UseCooldown { get; set; } = true;
    //public int CooldownBars { get; set; } = 10;

    // When true the signal hands its own (percentage based) stop-loss to the trader via
    // OverrideSlPercentage. When false the signal returns null for the SL, so the trader falls back
    // to the default percentage stop-loss from the trading settings.
    public bool UseStopLoss { get; set; } = false;

    // Stop-loss distance in vwStdev units below the lower band (long) or above the upper band (short).
    // SL price = band - SLStdevFactor * vwStdev (long) / band + SLStdevFactor * vwStdev (short).
    // Example: SLStdevFactor=1.0 → SL sits one full band-width below the break level.
    public double SLStdevFactor { get; set; } = 1.0;

    // Old ATR-based stop-loss: factor * ATR(Length)% — replaced by SLStdevFactor above.
    //public double StopLossAtrFactor { get; set; } = 2.0;

    // Bollinger-band width gate, applied to BollingerBandsPercentage = 100 * (upper/lower - 1).
    // A break is only flagged (signal fires / chart prints a label) when the BB width is inside
    // [BBMinPercentage, BBMaxPercentage]. A bound of 0 disables that side (so the default 0 max =
    // no upper limit). Both the atrrb signal and the chart drawer read these, so they stay in sync.
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 0.0;

    // When true a long signal also requires Stochastic to be oversold, and a short signal requires
    // Stochastic to be overbought (uses the global Stoch OS/OB thresholds from SettingsStoch).
    public bool RequireStochOsOb { get; set; } = false;

    public BabaSettings() : base()
    {
    }
}
