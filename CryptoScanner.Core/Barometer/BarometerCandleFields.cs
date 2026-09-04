using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// Which figure of a barometer measurement the graph draws.
/// </summary>
public enum BarometerGraphValue
{
    // Stored in the candles of the primary barometer symbol ($BMP)
    Average,
    Median,
    Rising,
    Spread,
    SymbolCount,

    // Stored in the candles of the second symbol ($BMX)
    Movement,
    BitcoinVersusMarket,
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
    /// Write a completed measurement into the price fields of the PRIMARY barometer candle ($BMP).
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
    /// Write the same measurement into the SECOND barometer candle ($BMX). A candle holds five
    /// numbers and the first symbol is full, so the figures that came later live here. Two of the
    /// five are in use; the rest is room for what comes next.
    /// <para>
    /// Both candles are written in the same pass from the same measurement, so they always describe
    /// the same minute. Bitcoin-versus-market has no value on a quote without a bitcoin pair; a zero
    /// is stored then, and the tooltip leaves the line out rather than showing a fake zero.
    /// </para>
    /// <para>Takes the candle by ref for the same reason as Store() above.</para>
    /// </summary>
    public static void StoreExtra(ref CryptoCandle candle, BarometerResult result)
    {
        candle.Open = result.BitcoinVersusMarket ?? 0m;
        candle.High = result.OutlierCount;      // see the overload below - both paths must agree
        candle.Low = 0m;                        // free
        candle.Close = result.AverageAbsolute;
        candle.Volume = 0;                      // free
    }


    /// <summary>
    /// The same two writes, but from a stored measurement instead of a fresh <see cref="BarometerResult"/>.
    /// <para>
    /// The emulator needs this: it measures per quote coin and per interval and then writes the
    /// heartbeat from what was stored, so the shared result object it calculated with has moved on
    /// to another interval by then. Kept here next to <see cref="Store"/> for the reason this whole
    /// class exists - a second copy of the field layout somewhere else would drift at the first
    /// change, and the failure would be a graph quietly plotting the wrong figure.
    /// </para>
    /// </summary>
    public static void Store(ref CryptoCandle candle, CryptoBarometerData data)
    {
        candle.Open = data.PriceMedian ?? 0m;
        candle.High = data.PricePercentageRising ?? 0m;
        candle.Low = data.PriceSpread ?? 0m;
        candle.Close = data.PriceBarometer ?? 0m;
        candle.Volume = data.PriceSymbolCount ?? 0;
    }


    /// <summary>
    /// The second page from a stored measurement. Outliers land in the High field, which was free:
    /// they say "this reading rests on fewer coins than it looks", and that is exactly the sort of
    /// thing you want to be able to see back afterwards rather than only in a live tooltip.
    /// </summary>
    public static void StoreExtra(ref CryptoCandle candle, CryptoBarometerData data)
    {
        candle.Open = data.PriceBitcoinVersusMarket ?? 0m;
        candle.High = data.PriceOutlierCount ?? 0m;
        candle.Low = 0m;                        // free
        candle.Close = data.PriceMovement ?? 0m;
        candle.Volume = 0;                      // free
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


    /// <summary>
    /// Read one figure back out of a barometer candle. The candle has to come from the symbol that
    /// figure lives in - see GetSymbolName.
    /// </summary>
    public static decimal Read(CryptoCandle candle, BarometerGraphValue value)
    {
        return value switch
        {
            // Primary symbol ($BMP)
            BarometerGraphValue.Median => candle.Open,
            BarometerGraphValue.Rising => candle.High,
            BarometerGraphValue.Spread => candle.Low,
            BarometerGraphValue.SymbolCount => candle.Volume,

            // Second symbol ($BMX)
            BarometerGraphValue.BitcoinVersusMarket => candle.Open,
            BarometerGraphValue.Movement => candle.Close,

            _ => candle.Close,
        };
    }


    /// <summary>
    /// Which barometer symbol holds this figure. The name still needs the quote appended, the same
    /// way the rest of the code builds it.
    /// </summary>
    public static string GetSymbolName(BarometerGraphValue value)
    {
        return value is BarometerGraphValue.Movement or BarometerGraphValue.BitcoinVersusMarket
            ? Const.Constants.SymbolNameBarometerExtra
            : Const.Constants.SymbolNameBarometerPrice;
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
            BarometerGraphValue.Movement => "Movement",
            BarometerGraphValue.BitcoinVersusMarket => "BTC vs rest",
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
        BarometerGraphValue.Movement,
        BarometerGraphValue.BitcoinVersusMarket,
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
        if (barometer.PriceMovement.HasValue)
            lines.Add($"Movement {barometer.PriceMovement.Value:N2}% (regardless of direction)");

        // Absent on a quote without a bitcoin pair - better no line than a fake zero, which would
        // read as "bitcoin moves exactly with the market".
        if (barometer.PriceBitcoinVersusMarket.HasValue)
            lines.Add($"BTC vs rest {barometer.PriceBitcoinVersusMarket.Value:N2}%");
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
            // How far the typical coin moved, ignoring direction. Never negative, and like the
            // spread it is neither good nor bad, so it gets no reference line.
            BarometerGraphValue.Movement => new BarometerGraphScale
            {
                CenteredOnZero = false,
                Low = 0m,
                High = null,            // grows with the data
                ReferenceLine = null,
                GridStep = null,
                Decimals = 2,
            },
            // Bitcoin against the median coin: zero means it moves exactly with the market, so this
            // belongs on the same zero-centred scale as the average, and it moves in the same order
            // of magnitude. Same floor of 1 therefore - erring high is the dangerous side, see the
            // note on the average below.
            BarometerGraphValue.BitcoinVersusMarket => new BarometerGraphScale
            {
                CenteredOnZero = true,
                MinimumSpan = 1m,
                IgnoreBeyond = 50m,     // same malfunction guard as the average
                Decimals = 2,
            },
            // Average and median. MinimumSpan is deliberately NOT the 5 the graph was born with:
            // that number came from a comment reading "barometer, something like -5 .. +5", which is
            // the range of a single coin, not of an average over hundreds of them. Measured over a
            // full graph window the average spans 0.7 and the median 0.3 percentage points, so a
            // floor of 5 flattened the line onto a fifteenth of the picture. Do not restore it in
            // the name of matching the original behaviour - the original behaviour is the bug.
            _ => new BarometerGraphScale
            {
                CenteredOnZero = true,
                MinimumSpan = 1m,
                IgnoreBeyond = 50m,     // malfunctions seen on Bybit Perpetual
                Decimals = 2,
            },
        };
    }


    /// <summary>
    /// Round steps a grid line may sit on, from fine to coarse. Only values that read well on an
    /// axis - no 0.3 or 0.7 - so the reader can tell what a line is worth without a label.
    /// </summary>
    private static readonly decimal[] GridSteps =
        [0.01m, 0.02m, 0.05m, 0.1m, 0.2m, 0.25m, 0.5m, 1m, 2m, 5m, 10m, 20m, 25m, 50m];

    /// <summary>Above this the lines start to read as a grey block instead of a grid.</summary>
    private const int MaxGridLines = 8;

    /// <summary>
    /// Grid lines for a scale centred on zero, derived from the span that is actually in view.
    /// <para>
    /// The grid was fixed at -3..+3 with one line per percent, which only suits one span. Now that
    /// the span follows the data down to a single percentage point, a fixed grid would leave nothing
    /// but the zero line in view - so the step is picked from the span instead: the finest round step
    /// that keeps the count under MaxGridLines.
    /// </para>
    /// <para>
    /// Both bounds are whole multiples of the step, which is what puts a line on zero. Deriving them
    /// from the span directly would offset the whole grid by half a step and lose that line, and the
    /// zero line is the one the graph is read against.
    /// </para>
    /// </summary>
    public static (decimal Low, decimal High, decimal Step) GetCenteredGrid(decimal span)
    {
        decimal half = span / 2m;

        decimal step = GridSteps[^1];
        foreach (decimal candidate in GridSteps)
        {
            if (span / candidate <= MaxGridLines)
            {
                step = candidate;
                break;
            }
        }

        decimal lines = Math.Floor(half / step);
        return (-lines * step, lines * step, step);
    }
}


/// <summary>
/// The vertical scale of the barometer graph for one figure.
/// <para>
/// CenteredOnZero selects the scale the graph has always used: symmetric around zero, growing with
/// the data but never below MinimumSpan, with the zero line in red and grey lines around it.
/// </para>
/// <para>
/// MinimumSpan has no default worth relying on: it is the one number that decides whether the line
/// fills the picture or lies flat against the middle, and it differs per figure by an order of
/// magnitude (a percentage point for the average, twenty for the breadth). Every centred scale
/// states it, so a new figure cannot silently inherit a floor that flattens it.
/// </para>
/// <para>
/// The remaining fields apply only when CenteredOnZero is false.
/// </para>
/// </summary>
public sealed class BarometerGraphScale
{
    public bool CenteredOnZero { get; init; }

    // --- centred scale ---

    /// <summary>
    /// Never zoom in further than this vertical span. Zero means no floor at all: the scale then
    /// follows the data whatever it does. That is the safe default - forgetting to set it costs a
    /// jumpy graph in a quiet market, which is visible, while a floor that is too high costs a flat
    /// line, which reads as "nothing is happening" and hides itself.
    /// </summary>
    public decimal MinimumSpan { get; init; }

    /// <summary>
    /// Values this far from zero or further are left out of the scale: for a percentage change they
    /// are exchange malfunctions, and one of them would flatten the whole graph. Null when the figure
    /// has no such ceiling - breadth reaches exactly -50 and +50 at its extremes, which are real
    /// readings (no coin rose, or every coin did) and must not be discarded.
    /// </summary>
    public decimal? IgnoreBeyond { get; init; }

    // The centred scale has no grid fields of its own: its lines follow the span that ends up in
    // view, see BarometerCandleFields.GetCenteredGrid.

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
