namespace CryptoScanner.Core.Enums;

/// <summary>
/// How the profit lock places the stop-loss once its trigger has been reached.
/// <para>
/// The trigger itself is the same for every method: the price has to reach
/// <c>MoveSlToBreakEvenPercentage</c> in profit, measured from the break-even price. What differs
/// is where the stop goes afterwards, and whether it keeps moving.
/// </para>
/// <para>
/// Except for the last two, which mirror Altrady's own stop protections: those are armed by a
/// filled take profit and ignore the trigger percentage.
/// </para>
/// </summary>
public enum CryptoProfitLockMethod
{
    /// <summary>
    /// One fixed level: the stop goes to break-even plus <c>MoveSlToBreakEvenSlPercentage</c>
    /// (minus, for a short) and stays there for the rest of the position.
    /// </summary>
    Fixed,

    /// <summary>
    /// The stop follows the price at a fixed distance: <c>MoveSlToBreakEvenTrailPercentage</c>
    /// below the highest price reached (above the lowest, for a short). It only ever moves towards
    /// the take profit - a pullback leaves it where it is.
    /// </summary>
    TrailingPercentage,

    /// <summary>
    /// The stop follows the Keltner channel and the parabolic SAR of the position interval, the
    /// method of the old trader (2021-2023, see git dd7caa72b): on every close of a position-interval
    /// candle the stop goes to the lower of the lower Keltner band and the SAR (the higher of the
    /// upper band and the SAR, for a short); when the whole candle sits above the upper band it goes
    /// up to the upper band instead. Never below the <see cref="Fixed"/> level
    /// (<c>MoveSlToBreakEvenSlPercentage</c>) and, like the percentage trail, only ever towards the
    /// take profit (open point 48).
    /// </summary>
    TrailingKeltnerPsar,

    /// <summary>
    /// Altrady's "TSL: Average Entry Price" (webhook protection_type BREAK_EVEN): once the first
    /// take profit has filled, the stop goes to the average entry price and stays there. Not armed by
    /// <c>MoveSlToBreakEvenPercentage</c> but by that fill, so it needs at least two take profit
    /// levels - with one level the position is closed by the time the stop would move.
    /// </summary>
    BreakEvenAfterFirstTakeProfit,

    /// <summary>
    /// Altrady's "TSL: Follow Take Profit" (webhook protection_type FOLLOW_TAKE_PROFIT): once the
    /// first take profit has filled the stop goes to the average entry price, and every following
    /// fill moves it up to the price of the take profit before it (TP2 filled: stop at TP1, TP3
    /// filled: stop at TP2, ...). Needs at least two take profit levels, like
    /// <see cref="BreakEvenAfterFirstTakeProfit"/>.
    /// </summary>
    FollowTakeProfit,
}
