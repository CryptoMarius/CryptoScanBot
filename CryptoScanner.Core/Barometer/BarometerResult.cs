namespace CryptoScanner.Core.Barometer;

/// <summary>
/// The outcome of a single barometer measurement over the full symbol pool of one quote.
/// <para>
/// The barometer used to return one number: the average percentage change. Every other figure
/// below is derived from exactly the same per-symbol percentages, which were already computed
/// and then thrown away. They are therefore nearly free - the expensive part of a measurement
/// is looking up two candles per symbol, not the arithmetic that follows.
/// </para>
/// <para>
/// One instance is reused for every measurement within a CalculateBarometerInternal() run. That
/// run walks minute by minute through the backlog, which can be hours long after a restart, so
/// allocating a result object (and its percentage buffer) per measurement would create thousands
/// of short-lived objects for nothing. Reset() is called at the start of every measurement.
/// </para>
/// </summary>
public class BarometerResult
{
    /// The per-symbol percentages of the current measurement. Reused across measurements.
    private readonly List<decimal> percentages = [];

    /// <summary>
    /// Average percentage change over all participating symbols. This is the original barometer
    /// value; it is what CryptoBarometerData.PriceBarometer holds and what every existing filter
    /// and stored signal uses, so its meaning must not change.
    /// </summary>
    public decimal Average { get; private set; }

    /// <summary>
    /// Median percentage change. Unlike the average this is immune to a single coin doubling.
    /// A wide gap between Median and Average means a handful of coins carries the whole move.
    /// </summary>
    public decimal Median { get; private set; }

    /// <summary>
    /// Percentage of symbols with a positive change, on a 0..100 scale. This is the market
    /// breadth: it separates "everything rises a little" (broad, healthy) from "three coins
    /// explode and the rest sinks" (narrow), two situations the average cannot tell apart.
    /// </summary>
    public decimal PercentageRising { get; private set; }

    /// <summary>
    /// Spread of the cross-section: the 75th percentile minus the 25th percentile. A low value
    /// means the coins move as one block, which is what a panic looks like; a high value means
    /// they go their own way.
    /// </summary>
    public decimal Spread { get; private set; }

    /// <summary>
    /// Number of symbols that contributed to this measurement. It varies per measurement because
    /// of missing candles and a changing symbol list, and without it an extreme reading cannot be
    /// judged - a barometer resting on 12 coins says something else than one resting on 380.
    /// </summary>
    public int SymbolCount { get; private set; }

    /// <summary>
    /// Number of symbols skipped because their percentage exceeded the outlier threshold. Expected
    /// to stay at zero; a rising count points at a data problem rather than at market movement.
    /// </summary>
    public int OutlierCount { get; set; }


    /// Clear the buffer and all figures, ready for the next measurement.
    public void Reset()
    {
        percentages.Clear();
        Average = 0;
        Median = 0;
        PercentageRising = 0;
        Spread = 0;
        SymbolCount = 0;
        OutlierCount = 0;
    }


    /// Register the percentage change of one symbol.
    public void Add(decimal percentage)
    {
        percentages.Add(percentage);
    }


    /// <summary>
    /// Derive all figures from the collected percentages. Returns false when no symbol took part,
    /// which tells the caller to skip this measurement completely (no candle is created).
    /// </summary>
    public bool Calculate()
    {
        SymbolCount = percentages.Count;
        if (SymbolCount == 0)
            return false;

        decimal sum = 0;
        int rising = 0;
        foreach (decimal percentage in percentages)
        {
            sum += percentage;
            if (percentage > 0)
                rising++;
        }

        Average = decimal.Round(sum / SymbolCount, 8);
        PercentageRising = decimal.Round(100m * rising / SymbolCount, 8);

        // The percentiles need a sorted list. Sorting a few hundred decimals is negligible next to
        // the candle lookups that produced them.
        percentages.Sort();
        Median = decimal.Round(Percentile(50), 8);
        Spread = decimal.Round(Percentile(75) - Percentile(25), 8);
        return true;
    }


    /// <summary>
    /// Percentile over the sorted percentage list, interpolating between the two surrounding
    /// entries. The nearest-rank alternative jumps around too much when few symbols take part.
    /// </summary>
    private decimal Percentile(int percentile)
    {
        if (percentages.Count == 1)
            return percentages[0];

        decimal position = (percentages.Count - 1) * percentile / 100m;
        int lower = (int)Math.Floor(position);
        int upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return percentages[lower];

        decimal fraction = position - lower;
        return percentages[lower] + fraction * (percentages[upper] - percentages[lower]);
    }
}
