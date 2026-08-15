using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// Which figure of a barometer measurement the graph draws.
/// </summary>
public enum BarometerGraphValue
{
    Average,
    Median,
    Rising,
    Spread,
    SymbolCount,
}

/// <summary>
/// How a barometer measurement is stored in the price fields of a barometer candle, and how to read
/// it back out.
/// <para>
/// A barometer is one number, so a candle has four price fields it does not need - all four used to
/// hold the same value. They now carry the separate figures of the same measurement, which gives all
/// of them a history and a graph for free. Note that High and Low are therefore NOT the highest and
/// lowest value; nothing draws these candles as candlesticks, but keep it in mind when reading raw
/// candle output.
/// </para>
/// <para>
/// Storing and reading live here together on purpose: split over two files they would drift apart at
/// the first change, and the failure would be silent - a graph quietly plotting the wrong figure.
/// </para>
/// </summary>
public static class BarometerCandleFields
{
    /// <summary>
    /// Write a completed measurement into the price fields of a barometer candle.
    /// <para>
    /// The ref is essential: CryptoCandle is a STRUCT. Taking it by value would assign to a copy that
    /// is thrown away on return, and the caller would store an all-zero candle - which is exactly
    /// what happened once. The same reason the caller ends with candles[time] = candle.
    /// </para>
    /// </summary>
    public static void Store(ref CryptoCandle candle, BarometerResult result)
    {
        candle.Open = result.Median;
        candle.High = result.PercentageRising;
        candle.Low = result.Spread;
        candle.Close = result.Average;          // the original barometer value, unchanged
        candle.Volume = result.SymbolCount;
    }


    /// <summary>
    /// Whether a candle still holds the layout from before this class existed, when all four price
    /// fields carried the same number. Reading such a candle would show the average as the breadth
    /// and again as the spread, so it has to be recomputed instead of drawn.
    /// <para>
    /// These candles survive a restart: barometer candles are stored in candles.db and reloaded,
    /// while LastCandleSynchronized makes the calculation resume at the end of the previous session.
    /// Everything older than that point keeps whatever layout it was written with.
    /// </para>
    /// <para>
    /// The test cannot mistake a valid candle for an old one: breadth (0..100) equalling the average
    /// (around zero) to the cent, at the same moment as the median and the spread, does not occur in
    /// practice. The all-zero case is the exception, and recomputing that costs nothing.
    /// </para>
    /// </summary>
    public static bool IsLegacyLayout(CryptoCandle candle)
    {
        return candle.Open == candle.Close && candle.High == candle.Close && candle.Low == candle.Close;
    }


    /// Read one figure back out of a barometer candle.
    public static decimal Read(CryptoCandle candle, BarometerGraphValue value)
    {
        return value switch
        {
            BarometerGraphValue.Median => candle.Open,
            BarometerGraphValue.Rising => candle.High,
            BarometerGraphValue.Spread => candle.Low,
            BarometerGraphValue.SymbolCount => candle.Volume,
            _ => candle.Close,
        };
    }


    /// The label of each figure, as shown in the dropdown of both dashboards.
    public static string GetName(BarometerGraphValue value)
    {
        return value switch
        {
            BarometerGraphValue.Median => "Median",
            BarometerGraphValue.Rising => "Rising",
            BarometerGraphValue.Spread => "Spread",
            BarometerGraphValue.SymbolCount => "Coins",
            _ => "Average",
        };
    }


    /// <summary>
    /// The figures the graph can draw, in dropdown order. Average comes first: it is the barometer
    /// as it always was.
    /// <para>
    /// SymbolCount is deliberately missing. It is stored in every candle and shown in the tooltip,
    /// but as a graph it says nothing: in a healthy market it is a flat line by definition, because
    /// the number of participating coins only moves when something is broken. It answers "can I
    /// trust this reading", and that is a question you ask at one moment, not over seven hours.
    /// </para>
    /// </summary>
    private static readonly BarometerGraphValue[] GraphValues =
    [
        BarometerGraphValue.Average,
        BarometerGraphValue.Median,
        BarometerGraphValue.Rising,
        BarometerGraphValue.Spread,
    ];

    /// The labels of the figures the graph can draw, in dropdown order.
    public static IReadOnlyList<string> Names { get; } = [.. GraphValues.Select(GetName)];


    /// Resolve a dropdown label back to its figure, falling back to the original barometer value.
    public static BarometerGraphValue Parse(string name)
    {
        foreach (BarometerGraphValue value in Enum.GetValues<BarometerGraphValue>())
        {
            if (GetName(value) == name)
                return value;
        }
        return BarometerGraphValue.Average;
    }


    /// <summary>
    /// All figures of one measurement, one per line, for the tooltip both dashboards put on a
    /// barometer row. The panels only have room for the average and the breadth; the rest is just as
    /// informative but needed far less often, so it stays a hover away.
    /// </summary>
    public static string Describe(CryptoBarometerData barometer)
    {
        if (barometer.PriceBarometer == null)
            return "";

        List<string> lines =
        [
            $"Average {barometer.PriceBarometer.Value:N2}%",
        ];

        if (barometer.PriceMedian.HasValue)
            lines.Add($"Median {barometer.PriceMedian.Value:N2}%");
        if (barometer.PricePercentageRising.HasValue)
            lines.Add($"Rising {barometer.PricePercentageRising.Value:N1}% of the coins");
        if (barometer.PriceSpread.HasValue)
            lines.Add($"Spread {barometer.PriceSpread.Value:N2}% (75th minus 25th percentile)");
        if (barometer.PriceSymbolCount.HasValue)
            lines.Add($"Based on {barometer.PriceSymbolCount.Value} coins");

        // Only worth a line when it actually happens - it points at a data problem, not at the market.
        if (barometer.PriceOutlierCount > 0)
            lines.Add($"Skipped {barometer.PriceOutlierCount!.Value} outliers");

        return string.Join("\n", lines);
    }


    /// <summary>
    /// How much is subtracted from a figure before the graph draws it, so a figure whose neutral
    /// point is not zero can still use a scale centred on zero. Breadth is neutral at 50 percent:
    /// shifted down by that, "more coins rose than fell" is simply above the line and green, exactly
    /// like a positive average. The stored value is not touched - the panel and the tooltip keep
    /// showing 53 percent, not +3.
    /// <para>Kept separate from GetScale() so a drawing loop can call it per candle without
    /// allocating a scale object every time.</para>
    /// </summary>
    public static decimal GetOffset(BarometerGraphValue value)
    {
        return value == BarometerGraphValue.Rising ? 50m : 0m;
    }


    /// Read one figure and shift it onto the scale the graph draws it on. See GetOffset.
    public static decimal ReadForGraph(CryptoCandle candle, BarometerGraphValue value)
    {
        return Read(candle, value) - GetOffset(value);
    }


    /// <summary>
    /// How the graph should scale one figure vertically. The average, the median and the shifted
    /// breadth all swing around zero and share a symmetric scale that grows with the data; the
    /// spread is never negative and starts at the bottom of the picture.
    /// </summary>
    public static BarometerGraphScale GetScale(BarometerGraphValue value)
    {
        return value switch
        {
            // Breadth after its 50-point shift: same treatment as the average, only wider. It swings
            // tens of points where the average moves in single percents, so zooming in to a span of
            // 5 would make every graph look like a panic.
            BarometerGraphValue.Rising => new BarometerGraphScale
            {
                CenteredOnZero = true,
                MinimumSpan = 20m,
                GridFrom = -30m,
                GridTo = 30m,
                GridEvery = 10m,
                Decimals = 0,
            },
            BarometerGraphValue.Spread => new BarometerGraphScale
            {
                CenteredOnZero = false,
                Low = 0m,
                High = null,            // grows with the data
                ReferenceLine = null,
                GridStep = null,        // a quarter of the range
                Decimals = 2,
            },
            BarometerGraphValue.SymbolCount => new BarometerGraphScale
            {
                CenteredOnZero = false,
                Low = 0m,
                High = null,
                ReferenceLine = null,
                GridStep = null,
                Decimals = 0,
            },
            // Average and median keep the scale the barometer graph always had, untouched.
            _ => new BarometerGraphScale
            {
                CenteredOnZero = true,
                IgnoreBeyond = 50m,     // malfunctions seen on Bybit Futures
                Decimals = 2,
            },
        };
    }
}


/// <summary>
/// The vertical scale of the barometer graph for one figure.
/// <para>
/// CenteredOnZero selects the scale the graph has always used: symmetric around zero, growing with
/// the data but never below MinimumSpan, with the zero line in red and grey lines around it. The
/// defaults of the four fields below it reproduce the original behaviour exactly, so the average and
/// the median only have to say CenteredOnZero = true.
/// </para>
/// <para>
/// The remaining fields apply only when CenteredOnZero is false.
/// </para>
/// </summary>
public sealed class BarometerGraphScale
{
    public bool CenteredOnZero { get; init; }

    // --- centred scale ---

    /// Never zoom in further than this vertical span.
    public decimal MinimumSpan { get; init; } = 5m;

    /// <summary>
    /// Values this far from zero or further are left out of the scale: for a percentage change they
    /// are exchange malfunctions, and one of them would flatten the whole graph. Null when the figure
    /// has no such ceiling - breadth reaches exactly -50 and +50 at its extremes, which are real
    /// readings (no coin rose, or every coin did) and must not be discarded.
    /// </summary>
    public decimal? IgnoreBeyond { get; init; }

    /// Grey grid lines from GridFrom up to GridTo, one every GridEvery.
    public decimal GridFrom { get; init; } = -3m;
    public decimal GridTo { get; init; } = 3m;
    public decimal GridEvery { get; init; } = 1m;

    // --- scale that does not sit around zero ---

    /// Fixed lower bound, or null to take it from the data.
    public decimal? Low { get; init; }

    /// Fixed upper bound, or null to take it from the data.
    public decimal? High { get; init; }

    /// Where the coloured reference line goes, or null for no such line.
    public decimal? ReferenceLine { get; init; }

    /// Distance between the grey grid lines, or null for a quarter of the range.
    public decimal? GridStep { get; init; }

    /// Decimals used when the figure is written out as text.
    public int Decimals { get; init; }
}
