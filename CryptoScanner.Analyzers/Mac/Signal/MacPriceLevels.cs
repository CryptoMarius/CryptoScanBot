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


    /// <summary>
    /// How far each of the given lines lies BEHIND the reference price, as a percentage of it: under
    /// it for a long, above it for a short. Lines on the other side, on the reference or missing are
    /// left out. Near to far. Measured the way PricePlacement.Adverse places a level (reference *
    /// (1 - p) for a long, reference * (1 + p) for a short), so a level at that percentage lands on
    /// the line.
    /// </summary>
    public static List<decimal> LineDistancesBehind(CryptoTradeSide side, decimal reference, IEnumerable<double?> lines)
    {
        List<decimal> distances = [];
        if (reference <= 0)
            return distances;
        foreach (double? line in lines)
        {
            if (line == null || line.Value <= 0)
                continue;
            decimal price = (decimal)line.Value;
            decimal distance = side == CryptoTradeSide.Long ? reference - price : price - reference;
            if (distance > 0)
                distances.Add(100m * distance / reference);
        }
        distances.Sort();
        return distances;
    }


    /// <summary>
    /// The DCA distances taken from the line distances (near to far): at most <paramref name="count"/>,
    /// each at least <paramref name="minimumGap"/> percent from the entry and from the DCA before it,
    /// so two lines that lie on top of each other do not become two orders on the same price.
    /// </summary>
    public static List<decimal> PickDcaDistances(List<decimal> distances, int count, decimal minimumGap)
    {
        List<decimal> picked = [];
        decimal previous = 0m;
        foreach (decimal distance in distances)
        {
            if (picked.Count >= count)
                break;
            if (distance - previous < minimumGap)
                continue;
            picked.Add(distance);
            previous = distance;
        }
        return picked;
    }


    /// <summary>
    /// Like <see cref="PickDcaDistances"/>, but one line stays free for the stop: as long as no line
    /// lies far enough beyond the deepest DCA, that DCA is given up. With two lines behind the entry
    /// that is one DCA on the first and the stop on the second (measured on run 282 of 17-10-2026:
    /// the DCAs used up every line on 2.042 of 2.450 positions, which then had no stop at all).
    /// </summary>
    public static List<decimal> PickDcaDistancesKeepingStopLine(List<decimal> distances, int count, decimal minimumGap, decimal margin)
    {
        List<decimal> picked = PickDcaDistances(distances, count, minimumGap);
        while (picked.Count > 0 && StopDistanceOnLineBeyond(distances, picked[^1], minimumGap, margin) == null)
            picked.RemoveAt(picked.Count - 1);
        return picked;
    }


    /// <summary>
    /// The stop on the first line beyond <paramref name="deepest"/> (the deepest DCA, 0 without DCAs),
    /// at least <paramref name="minimumGap"/> percent further, plus <paramref name="margin"/> percent
    /// so a wick that just touches the line does not stop the position out. Null when there is no
    /// such line.
    /// </summary>
    public static decimal? StopDistanceOnLineBeyond(List<decimal> distances, decimal deepest, decimal minimumGap, decimal margin)
    {
        foreach (decimal distance in distances)
        {
            if (distance - deepest >= minimumGap)
                return distance + Math.Max(0m, margin);
        }
        return null;
    }
}
