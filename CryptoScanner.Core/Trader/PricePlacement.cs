using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Where a price lands when it is placed a percentage away from an anchor: the take profit, the
/// stop loss and its limit, the DCA levels, the profit-lock trigger and the trailing stop all do
/// this, and they all have to do it the same way.
/// <para>
/// A long is written as it always was: <c>anchor * (1 + p)</c> upward, <c>anchor * (1 - p)</c>
/// downward. A short is its mirror in LOG space and not in arithmetic space: <c>anchor / (1 + p)</c>
/// downward and <c>anchor / (1 - p)</c> upward. The arithmetic mirror (<c>anchor * (1 - p)</c> for a
/// short's target) put the target of a short 2,3% further away and its stop 4,6% closer than the
/// long's, measured in log percent over 85.468 positions - a built-in handicap of 1,16 to 1,20
/// percentage points in win rate for every strategy, whatever it did (open point 27, measured
/// 2026-08-21, changed 2026-09-26). Since then a long and a short with the same settings need the
/// same move, in either direction, to reach the same level.
/// </para>
/// <para>
/// Consequence for anyone comparing runs: every short result from before 2026-09-26 was made with
/// the arithmetic mirror. And Altrady is handed percentages, not prices, and places them its own
/// (arithmetic) way, so a delegated short sits a fraction off from the paper administration.
/// </para>
/// </summary>
public static class PricePlacement
{
    /// <summary>
    /// The price <paramref name="percentage"/> in the PROFITABLE direction from the anchor: up
    /// for a long, down for a short.
    /// </summary>
    public static decimal Favorable(CryptoTradeSide side, decimal anchor, decimal percentage)
    {
        decimal factor = 1m + percentage / 100m;
        if (side == CryptoTradeSide.Long)
            return anchor * factor;
        return anchor / factor;
    }

    /// <summary>
    /// The price <paramref name="percentage"/> in the ADVERSE direction from the anchor: down for
    /// a long, up for a short. A percentage of 100 or more has no mirror (the long would be at or
    /// below zero); the short then falls back to the arithmetic form rather than divide by zero.
    /// </summary>
    public static decimal Adverse(CryptoTradeSide side, decimal anchor, decimal percentage)
    {
        decimal factor = 1m - percentage / 100m;
        if (side == CryptoTradeSide.Long)
            return anchor * factor;
        if (factor <= 0)
            return anchor * (1m + percentage / 100m);
        return anchor / factor;
    }
}
