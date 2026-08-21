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

    /// <summary>
    /// Average of the percentages ignoring their sign: how far the typical coin moved, regardless of
    /// direction. The figures above all describe WHERE the market went; this one says how hard it is
    /// moving at all. A market where every coin does 0.1% and one where every coin does 3% can
    /// produce the same average, the same median and the same breadth.
    /// <para>It costs nothing: the loop in Calculate() already walks the percentages once.</para>
    /// </summary>
    public decimal AverageAbsolute { get; private set; }

    /// <summary>
    /// The percentage of bitcoin itself, if it took part in this measurement. Set by the caller,
    /// which is the only place that knows which symbol bitcoin is on this exchange.
    /// </summary>
    public decimal? BitcoinPercentage { get; private set; }

    /// <summary>
    /// Bitcoin minus the median coin. Positive means bitcoin is outperforming the rest, which is
    /// money moving towards safety; negative means the smaller coins are being bought, which is
    /// appetite for risk. It says nothing about the direction of the market as a whole - both can
    /// happen while everything rises or while everything falls.
    /// <para>
    /// Measured against the median and not the average on purpose: three altcoins doubling would
    /// drag the average down and fake a rotation that never happened.
    /// </para>
    /// </summary>
    public decimal? BitcoinVersusMarket { get; private set; }


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
        AverageAbsolute = 0;
        BitcoinPercentage = null;
        BitcoinVersusMarket = null;
    }


    /// Register the percentage change of one symbol.
    public void Add(decimal percentage)
    {
        percentages.Add(percentage);
    }


    /// <summary>
    /// Register that this percentage belongs to bitcoin. It is also added through Add() like every
    /// other coin - bitcoin is part of the market, so it counts towards the average and the breadth
    /// as well. This only remembers it separately.
    /// </summary>
    public void SetBitcoin(decimal percentage)
    {
        BitcoinPercentage = percentage;
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

        // One pass over the percentages produces the sum, the count of risers and the sum of the
        // absolute values. Adding the third costs one Math.Abs per coin inside a loop that was
        // already running - next to the two candle lookups per coin that produced these numbers it
        // does not register.
        decimal sum = 0;
        decimal sumAbsolute = 0;
        int rising = 0;
        foreach (decimal percentage in percentages)
        {
            sum += percentage;
            sumAbsolute += Math.Abs(percentage);
            if (percentage > 0)
                rising++;
        }

        Average = decimal.Round(sum / SymbolCount, 8);
        AverageAbsolute = decimal.Round(sumAbsolute / SymbolCount, 8);
        PercentageRising = decimal.Round(100m * rising / SymbolCount, 8);

        // The percentiles need a sorted list. Sorting a few hundred decimals is negligible next to
        // the candle lookups that produced them.
        percentages.Sort();
        Median = decimal.Round(Percentile(50), 8);
        Spread = decimal.Round(Percentile(75) - Percentile(25), 8);

        // Only meaningful once the median is known. Stays null when bitcoin did not take part, which
        // happens on a quote that has no bitcoin pair.
        if (BitcoinPercentage.HasValue)
            BitcoinVersusMarket = decimal.Round(BitcoinPercentage.Value - Median, 8);
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
