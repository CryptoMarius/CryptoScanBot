using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Trend;

[Serializable]
public class TrendSettings : SettingsSignalStrategyBase
{
    private const string GroupEntry = "Entry";
    private const string GroupExit = "Exit";

    /// <summary>
    /// Leave as soon as the trend slot this signal entered on stands against the position: bearish
    /// under a long, bullish under a short. Off is the behaviour up to and including run 1036, where
    /// the strategy had no exit of its own at all and the position lived entirely on the global stop
    /// loss and take profit.
    /// <para>
    /// The reason this exists: the entry is trend-following, but the exit was a fixed take profit
    /// percentage, which cuts off exactly the run the entry aims at. A trend-following entry needs
    /// a trend-following exit, otherwise the winners are capped and the losers are not.
    /// </para>
    /// <para>
    /// Written as a STATE ("the trend is against us") and not as an event ("it flipped on this
    /// candle"), so a candle the monitor did not get to see is not a lost exit - see
    /// SignalCreateBase.IsExitSignal.
    /// </para>
    /// </summary>
    [SettingCaption("Exit when the trend reverts", Group = GroupExit,
        Tooltip = "Leave as soon as the trend stands against the position: bearish under a long, "
            + "bullish under a short. Off leaves the position to the global stop loss and take profit.")]
    public bool ExitOnTrendRevert { get; set; } = false;

    /// <summary>
    /// Arm the long on a flip to BEARISH and the short on a flip to BULLISH, instead of the other
    /// way round. Everything after that stays as it is: the long still waits for a ZigZag Low after
    /// the signal and a close back above it, the short still waits for a High and a close under it.
    /// So an inverted long buys the bounce after the trend turned down, rather than the resumption
    /// after it turned up.
    /// <para>
    /// Measured on 15 September 2026 over 40 perpetuals and 1.8 million candles
    /// (TrendFlipPredictiveMeasurement): after a flip to bullish the price rose LESS often than at a
    /// random moment in 29 of 30 measured cells, and after a flip to bearish MORE often in 28 of 30,
    /// on average 1.43 percentage points either way. If that holds up as a position, the strategy was
    /// trading the right signal in the wrong direction. This setting is what makes that testable.
    /// </para>
    /// </summary>
    [SettingCaption("Invert the direction", Group = GroupEntry,
        Tooltip = "Arm the long on a flip to bearish and the short on a flip to bullish. The "
            + "pullback entry after that is unchanged, so an inverted long buys the bounce.")]
    public bool InvertDirection { get; set; } = false;


    public TrendSettings() : base()
    {
        SoundFileLong = "sound-trend-oversold.wav";
        SoundFileShort = "sound-trend-overbought.wav";
    }

}