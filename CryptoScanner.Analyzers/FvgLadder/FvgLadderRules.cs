using CryptoScanner.Core.Settings;

namespace CryptoScanner.Analyzers.FvgLadder;

/// <summary>A bullish fair value gap as the rules see it: its top and bottom, and whether it is a 4h gap.</summary>
public readonly record struct FvgLadderZone(decimal Top, decimal Bottom, bool Is4h);

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
    /// minimum are dropped, and a 1h gap that overlaps a 4h gap gives way to that 4h gap. With
    /// <paramref name="use4h"/> off only the 1h gaps count (the Grid preset).
    /// </summary>
    public static List<decimal> UsableTops(IEnumerable<FvgLadderZone> zones, decimal minimumPercentage, bool use4h)
    {
        List<FvgLadderZone> wide = [.. zones.Where(z => IsWideEnough(z.Top, z.Bottom, minimumPercentage))];
        List<FvgLadderZone> zones4h = use4h ? [.. wide.Where(z => z.Is4h)] : [];
        List<decimal> tops = [.. zones4h.Select(z => z.Top)];
        foreach (FvgLadderZone zone in wide.Where(z => !z.Is4h))
        {
            bool overlaps = zones4h.Any(z4 => zone.Top >= z4.Bottom && zone.Bottom <= z4.Top);
            if (!overlaps)
                tops.Add(zone.Top);
        }
        return [.. tops.Distinct().OrderByDescending(t => t)];
    }


    /// <summary>
    /// Rules 3.1 / 3.2 / 6.2: where the buy limit of a new position goes. Only the
    /// <paramref name="orderZones"/> nearest gap tops under the price carry orders; of those the
    /// highest one at or under <paramref name="limit"/> (no limit: any) is the one the price reaches
    /// first, and therefore the one the entry goes on. Null when there is none.
    /// </summary>
    public static decimal? EntryTarget(IReadOnlyList<decimal> topsDescending, decimal price, int orderZones, decimal? limit)
    {
        foreach (decimal top in topsDescending.Where(t => t < price).Take(Math.Max(0, orderZones)))
        {
            if (limit == null || top <= limit.Value)
                return top;
        }
        return null;
    }


    /// <summary>
    /// The Grid preset: the highest of the <paramref name="orderZones"/> nearest gap tops under the
    /// price that lies at least <paramref name="spacingPercentage"/> away from the first buy of every
    /// open position. Null when there is none.
    /// </summary>
    public static decimal? GridEntryTarget(IReadOnlyList<decimal> topsDescending, decimal price, int orderZones,
        IReadOnlyList<FvgLadderTrade> openTrades, decimal spacingPercentage)
    {
        foreach (decimal top in topsDescending.Where(t => t < price).Take(Math.Max(0, orderZones)))
        {
            if (FarEnoughFromAll(top, openTrades, spacingPercentage))
                return top;
        }
        return null;
    }


    /// <summary>
    /// The Grid preset: a new position has to be at least this far (either side) from the first buy
    /// of every open position.
    /// </summary>
    public static bool FarEnoughFromAll(decimal price, IReadOnlyList<FvgLadderTrade> openTrades, decimal spacingPercentage)
        => openTrades.All(t => t.FirstBuyPrice > 0 && 100m * Math.Abs(price / t.FirstBuyPrice - 1m) >= spacingPercentage);


    /// <summary>
    /// Rules 6.1 / 6.2: may a new (parallel) position be started, and under which price? With no
    /// open position: yes, anywhere (<paramref name="limit"/> null). Otherwise only once the DCA's of
    /// the lowest open position (lowest last buy) have all been bought, and then at least
    /// <paramref name="stepPercentage"/> under that last buy.
    /// </summary>
    public static bool NewTradeAllowed(IReadOnlyList<FvgLadderTrade> openTrades, decimal stepPercentage, out decimal? limit)
    {
        limit = null;
        if (openTrades.Count == 0)
            return true;

        FvgLadderTrade lowest = openTrades.MinBy(t => t.LastBuyPrice);
        if (!lowest.DcasDone)
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
    /// Rule 5.1 (Grid preset): the take profit lies a percentage above the ENTRY, while the trader
    /// measures it from the break-even price including the fees of the buy and the predicted sell
    /// (TradeTools: entry x (1 + 2 x fee)). This is the percentage from that break-even price that
    /// lands on entry + <paramref name="takeProfitPercentage"/>. The Ladder preset measures from the
    /// break-even price directly and do not need this.
    /// </summary>
    public static decimal TakeProfitFromBreakEven(decimal takeProfitPercentage, decimal feeRatePercentage)
        => 100m * ((1m + takeProfitPercentage / 100m) / (1m + 2m * feeRatePercentage / 100m) - 1m);


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
