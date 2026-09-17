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
    // Groupbox header, spelled exactly as the Avalonia view does.
    private const string GroupCandleLimits = "Candle limits";

    // Donchian lookback for the outer bands, computed over the PREVIOUS BandLength candles
    // (Pine: ta.highest(high[1], len) / ta.lowest(low[1], len); default 20).
    [SettingCaption("Band length",
        Tooltip = "Donchian lookback for the outer bands, computed over the PREVIOUS N candles (Pine default 20).")]
    public int BandLength { get; set; } = 20;

    // Outer band multiplier: middle ± halfRange * (OuterMult / 2.5) (Pine default 3.2).
    [SettingCaption("Band multiplier",
        Tooltip = "Outer band multiplier: middle ± halfRange × (mult / 2.5) (Pine default 3.2). Higher = wider bands = fewer signals.")]
    public double OuterMult { get; set; } = 3.2;

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


    // --- Candle limits ---

    // Upper limit on the size of the candle that breaks the band, as a multiple of the average
    // candle size over the previous DbrBandsHelper.CandleAverageLength candles. 0 = no limit.
    // Measured on ten dbr runs over january-august 2026: entries on a candle five times the average
    // won 44,9% against 59,7% for the rest, walked the whole DCA ladder in 73,8% of the cases and
    // cost 138 USDT per run - 23,3% of those losers sat on the stop within two hours, against 1,3%
    // on a normal candle. The ladder runs to 4% and the stop sits beyond that, so a candle that is
    // itself 8 or 12% tall covers the entire ladder in one move.
    [SettingCaption("Maximum candle size (times the average)", Group = GroupCandleLimits,
        Tooltip = "Skip the signal when the candle that breaks the band is more than N times as tall (high minus low) as the average of the previous 20 candles. 0 = no limit.")]
    public double MaxCandleSizeRatio { get; set; } = 0.0;

    // Upper limit on the volume of the candle that breaks the band, as a multiple of the average
    // volume over the same window. 0 = no limit. Four times the average cost 160 USDT per run in
    // the same measurement: such a candle is an outside blow (news, liquidations) rather than a
    // band break that springs back.
    [SettingCaption("Maximum candle volume (times the average)", Group = GroupCandleLimits,
        Tooltip = "Skip the signal when the candle that breaks the band has more than N times the average volume of the previous 20 candles. 0 = no limit.")]
    public double MaxCandleVolumeRatio { get; set; } = 0.0;

    // Window the two limits above measure "normal" over: the candles BEFORE the breaking one.
    // 20 is what the measurement of 15-09-2026 used.
    [SettingCaption("Average over N candles", Group = GroupCandleLimits,
        Tooltip = "The number of candles before the breaking one that the maximum size and volume are measured against. 20 is the measured default.")]
    public int CandleAverageLength { get; set; } = 20;

    // What to do with a signal on a candle that is over MaxCandleSizeRatio: 0 drops it, a value
    // above 0 keeps it but moves the entry that fraction of the candle's own height beyond its
    // close - a long buys lower, a short sells higher. Only does something when the entry is placed
    // as a LIMIT order (Settings.Trading.EntryOrderType), because a market order ignores the price
    // the signal hands over.
    // Measured on runs 808 and 917 (5.729 positions, the entry replayed on 1m candles): on the
    // signals above five times the average, entering at market is -199,9 USDT and dropping them
    // outright is 0, while a limit one whole candle height further out fills 9% of them and makes
    // +113,1. Over the whole run that is 1.790,0 against 1.477,0 for market and 1.676,9 for
    // dropping them, better in both period halves.
    [SettingCaption("Entry retracement on a large candle", Group = GroupCandleLimits,
        Tooltip = "What to do when the candle is over the maximum size: 0 skips the signal, 1.0 keeps it and places the entry one whole candle height beyond the close (a long buys lower, a short sells higher). Needs the entry order type to be Limit.")]
    public double LargeCandleRetracementPart { get; set; } = 0.0;

    public DbrSettings() : base()
    {
        SoundFileLong = "sound-dbr-oversold.wav";
        SoundFileShort = "sound-dbr-overbought.wav";
    }
}
