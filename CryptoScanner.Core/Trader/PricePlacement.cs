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
/// <para>
/// Changed back on 10-10-2026: a short uses the plain percentage again, <c>anchor * (1 - p)</c>
/// downward and <c>anchor * (1 + p)</c> upward, exactly the way Altrady places the percentages it is
/// handed. The scanner follows Altrady wherever it can: the emulator has to predict what Altrady does
/// live, and the mirrored prices did not exist there (a short's 24% target: Altrady 76,00, the scanner
/// 80,65). The small built-in handicap of a short described above is real live, so the emulator now
/// shows it again. Every DCA, take profit, stop, trail and lock price goes through this one class.
/// </para>
/// <para>
/// Changed on 24-10-2026 for the take profit only: Altrady does NOT place the take profit of a short
/// with the plain percentage. Every short it closed on HyperLiquid between 22 and 24-10-2026 had its
/// 7.5% take profit at <c>offsetPrice / 1.075</c>, its offsetPrice being our TpGridBreakEvenPrice
/// (WLFI: 0.0569127324 / 1.075 = 0.0529420767; ETHFI, LINK, JUP, STX, FIL, S, DOGE, CHIP, PENDLE,
/// AERO and ZEC the same way), and closed with 7.00% net while the scanner still waited for its
/// target 7.5% below the anchor. The stop of those same shorts does use the plain percentage (WLFI:
/// 0.056964 * 1.08 = 0.06152112). So every take profit price goes through <see cref="TakeProfit"/>,
/// which divides for a short, and <see cref="Favorable"/> keeps the plain percentage for the profit
/// lock, whose trigger has not been compared with Altrady.
/// </para>
public static class PricePlacement
{
    /// <summary>
    /// The price of a take profit <paramref name="percentage"/> away from the anchor, placed the way
    /// Altrady places the price_percentage of a take profit: <c>anchor * (1 + p)</c> for a long and
    /// <c>anchor / (1 + p)</c> for a short. For a short the anchor lies p above the take profit, not
    /// the take profit p below the anchor. A level of 100% or more stays a positive price the market
    /// never reaches, which is how such a level is used ("never").
    /// </summary>
    public static decimal TakeProfit(CryptoTradeSide side, decimal anchor, decimal percentage)
    {
        decimal factor = 1m + percentage / 100m;
        if (side == CryptoTradeSide.Long)
            return anchor * factor;
        // A percentage of -100 or less has nothing to divide by; it falls back to the plain form,
        // which is what Favorable returned for it, instead of throwing or going negative.
        if (factor <= 0)
            return anchor * (1m - percentage / 100m);
        return anchor / factor;
    }

    /// <summary>
    /// The price <paramref name="percentage"/> in the PROFITABLE direction from the anchor: up
    /// for a long, down for a short.
    /// </summary>
    public static decimal Favorable(CryptoTradeSide side, decimal anchor, decimal percentage)
    {
        decimal factor = 1m + percentage / 100m;
        if (side == CryptoTradeSide.Long)
            return anchor * factor;
        //return anchor / factor;
        // A short's target of 100% or more lies at or below zero. Such a level is used as "never"
        // (for instance 200% for the part that leaves through a trailing stop), so it goes to a price
        // the market never reaches instead. Without this the order check threw on the negative price
        // and no short was opened at all (runs 2706/2707, 10-10-2026).
        if (percentage >= 100m)
            return anchor / factor;
        return anchor * (1m - percentage / 100m);
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
        //if (factor <= 0)
        //    return anchor * (1m + percentage / 100m);
        //return anchor / factor;
        return anchor * (1m + percentage / 100m);
    }
}
