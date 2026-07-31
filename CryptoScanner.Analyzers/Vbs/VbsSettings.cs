using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Vbs;

// VBS (VWAP Band Strategy) — Mean Reversion Bands (volume-weighted VWAP bands), reverse-engineered from the
// trading-course chart. The band is a rolling VWAP basis with a volume-weighted stdev envelope, plus an
// optional fast-ATR term: VWMA(hlc3, Length) +/- (Mult * vwStdev(hlc3, Length) + AtrMult * ATR(AtrLength)).
// It is NOT a Bollinger band (no SMA of close, no plain stdev). A long alert fires when price breaks the
// LOWER band while RSI is oversold; a short on the UPPER band while RSI is overbought. These parameters
// drive BOTH the chart drawer (VbsBands) and the signal (VbsBandsHelper), so the chart and the alert always
// stay in sync.
[Serializable]
public class VbsSettings : SettingsSignalStrategyBase
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

    // Stop-loss = Entry -/+ ACS%, where ACS (Average Candle Size) = AcsFactor * SMA((high-low)/close, AcsLength) * 100.
    // Reverse-engineered from the reference (TradingBuddy): the SL distance % equals the average candle size %.
    // Defaults (2.17 / 50) were fit against live signals. The SL% is handed to the trader via
    // OverrideSlPercentage when UseStopLoss is on.
    public double AcsFactor { get; set; } = 2.17;
    public int AcsLength { get; set; } = 50;

    // Take-profit = Entry -/+ RiskRewardRatio * SL-distance, i.e. TP% = RiskRewardRatio * ACS%. When on,
    // the signal hands this single TP to the trader via OverrideProfitPercentage (replacing the global TP
    // grid for this position). RiskRewardRatio determines how far the TP sits (1.0 = same distance as the
    // stop-loss = 1:1; higher = further away).
    public bool UseTakeProfit { get; set; } = false;
    public double RiskRewardRatio { get; set; } = 1.0;

    // Bollinger-band width gate, applied to BollingerBandsPercentage = 100 * (upper/lower - 1).
    // A break is only flagged (signal fires / chart prints a label) when the BB width is inside
    // [BBMinPercentage, BBMaxPercentage]. A bound of 0 disables that side (so the default 0 max =
    // no upper limit). Both the atrrb signal and the chart drawer read these, so they stay in sync.
    public double BBMinPercentage { get; set; } = 1.50;
    public double BBMaxPercentage { get; set; } = 0.0;

    // When true a long signal also requires Stochastic to be oversold, and a short signal requires
    // Stochastic to be overbought (uses the global Stoch OS/OB thresholds from SettingsStoch).
    public bool RequireStochOsOb { get; set; } = false;

    public VbsSettings() : base()
    {
        SoundFileLong = "sound-vbs-oversold.wav";
        SoundFileShort = "sound-vbs-overbought.wav";
    }
}
