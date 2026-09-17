namespace CryptoScanner.Core.Enums;

/// <summary>
/// Which signal wins when several of them are ready on the same coin in the same minute.
/// <para>
/// Only ONE position can be open per coin - PositionTools.HasPosition looks in the position list by
/// coin name, without the side - so the first signal that gets through opens the position and the
/// rest of that minute is never looked at. Until 16-09-2026 the order was an accident of the loop:
/// it walked the interval list in enum order (1m, 2m, 3m, 5m, ...), so the SHORTEST interval whose
/// candle had just closed always went first. Nothing about that is a decision, which is why it is one
/// now.
/// </para>
/// <para>
/// What the choice is NOT about: which signal survives inside one interval. SignalCreate calls
/// ClearSignalsUpTo(interval.Duration) before it adds a signal, which empties the signal list of that
/// interval AND of every lower one ("higher timeframe overrules lower"). So an interval holds at most
/// one signal, the newest, and several signals only sit side by side when the one on the HIGHER
/// interval arrived first. Measured on run 561 (20.016 signals, 2119 positions, 5m/15m/30m/1h): at
/// 58,6% of the entries another interval still held a living signal and at 45,5% that alternative was
/// on a higher interval - so this order decides about half of all entries. It shows in the outcome as
/// well: 5m produced 41% of the signals and 72% of the positions.
/// </para>
/// <para>
/// The choice belongs in an emulator run, not in an opinion: each value replays the same period and
/// the same signals in a different order, so the queue can put them side by side
/// ("TradingOverrides": {"SignalPriority": 1}).
/// </para>
/// </summary>
public enum CryptoSignalPriority
{
    /// <summary>
    /// The shortest interval first, and inside one interval the order the signals were added. This
    /// is what the loop did before this setting existed, so it changes nothing.
    /// </summary>
    ShortestIntervalFirst = 0,

    /// <summary>
    /// The longest interval first. The reasoning to test: a signal on a slower interval rests on more
    /// candles, so it should outrank the 1m signal that happens to fire in the same minute.
    /// </summary>
    LongestIntervalFirst = 1,

    /// <summary>
    /// The signal whose price sits closest to the last price, whatever interval it came from. The
    /// reasoning to test: the entry that is nearest is the one the market can still reach, so it is
    /// the least likely to be an order that never fills (and gets cancelled after EntryRemoveTime).
    /// </summary>
    NearestEntryPrice = 2,
}
