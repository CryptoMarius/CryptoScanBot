using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.ChochFvg;

/// <summary>
/// Change of character with a fair value gap: after the first close beyond the last swing against
/// the trend, a limit on the middle of a fair value gap of that leg inside its fib zone, a stop
/// behind the candle that made the gap and a target at a fixed risk/reward. See ChochFvg.md.
/// </summary>
[Serializable]
public class ChochFvgSettings : SettingsSignalStrategyBase
{
    /// <summary>A swing high (low) needs this many lower highs (higher lows) on each side.</summary>
    [SettingCaption("Swing strength (candles)", SubHeader = "Structure",
        Tooltip = "A swing high needs this many candles with a lower high on each side, a swing low this many "
            + "with a higher low.")]
    public int SwingStrength { get; set; } = 3;

    /// <summary>The window in which the low (high) of the move before the change of character lies.</summary>
    [SettingCaption("Lookback (candles)",
        Tooltip = "The lowest low (highest high for a short) of this many candles is the start of the leg "
            + "that makes the change of character.")]
    public int LookbackCandles { get; set; } = 60;

    /// <summary>The start of the entry zone as a retracement of the leg (0..100).</summary>
    [SettingCaption("Fib zone from %", SeparatorBefore = true, SubHeader = "Entry",
        Tooltip = "The middle of the fair value gap has to lie between these two retracements of the leg, "
            + "measured from its end back to its start.")]
    public decimal FibZoneStart { get; set; } = 50m;

    /// <summary>The end of the entry zone as a retracement of the leg (0..100).</summary>
    [SettingCaption("Fib zone to %",
        Tooltip = "The middle of the fair value gap has to lie between these two retracements of the leg, "
            + "measured from its end back to its start.")]
    public decimal FibZoneEnd { get; set; } = 61.8m;

    /// <summary>The limit order is withdrawn when it has not filled within this many candles.</summary>
    [SettingCaption("Entry expiry (candles)",
        Tooltip = "The limit on the middle of the gap is withdrawn when it has not filled within this many "
            + "candles, or as soon as the price reaches the target without it.")]
    public int EntryExpiryCandles { get; set; } = 60;

    /// <summary>The take profit as a multiple of the distance from the entry to the stop.</summary>
    [SettingCaption("Risk/reward", SeparatorBefore = true, SubHeader = "Exit",
        Tooltip = "The take profit lies this many times the distance from the entry to the stop beyond the "
            + "entry. The stop lies behind the candle that made the gap. No DCA.")]
    public decimal RiskReward { get; set; } = 4m;

    /// <summary>A setup with a stop closer to the entry than this percentage (0..100) is dropped.</summary>
    [SettingCaption("Minimum stop %",
        Tooltip = "A setup whose stop lies closer to the entry than this percentage is dropped: on such a "
            + "small stop the fees eat most of the risk. 0 takes every setup.")]
    public decimal MinimumStopPercentage { get; set; } = 0.2m;

    /// <summary>Only take a change of character inside the session window below.</summary>
    [SettingCaption("Only in the session window", SeparatorBefore = true, SubHeader = "Session",
        Tooltip = "Only a change of character on a candle that closes inside this window (New York time) "
            + "places an order.")]
    public bool UseSessionWindow { get; set; } = true;

    /// <summary>The start of the session window, New York time (HH:mm).</summary>
    [SettingCaption("Window from (New York time)", Indented = true, EnabledWhen = nameof(UseSessionWindow), Trim = true,
        Tooltip = "The start of the window as HH:mm in New York time; the stock market opens at 09:30.")]
    public string SessionStart { get; set; } = "09:30";

    /// <summary>The end of the session window, New York time (HH:mm).</summary>
    [SettingCaption("Window to (New York time)", Indented = true, EnabledWhen = nameof(UseSessionWindow), Trim = true,
        Tooltip = "The end of the window as HH:mm in New York time.")]
    public string SessionEnd { get; set; } = "11:30";

    public ChochFvgSettings() : base()
    {
        // Built for the 1 minute candle
        IntervalList.Add("1m");
    }
}
