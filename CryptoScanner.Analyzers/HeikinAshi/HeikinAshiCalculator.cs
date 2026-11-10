using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.HeikinAshi;

/// <summary>
/// One Heikin Ashi candle, derived from a real candle and the Heikin Ashi candle before it.
/// </summary>
public readonly record struct HeikinAshiCandle(CandleTime OpenTime, decimal Open, decimal High, decimal Low, decimal Close)
{
    /// <summary>1 for a green candle (close above open), -1 for a red one, 0 when open and close are equal.</summary>
    public int Colour => Close > Open ? 1 : Close < Open ? -1 : 0;

    /// <summary>The full length of the candle, high minus low.</summary>
    public decimal Range => High - Low;

    /// <summary>The wick above the body.</summary>
    public decimal UpperWick => High - Math.Max(Open, Close);

    /// <summary>The wick under the body.</summary>
    public decimal LowerWick => Math.Min(Open, Close) - Low;

    /// <summary>
    /// Whether the wick above the body is longer than <paramref name="minimumPercentage"/> of the
    /// candle's range. A candle without a range has no wick.
    /// </summary>
    public bool HasUpperWick(decimal minimumPercentage)
        => Range > 0 && UpperWick > minimumPercentage / 100m * Range;

    /// <summary>
    /// Whether the wick under the body is longer than <paramref name="minimumPercentage"/> of the
    /// candle's range. A candle without a range has no wick.
    /// </summary>
    public bool HasLowerWick(decimal minimumPercentage)
        => Range > 0 && LowerWick > minimumPercentage / 100m * Range;
}


/// <summary>
/// Turns real candles into Heikin Ashi candles:
/// <list type="bullet">
/// <item>close = (open + high + low + close) / 4 of the real candle</item>
/// <item>open = (open + close) / 2 of the previous Heikin Ashi candle</item>
/// <item>high = the highest of the real high, the Heikin Ashi open and close</item>
/// <item>low = the lowest of the real low, the Heikin Ashi open and close</item>
/// </list>
/// Every candle depends on all candles before it. The first one is seeded with the open as the
/// average of the real open and close; the influence of that seed halves with every candle, so after
/// a few dozen candles it is gone.
/// </summary>
public static class HeikinAshiCalculator
{
    /// <summary>
    /// The Heikin Ashi candles of <paramref name="candles"/>, oldest first, in the same order.
    /// </summary>
    public static List<HeikinAshiCandle> Calculate(IReadOnlyList<CryptoCandle> candles)
    {
        List<HeikinAshiCandle> result = new(candles.Count);
        decimal previousOpen = 0m;
        decimal previousClose = 0m;
        for (int i = 0; i < candles.Count; i++)
        {
            CryptoCandle candle = candles[i];
            decimal close = (candle.Open + candle.High + candle.Low + candle.Close) / 4m;
            decimal open = i == 0 ? (candle.Open + candle.Close) / 2m : (previousOpen + previousClose) / 2m;
            decimal high = Math.Max(candle.High, Math.Max(open, close));
            decimal low = Math.Min(candle.Low, Math.Min(open, close));
            result.Add(new HeikinAshiCandle(candle.OpenTime, open, high, low, close));
            previousOpen = open;
            previousClose = close;
        }
        return result;
    }
}
