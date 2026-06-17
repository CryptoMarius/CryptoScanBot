namespace CryptoScanner.Core.Settings.Strategy;

// "atrrb" — Mean Reversion Bands (Bollinger + ATR), reverse-engineered from the trading-course chart.
// The band is a Bollinger basis with an added fast-ATR term: SMA(Length) +/- (Mult * stdev + AtrMult * ATR).
// A long alert fires when price breaks the LOWER band while RSI is oversold; a short on the UPPER band
// while RSI is overbought. These parameters drive BOTH the chart drawer (AtrRbBands) and the signal
// (AtrRbBandsHelper), so the chart and the alert always stay in sync.
[Serializable]
public class SettingsSignalStrategyAtrRb : SettingsSignalStrategyBase
{
    // SMA / standard-deviation window for the Bollinger basis (fit against the reference: ~90).
    public int Length { get; set; } = 90;

    // Standard-deviation multiplier: basis +/- Mult * stdev (the Bollinger part).
    public double Mult { get; set; } = 2.5;

    // Fast ATR length added on top of the stdev band (brings back the angular shape).
    public int AtrLength { get; set; } = 14;

    // Multiplier on the fast ATR term: band = Mult * stdev + AtrMult * ATR(AtrLength). 0 = pure Bollinger.
    public double AtrMult { get; set; } = 1.0;

    // RSI confluence: only fire a sell on an upper-band break when RSI is overbought, and a buy on a
    // lower-band break when RSI is oversold. The overbought/oversold LEVELS are taken from the general
    // RSI settings (Indicators tab: GlobalData.Settings.General.SettingsRsi), so all strategies share them.
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
    public bool UseCooldown { get; set; } = true;
    public int CooldownBars { get; set; } = 10;

    // When true the signal hands its own (percentage based) stop-loss to the trader via
    // OverrideSlPercentage. When false the signal returns null for the SL, so the trader falls back
    // to the default percentage stop-loss from the trading settings.
    public bool UseStopLoss { get; set; } = true;

    // Multiplier applied to the ATR when deriving the stop-loss distance: SL distance = factor * ATR(AtrLength)%.
    public double StopLossAtrFactor { get; set; } = 2.0;

    // Zone confirmations — when any of these is enabled, the band break must ALSO be a rejection at
    // one of the enabled zone types (OR). All disabled = no zone filter. Same logic/extensions as the
    // StoRsi zone checkboxes (WasRejectedAtDlz/Fvg/SmcZone).
    public bool UseDlzZone { get; set; } = false;
    public bool UseFvgZone { get; set; } = false;
    public bool UseSmcZone { get; set; } = false;

    public SettingsSignalStrategyAtrRb() : base()
    {
    }
}
