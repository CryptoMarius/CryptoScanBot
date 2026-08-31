namespace CryptoScanner.Core.Enums;

/// <summary>
/// How the profit lock places the stop-loss once its trigger has been reached.
/// <para>
/// The trigger itself is the same for every method: the price has to reach
/// <c>MoveSlToBreakEvenPercentage</c> in profit, measured from the break-even price. What differs
/// is where the stop goes afterwards, and whether it keeps moving.
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
}
