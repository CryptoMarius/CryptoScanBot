using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.ImpulseBox;

/// <summary>
/// Impulse box: a strong move, then a box of bodies, then a trade against the impulse when the box
/// breaks the other way (counter) or when a break with the impulse fails (fake break). The exit is
/// the one of the trading settings. Defaults are the reference setting of the dry run; see
/// ImpulseBox.md.
/// </summary>
[Serializable]
public class ImpulseBoxSettings : SettingsSignalStrategyBase
{
    /// <summary>The impulse must be at least this many ATR(14) high.</summary>
    [SettingCaption("Impulse size (x ATR)", SubHeader = "Impulse",
        Tooltip = "The move from the start of the impulse to its last close must be at least this many "
            + "times the ATR(14).")]
    public double ImpulseAtrMultiplier { get; set; } = 3.0;

    /// <summary>The impulse ends at the highest (lowest) close of this many candles.</summary>
    [SettingCaption("Impulse window (candles)",
        Tooltip = "The impulse ends at the highest close (lowest for a move down) of this many candles and "
            + "starts at the lowest body (highest) inside them.")]
    public int ImpulseMaxCandles { get; set; } = 16;

    /// <summary>The closes inside the impulse may fall back at most this percentage of its height.</summary>
    [SettingCaption("Max pullback in the impulse %",
        Tooltip = "Inside the impulse no close may fall back more than this percentage of the impulse "
            + "height from the best close so far.")]
    public double ImpulseMaxPullbackPercentage { get; set; } = 38.2;

    /// <summary>The number of candles after the impulse that make the box.</summary>
    [SettingCaption("Box length (candles)", SeparatorBefore = true, SubHeader = "Box",
        Tooltip = "The box is valid after this many candles after the impulse. It is built from the "
            + "bodies (open and close); wicks do not count.")]
    public int BoxCandles { get; set; } = 24;

    /// <summary>The box may be at most this percentage of the impulse height.</summary>
    [SettingCaption("Max box height %",
        Tooltip = "A box taller than this percentage of the impulse height is no balance and is dropped.")]
    public double BoxMaxHeightPercentage { get; set; } = 50;

    /// <summary>A valid box that does not break within this many candles is dropped.</summary>
    [SettingCaption("Box expiry (candles)",
        Tooltip = "A valid box that has not been broken by a close within this many candles is dropped.")]
    public int BoxExpiryCandles { get; set; } = 192;

    /// <summary>Trade the first close outside the box against the impulse.</summary>
    [SettingCaption("Counter trade", SeparatorBefore = true, SubHeader = "Trades",
        Tooltip = "The first close outside the box goes against the impulse: short after an impulse up, "
            + "long after an impulse down.")]
    public bool TradeCounter { get; set; } = true;

    /// <summary>Trade a break with the impulse that closes back inside the box.</summary>
    [SettingCaption("Fake break",
        Tooltip = "The first close outside the box goes with the impulse, but a candle closes back inside "
            + "the box within the window below: trade against the impulse.")]
    public bool TradeFakeBreak { get; set; } = true;

    /// <summary>The number of candles after the break in which a close back inside counts.</summary>
    [SettingCaption("Fake break window (candles)", Indented = true, EnabledWhen = nameof(TradeFakeBreak),
        Tooltip = "How many candles after a break with the impulse a close back inside the box still "
            + "counts as a fake break.")]
    public int FakeBreakWindowCandles { get; set; } = 8;

    /// <summary>Follow the first close outside the box with the impulse.</summary>
    [SettingCaption("Continuation",
        Tooltip = "Follow the first close outside the box in the direction of the impulse. Off by default: "
            + "it measured worse than a blind entry.")]
    public bool TradeContinuation { get; set; } = false;

    public ImpulseBoxSettings() : base()
    {
        // Measured on the 15 minute candle
        IntervalList.Add("15m");
    }
}
