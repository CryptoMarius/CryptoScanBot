using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Mac;

/// <summary>The four cloud lines and the levels in force at one candle.</summary>
public readonly record struct MacLineValues(
    double? EmaFast, double? EmaSecond, double? SmaMedium, double? SmaSlow,
    double? PivotHigh, double? PivotLow)
{
    public bool HasCloud => EmaFast != null && EmaSecond != null && SmaMedium != null && SmaSlow != null;

    /// <summary>
    /// The highest of the lines that EXIST at this candle, null when none of them do.
    /// <para>
    /// Deliberately looser than the same pair on MacCandleData, which the strategy reads: that one
    /// wants all four lines before it calls anything a cloud. Here the cloud is a drawing, and
    /// waiting for the slow SMA would leave the first 150 candles of every chart empty - which is
    /// exactly what "scroll left and the cloud is gone" was.
    /// </para>
    /// </summary>
    public double? CloudTop
    {
        get
        {
            double? top = null;
            foreach (double? line in new[] { EmaFast, EmaSecond, SmaMedium, SmaSlow })
            {
                if (line != null && (top == null || line.Value > top.Value))
                    top = line;
            }
            return top;
        }
    }

    /// <summary>The lowest of the lines that exist at this candle, null when none of them do.</summary>
    public double? CloudBottom
    {
        get
        {
            double? bottom = null;
            foreach (double? line in new[] { EmaFast, EmaSecond, SmaMedium, SmaSlow })
            {
                if (line != null && (bottom == null || line.Value < bottom.Value))
                    bottom = line;
            }
            return bottom;
        }
    }
}

/// <summary>
/// The MAC lines over a WHOLE candle list, for the chart overlay.
/// <para>
/// The strategy itself does not use this: it reads the values the indicator hub computed one candle
/// at a time (<see cref="Indicators.MacIndicatorExtension"/>). Two paths to the same numbers is a
/// known way to drift apart, so the pivot rule below is deliberately the same walk as the one in
/// the extension - candidate at <c>right</c> candles back, strictly higher (lower) than every other
/// candle in the window - and a test compares the two against each other.
/// </para>
/// <para>
/// A moving average is a moving average, so the lines cannot drift far; they come from Skender here
/// and from the hub there, both on the close.
/// </para>
/// </summary>
public static class MacLinesHelper
{
    public static MacLineValues[] Compute(List<CryptoCandle> candles)
    {
        var result = new MacLineValues[candles.Count];
        if (candles.Count == 0)
            return result;

        MacSettings settings = MacPlugin.Settings;
        var lengths = settings.Lines();
        int fastLength = Math.Max(1, lengths.Fast);
        int secondLength = Math.Max(fastLength + 1, lengths.Second);
        int mediumLength = Math.Max(2, lengths.Medium);
        int slowLength = Math.Max(mediumLength + 1, lengths.Slow);

        var quotes = candles.AsQuotes();
        var fast = quotes.ToEma(fastLength);
        var second = quotes.ToEma(secondLength);
        var medium = quotes.ToSma(mediumLength);
        var slow = quotes.ToSma(slowLength);

        int left = Math.Max(1, settings.PivotLeftCandles);
        int right = Math.Max(1, settings.PivotRightCandles);
        int window = left + right + 1;

        double? pivotHigh = null;
        double? pivotLow = null;
        for (int i = 0; i < candles.Count; i++)
        {
            // A pivot is only confirmed once its right-hand candles are in, so at candle i the
            // candidate sits at i - right and the window runs from i - window + 1 to i.
            if (i >= window - 1)
            {
                int candidate = i - right;
                decimal high = candles[candidate].High;
                decimal low = candles[candidate].Low;
                bool isHigh = true;
                bool isLow = true;
                for (int j = i - window + 1; j <= i; j++)
                {
                    if (j == candidate)
                        continue;
                    if (isHigh && candles[j].High >= high)
                        isHigh = false;
                    if (isLow && candles[j].Low <= low)
                        isLow = false;
                    if (!isHigh && !isLow)
                        break;
                }
                if (isHigh)
                    pivotHigh = (double)high;
                if (isLow)
                    pivotLow = (double)low;
            }

            result[i] = new MacLineValues(
                fast[i].Ema, second[i].Ema, medium[i].Sma, slow[i].Sma, pivotHigh, pivotLow);
        }

        return result;
    }
}
