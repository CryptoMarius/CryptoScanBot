using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.VwapMr;

/// <summary>
/// VWAP mean reversion: in when the price touches a band around the day VWAP and the candle turns,
/// out when the price is back at the VWAP. Defaults: 2.0 x stdev(close, 20), a reversal candle,
/// a 1.5% stop loss. See VwapMr.md for what the emulator measured with these and other values.
/// </summary>
[Serializable]
public class VwapMrSettings : SettingsSignalStrategyBase
{
    /// <summary>The number of closes the standard deviation is taken over.</summary>
    [SettingCaption("Stdev length (closes)", SubHeader = "Bands",
        Tooltip = "The number of closes the standard deviation of the bands is taken over. The VWAP "
            + "itself always runs from 00:00 UTC.")]
    public int StdevLength { get; set; } = 20;

    /// <summary>Band distance: vwap +/- Mult * stdev(close, StdevLength).</summary>
    [SettingCaption("Multiplier (stdev)",
        Tooltip = "The bands lie this many standard deviations above and below the day VWAP.")]
    public double Mult { get; set; } = 2.0;

    /// <summary>
    /// A long also wants a green candle (close above open), a short a red one. Off fires on the
    /// band touch alone.
    /// </summary>
    [SettingCaption("Require a reversal candle", SeparatorBefore = true, SubHeader = "Entry",
        Tooltip = "A long also wants a green candle (close above open), a short a red one. Off fires "
            + "on the band touch alone.")]
    public bool RequireReversalCandle { get; set; } = true;

    /// <summary>
    /// Stop loss as a percentage from the entry, handed to the trader with the signal. Zero leaves
    /// the position to the stop loss of the trading settings.
    /// </summary>
    [SettingCaption("Stop loss %", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "Stop loss as a percentage from the entry. Zero leaves the position to the stop "
            + "loss of the trading settings.")]
    public decimal StopLossPercentage { get; set; } = 1.5m;

    /// <summary>
    /// Leave when a candle closes at or beyond the day VWAP: at or above it for a long, at or under
    /// it for a short. The take profit of the trading settings keeps working next to it, so a run
    /// that wants to measure the VWAP exit alone sets that wide.
    /// </summary>
    [SettingCaption("Exit at the VWAP",
        Tooltip = "Leave when a candle closes at or beyond the day VWAP. The take profit of the "
            + "trading settings keeps working next to it; set that wide to measure the VWAP exit alone.")]
    public bool ExitAtVwap { get; set; } = true;

    public VwapMrSettings() : base()
    {
    }
}
