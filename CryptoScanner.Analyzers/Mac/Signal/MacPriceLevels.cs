using CryptoScanner.Core.Enums;

namespace CryptoScanner.Analyzers.Mac.Signal;

/// <summary>
/// The arithmetic behind the MAC limit entry and the stop behind the level, kept apart from the
/// signal so it can be tested without candles.
/// </summary>
public static class MacPriceLevels
{
    /// <summary>
    /// The nearest of two levels that lies BEHIND the reference price: for a long the highest one
    /// under it, for a short the lowest one above it. Null when neither does. A level exactly on the
    /// reference is not behind it, unless <paramref name="inclusive"/> is set.
    /// </summary>
    public static decimal? NearestLevelBehind(CryptoTradeSide side, decimal reference, double? first, double? second,
        bool inclusive = false)
    {
        decimal? best = null;
        foreach (double? candidate in new[] { first, second })
        {
            if (candidate == null || candidate.Value <= 0)
                continue;
            decimal level = (decimal)candidate.Value;
            if (side == CryptoTradeSide.Long)
            {
                if ((level < reference || (inclusive && level == reference)) && (best == null || level > best.Value))
                    best = level;
            }
            else
            {
                if ((level > reference || (inclusive && level == reference)) && (best == null || level < best.Value))
                    best = level;
            }
        }
        return best;
    }


    /// <summary>
    /// The stop as a percentage from the entry when it lies <paramref name="margin"/> (a price
    /// distance) beyond the level: under it for a long, above it for a short. Measured the way the
    /// trader places a signal stop (PricePlacement.Adverse: entry * (1 - p) for a long,
    /// entry * (1 + p) for a short), so the stop lands exactly there.
    /// </summary>
    public static decimal StopPercentageBehindLevel(CryptoTradeSide side, decimal entry, decimal level, decimal margin)
    {
        if (entry <= 0)
            return 0m;
        decimal stopPrice = side == CryptoTradeSide.Long ? level - margin : level + margin;
        decimal distance = side == CryptoTradeSide.Long ? entry - stopPrice : stopPrice - entry;
        return Math.Max(0m, 100m * distance / entry);
    }
}
