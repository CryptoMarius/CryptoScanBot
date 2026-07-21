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

    // Fast ATR length added on top of the vw-stdev band (optional angular shape).
    public int AtrLength { get; set; } = 14;

    // Multiplier on the fast ATR term: band = Mult * vwStdev + AtrMult * ATR(AtrLength). 0 = pure VWAP bands.
    public double AtrMult { get; set; } = 0.0;

    // Optional "volume surge" widening, reverse-engineered from TradingBuddy's served bands: after a
    // recent volume spike the band widens. surge = max(volume over the last VolumeSurgeLength bars) /
    // SMA(volume, Length). The effective multiplier becomes Mult + VolumeSurgeFactor * max(0, surge -
    // VolumeSurgeThreshold), so normal volume (surge <= threshold) leaves the pure VWAP band untouched.
    // Off by default; enabling it brings the bands ~closer to TradingBuddy's (explains ~45-48% of their
    // widening, not bit-exact). Defaults (5 / 1.05 / 0.031) were fit on BTC/ETH/SOL/RUNE/XRP/DOGE 1h+4h.
    public bool UseVolumeSurge { get; set; } = false;
    public int VolumeSurgeLength { get; set; } = 5;
    public double VolumeSurgeThreshold { get; set; } = 1.05;
    public double VolumeSurgeFactor { get; set; } = 0.031;

    // RSI confluence: only fire a sell on an upper-band break when RSI is overbought, and a buy on a
    // lower-band break when RSI is oversold. The overbought/oversold LEVELS are taken from the general
    // RSI settings (Indicators tab: GlobalData.Settings.General.SettingsRsi), so all strategies share them.
    // TODO: Rename to RequireRsiOsOb
    public bool UseRsiFilter { get; set; } = true;

    // Symmetric slide ("glijbaan") filter — Kaufman efficiency ratio over SlideWindow bars.
    // When enabled: suppress LONG signals during a DOWN-slide and SHORT signals during an UP-slide
    // (don't trade into an ongoing, efficient, one-way move). All directions are derived from the same
    // efficiency + minimum-move thresholds.
    public bool UseSlideFilter { get; set; } = false;
    public int SlideWindow { get; set; } = 40;
    public double SlideMinEfficiency { get; set; } = 0.35;
    public double SlideMinMovePercent { get; set; } = 1.0;

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

    // Zone confirmations — when any of these is enabled, the band break must ALSO be a rejection at
    // one of the enabled zone types (OR). All disabled = no zone filter. Same logic/extensions as the
    // StoRsi zone checkboxes (WasRejectedAtDlz/Fvg/SmcZone).
    public bool UseDlzZone { get; set; } = false;
    public bool UseFvgZone { get; set; } = false;
    public bool UseSmcZone { get; set; } = false;

    public BabaSettings() : base()
    {
    }
}
