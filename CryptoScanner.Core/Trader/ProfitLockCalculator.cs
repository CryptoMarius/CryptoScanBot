using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Pure, stateless helpers for the profit lock, extracted from <see cref="PositionMonitor"/> for
/// the same reason as <see cref="StopLossCalculator"/>: every branch is then testable without a
/// position, a database or GlobalData.
///
/// The profit lock has two halves and they are deliberately separate:
///   1. the TRIGGER - how far in profit the price has to get before the lock arms itself, and
///   2. the PLACEMENT - where the stop-loss goes once it is armed.
/// Keeping the placement closer to break-even than the trigger leaves room between the price and
/// the stop, so the position is not stopped out by the very move that armed the lock.
/// </summary>
public static class ProfitLockCalculator
{
    /// <summary>+1 for a long, -1 for a short. The whole file is written long-first and mirrored.</summary>
    public static int Multiplier(CryptoTradeSide side) => side == CryptoTradeSide.Long ? +1 : -1;

    /// <summary>
    /// The price at which the lock arms: break-even plus the trigger percentage (minus, for a short).
    /// </summary>
    public static decimal TriggerPrice(CryptoTradeSide side, decimal breakEvenPrice, decimal triggerPercentage)
        => breakEvenPrice + Multiplier(side) * breakEvenPrice * triggerPercentage / 100m;

    /// <summary>
    /// How far the position is in profit right now, in percent from break-even. Negative when the
    /// price is on the wrong side of break-even.
    /// </summary>
    public static decimal ProfitPercentage(CryptoTradeSide side, decimal breakEvenPrice, decimal favorablePrice)
    {
        if (breakEvenPrice <= 0)
            return 0m;
        return Multiplier(side) * (favorablePrice - breakEvenPrice) / breakEvenPrice * 100m;
    }

    /// <summary>
    /// Where the stop goes for <see cref="CryptoProfitLockMethod.Fixed"/>: break-even plus the SL
    /// percentage. The SL percentage is capped to the trigger, because a stop beyond the level that
    /// just armed the lock sits at or through the current price and would fill on the spot.
    /// </summary>
    public static decimal FixedStop(CryptoTradeSide side, decimal breakEvenPrice,
        decimal triggerPercentage, decimal slPercentage)
    {
        decimal pct = Math.Min(slPercentage, triggerPercentage);
        return breakEvenPrice + Multiplier(side) * breakEvenPrice * pct / 100m;
    }

    /// <summary>
    /// Where the stop goes for <see cref="CryptoProfitLockMethod.TrailingPercentage"/>: the trail
    /// percentage below the best price the position has seen (above it, for a short).
    /// <para>
    /// <paramref name="currentTrailingStop"/> is the level the position already trails at; pass 0
    /// when the lock has just armed. The result never moves back towards the entry - a pullback
    /// leaves the stop where it was, which is the whole point of a trailing stop.
    /// </para>
    /// </summary>
    public static decimal TrailingStop(CryptoTradeSide side, decimal favorablePrice,
        decimal trailPercentage, decimal currentTrailingStop)
    {
        int multiplier = Multiplier(side);
        decimal candidate = favorablePrice - multiplier * favorablePrice * trailPercentage / 100m;

        if (currentTrailingStop <= 0)
            return candidate;

        // Ratchet: only ever towards the take profit (long: higher; short: lower).
        return multiplier == 1
            ? Math.Max(candidate, currentTrailingStop)
            : Math.Min(candidate, currentTrailingStop);
    }

    /// <summary>
    /// Whether the profit-lock level actually replaces the stop that is already there. Tighten only:
    /// it wins when there was no stop at all, or when it sits closer to the price than the current
    /// one (long: higher is tighter; short: lower is tighter). A trailing stop that could ever
    /// LOOSEN the existing stop-loss would widen the risk on a position that just went into profit,
    /// which is the opposite of what the lock is for.
    /// </summary>
    public static bool Tightens(CryptoTradeSide side, decimal lockStop, decimal? currentStop)
    {
        if (currentStop == null)
            return true;
        return Multiplier(side) == 1 ? lockStop > currentStop.Value : lockStop < currentStop.Value;
    }

    /// <summary>
    /// The worst acceptable fill for the profit-lock stop: one percent of the price further away
    /// than the trigger. In paper trading the fill happens at the stop price so this never bites,
    /// but a real exchange can fill anywhere between the two.
    /// </summary>
    public static decimal StopLimit(CryptoTradeSide side, decimal lockStop)
    {
        decimal gap = Math.Abs(lockStop * 0.01m);
        return lockStop - Multiplier(side) * gap;
    }

    /// <summary>
    /// The inverse of <see cref="TrailingStop"/>: the price the market has to reach before the
    /// trailing stop moves again. Used for the trigger-price fence, so HandlePosition wakes up on
    /// the candle that makes a new extreme instead of only on a take-profit or stop touch.
    /// </summary>
    public static decimal PriceThatMovesTrailingStop(CryptoTradeSide side, decimal trailingStop,
        decimal trailPercentage)
    {
        decimal factor = 1m - Multiplier(side) * trailPercentage / 100m;
        if (factor <= 0)
            return trailingStop;
        return trailingStop / factor;
    }
}
