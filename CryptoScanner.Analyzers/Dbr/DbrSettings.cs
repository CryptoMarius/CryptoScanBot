using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dbr;

// "dbr" — Donchian Breakout Reversion (DBR): Donchian-based outer bands (the gray "plateaus") with an
// EMA+ATR middle cloud (DIDO). A long alert fires when the Low breaks the macro LOWER band; a short
// when the High breaks the macro UPPER band, with optional HMA-trend / RSI / Stochastic-RSI filters.
// These parameters drive BOTH the chart drawer (DbrBands) and the dbr signal (DbrBandsHelper),
// so the chart and the alert always stay in sync. Defaults match the original Pine inputs.
[Serializable]
public class DbrSettings : SettingsSignalStrategyBase
{
    // Donchian lookback for the outer bands, computed over the PREVIOUS BandLength candles
    // (Pine: ta.highest(high[1], len) / ta.lowest(low[1], len); default 20).
    [SettingCaption("Band length",
        Tooltip = "Donchian lookback for the outer bands, computed over the PREVIOUS N candles (Pine default 20).")]
    public int BandLength { get; set; } = 20;

    // Outer band multiplier: middle ± halfRange * (OuterMult / 2.5) (Pine default 3.2).
    [SettingCaption("Band multiplier",
        Tooltip = "Outer band multiplier: middle ± halfRange × (mult / 2.5) (Pine default 3.2). Higher = wider bands = fewer signals.")]
    public double OuterMult { get; set; } = 3.2;

    // When true a long signal also requires RSI to be oversold, and a short signal requires RSI
    // to be overbought (uses the global RSI OS/OB thresholds from SettingsRsi).
    [SettingCaption("Require RSI overbought/oversold",
        Tooltip = "Only fire a short on an upper-band break when RSI ≥ overbought, and a long on a lower-band break when RSI ≤ oversold. The overbought/oversold levels are taken from the Indicators tab (RSI settings).")]
    public bool UseRsiFilter { get; set; } = true;

    // When true a long signal also requires Stochastic to be oversold, and a short signal requires
    // Stochastic to be overbought (uses the global Stoch OS/OB thresholds from SettingsStoch).
    [SettingCaption("Require Stochastic oversold/overbought",
        Tooltip = "When on, a long signal also requires Stochastic to be oversold and a short signal requires Stochastic to be overbought (uses the global Stoch thresholds).")]
    public bool RequireStochOsOb { get; set; } = false;

    // Allow consecutive signals while price stretches further beyond the band within one break run
    // (Pine "HYPE-stijl": a new label on every higher High / lower Low). When off only the first
    // candle of a break run fires.
    [SettingCaption("Allow stacked signals on a stronger break (HYPE style)",
        Tooltip = "When on, a new signal fires on every higher High / lower Low while price keeps stretching beyond the band. When off only the first candle of a break run fires.")]
    public bool AllowStack { get; set; } = true;

    // When true the signal hands its own stop-loss percentage (the band-width % printed in the
    // chart label) to the trader via OverrideSlPercentage. When false the signal returns null,
    // so the trader falls back to the default percentage stop-loss from the trading settings.
    [SettingCaption("Use stop-loss",
        Tooltip = "When on, the signal passes the band-width percentage (the chart label) as stop-loss to the trader. When off, no stop-loss is handed over (null) and the trader uses its default percentage stop-loss.")]
    public bool UseStopLoss { get; set; } = false;

    // Number of consecutive higher timeframes that must show the same band break before the signal
    // fires. 0 = this timeframe only (normal behaviour). Lives here rather than in the global entry
    // conditions because only a band strategy has a band break to confirm.
    [SettingCaption("Band break confirmation on higher timeframes",
        Tooltip = "Number of consecutive higher timeframes that must show the same band break. 0 = this timeframe only. Example: 1 means the next higher timeframe has to break its band as well. Missing indicator data on a higher timeframe counts as no confirmation.")]
    public int BandBreakConfirmationCount { get; set; } = 0;

    public DbrSettings() : base()
    {
        SoundFileLong = "sound-dbr-oversold.wav";
        SoundFileShort = "sound-dbr-overbought.wav";
    }
}
