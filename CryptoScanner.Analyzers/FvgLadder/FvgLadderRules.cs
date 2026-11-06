using CryptoScanner.Core.Settings;

namespace CryptoScanner.Analyzers.FvgLadder;

/// <summary>
/// A bullish fair value gap as the rules see it: its top and bottom, whether it is a 4h gap, and
/// whether the last closed candle of the gap's own interval (1h or 4h) closed above its top.
/// </summary>
public readonly record struct FvgLadderZone(decimal Top, decimal Bottom, bool Is4h, bool ClosedAbove = true);

/// <summary>
/// What the rules need to know about one open (filled) position of the strategy: the price of its
/// first buy, the price of its most recent buy, and whether all of its DCA's have been bought.
/// </summary>
public readonly record struct FvgLadderTrade(decimal FirstBuyPrice, decimal LastBuyPrice, bool DcasDone);

/// <summary>One DCA level: the price of the buy limit and its size as a factor of the entry (100 = the entry again).</summary>
public readonly record struct FvgLadderLevel(decimal Price, decimal Factor);


/// <summary>
/// The rules of "BTC FVG DCA - Regels ter afstemming" as plain calculations, without candles, zones
/// or positions, so each one can be tested on its own. The signal (FvgLadderLong) collects the inputs.
/// All percentages are on the 0..100 scale.
/// </summary>
public static class FvgLadderRules
{
    /// <summary>
    /// Rule 2.3: the gap has to be at least this wide, measured as (top - bottom) / bottom, the same
    /// way ZoneFvg measures it. The minimum itself counts.
    /// </summary>
    public static bool IsWideEnough(decimal top, decimal bottom, decimal minimumPercentage)
        => bottom > 0 && top > bottom && 100m * (top - bottom) / bottom >= minimumPercentage;


    /// <summary>
    /// Rules 2.1 - 2.5: the gaps that count, as their tops from high to low. Gaps narrower than the
    /// minimum are dropped, and a 1h gap that overlaps a 4h gap gives way to that 4h gap.
    /// </summary>
    public static List<decimal> UsableTops(IEnumerable<FvgLadderZone> zones, decimal minimumPercentage)
        => [.. UsableZones(zones, minimumPercentage).Select(z => z.Top).Distinct().OrderByDescending(t => t)];


    /// <summary>
    /// Rule 3, since 03-11-2026: the usable gap tops (see <see cref="UsableTops"/>) an entry may go on: only the gaps
    /// whose own interval closed a candle above the top. A price that comes up from under a gap and
    /// crosses it within a few minute candles has not broken it yet; the entry waits for the close of
    /// the 1h or 4h candle above it. The DCA ladder keeps using all usable tops.
    /// </summary>
    public static List<decimal> EntryTops(IEnumerable<FvgLadderZone> zones, decimal minimumPercentage)
        => [.. UsableZones(zones, minimumPercentage).Where(z => z.ClosedAbove).Select(z => z.Top).Distinct().OrderByDescending(t => t)];


    /// <summary>Rules 2.1 - 2.5 as zones: wide enough, and a 1h gap overlapping a 4h gap dropped.</summary>
    private static List<FvgLadderZone> UsableZones(IEnumerable<FvgLadderZone> zones, decimal minimumPercentage)
    {
        List<FvgLadderZone> wide = [.. zones.Where(z => IsWideEnough(z.Top, z.Bottom, minimumPercentage))];
        List<FvgLadderZone> zones4h = [.. wide.Where(z => z.Is4h)];
        List<FvgLadderZone> usable = [.. zones4h];
        foreach (FvgLadderZone zone in wide.Where(z => !z.Is4h))
        {
            bool overlaps = zones4h.Any(z4 => zone.Top >= z4.Bottom && zone.Bottom <= z4.Top);
            if (!overlaps)
                usable.Add(zone);
        }
        return usable;
    }


    /// <summary>
    /// Rules 3.1 / 3.2 / 6.2: where the buy limit of a new position goes. Only the
    /// <paramref name="gapsInView"/> nearest gap tops under the price take part; of those the highest
    /// one at or under <paramref name="limit"/> (no limit: any) is the one the price reaches first,
    /// and therefore the one the entry goes on. Null when there is none.
    /// </summary>
    public static decimal? EntryTarget(IReadOnlyList<decimal> topsDescending, decimal price, int gapsInView, decimal? limit)
    {
        foreach (decimal top in topsDescending.Where(t => t < price).Take(Math.Max(0, gapsInView)))
        {
            if (limit == null || top <= limit.Value)
                return top;
        }
        return null;
    }


    /// <summary>
    /// Rules 6.1 / 6.2: may a new position be started next to the open ones, and under which price?
    /// <list type="bullet">
    /// <item>Not when <paramref name="maxPositions"/> positions are open already (0 = no limit).</item>
    /// <item>With no open position: yes, anywhere (<paramref name="limit"/> null).</item>
    /// <item>Otherwise only under the lowest open position (lowest last buy), at least
    /// <paramref name="stepPercentage"/> under its last buy - never above it or in between - and
    /// with DCA only once that position has bought all of its DCA's.</item>
    /// </list>
    /// </summary>
    public static bool NewTradeAllowed(IReadOnlyList<FvgLadderTrade> openTrades, int maxPositions, bool useDca,
        decimal stepPercentage, out decimal? limit)
    {
        limit = null;
        if (maxPositions > 0 && openTrades.Count >= maxPositions)
            return false;
        if (openTrades.Count == 0)
            return true;

        FvgLadderTrade lowest = openTrades.MinBy(t => t.LastBuyPrice);
        if (useDca && !lowest.DcasDone)
            return false;

        limit = lowest.LastBuyPrice * (1m - stepPercentage / 100m);
        return true;
    }


    /// <summary>
    /// Rules 4.1 - 4.4: the DCA ladder of a position entered at <paramref name="entryPrice"/>. Every
    /// level lies at least <paramref name="stepPercentage"/> under the buy before it; when a gap top
    /// lies between that step and <paramref name="fvgMaxPercentage"/> under it, the level goes on the
    /// highest such top instead. The size grows by <paramref name="factor"/> per level
    /// (factor 2: 200, 400, ... percent of the entry).
    /// <para>
    /// The ladder is fixed when the entry is placed: a gap that only forms later is not used.
    /// </para>
    /// </summary>
    public static List<FvgLadderLevel> DcaLevels(decimal entryPrice, IReadOnlyList<decimal> tops, int maxDca,
        decimal stepPercentage, decimal fvgMaxPercentage, decimal factor)
    {
        List<FvgLadderLevel> levels = [];
        decimal last = entryPrice;
        decimal size = 100m;
        for (int i = 0; i < maxDca; i++)
        {
            decimal high = last * (1m - stepPercentage / 100m);
            decimal low = last * (1m - fvgMaxPercentage / 100m);
            // The highest top inside the band, the step itself when there is none
            decimal? snap = tops.Where(t => t >= low && t <= high).Select(t => (decimal?)t).Max();
            decimal level = snap ?? high;

            size *= factor;
            levels.Add(new FvgLadderLevel(level, size));
            last = level;
        }
        return levels;
    }


    /// <summary>
    /// The ladder in the form the trader takes it (SignalCreateBase.OverrideDcaList): each level as a
    /// percentage under the entry price, the anchor the trader measures a strategy's own ladder from.
    /// </summary>
    public static List<CryptoDcaEntry> ToDcaList(decimal entryPrice, IReadOnlyList<FvgLadderLevel> levels)
        => [.. levels.Select(l => new CryptoDcaEntry
        {
            Percentage = Math.Round(100m * (1m - l.Price / entryPrice), 6),
            Factor = l.Factor,
        })];


    /// <summary>
    /// The EMA of the closes with a weight of 2 / (length + 1), started on the first close.
    /// </summary>
    public static decimal Ema(IEnumerable<decimal> closes, int length)
    {
        decimal alpha = 2m / (length + 1);
        decimal? ema = null;
        foreach (decimal close in closes)
            ema = ema == null ? close : alpha * close + (1m - alpha) * ema.Value;
        return ema ?? 0m;
    }


    /// <summary>One step of the EMA above.</summary>
    public static decimal NextEma(decimal previousEma, decimal close, int length)
    {
        decimal alpha = 2m / (length + 1);
        return alpha * close + (1m - alpha) * previousEma;
    }
}
