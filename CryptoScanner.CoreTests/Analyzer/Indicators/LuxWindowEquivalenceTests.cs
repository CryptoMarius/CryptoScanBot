using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.CoreTests.Analyzer.Indicators;

/// <summary>
/// Holds both Lux Multi-RSI implementations to the Pine original
/// (<c>E:\PineScripts\RSI Multi Length [LuxAlgo].txt</c>). The original keeps its <c>var num</c> /
/// <c>var den</c> arrays for the WHOLE chart: no window, no bar skipped, and the value plotted on a
/// bar is the state after that bar. Everything below follows from those two facts:
/// <list type="number">
/// <item>the warm-up must be long enough that the answer no longer depends on it — 99 bars was not;</item>
/// <item>a candle's 5m value is the one at its LAST closed 5m sub-candle, not an earlier one.</item>
/// </list>
/// <para>
/// <c>IntervalIndicatorHub</c> advances the Lux RMA one step per candle and stores the result on
/// CryptoData; <c>LuxIndicator.CalculateNew</c> replays a fixed window and is called once per signal
/// from SignalCreate. That looks like plain duplication, and removing it is a tempting cleanup — the
/// hub value is already there, so why recompute?
/// </para>
/// <para>
/// Because they disagreed. The RMA alphas are 1/10..1/20, so the seed is largely forgotten within a
/// hundred bars, but the result is not the RMA — it is a COUNT of how many of the 11 RSI lengths sit
/// above 70 or below 30. A hair of difference in a borderline length flips the whole count by one,
/// and one length is 100/11 = 9.09 percentage points. This test measures how often that happens.
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class LuxWindowEquivalenceTests
{
    private const int LuxMin = 10;
    private const int LuxMax = 20;
    private const int LuxN = LuxMax - LuxMin + 1;

    /// <summary>The same series the golden reference uses, so this measures the same data.</summary>
    private static List<decimal> MakeCloses(int count)
    {
        var list = new List<decimal>(count);
        for (int i = 0; i < count; i++)
        {
            double mid = 100 + 15 * Math.Sin(i * 0.05) + 5 * Math.Sin(i * 0.23)
                       + 3 * Math.Cos(i * 0.11) + (i % 7) * 0.10;
            list.Add(Math.Round((decimal)mid, 4));
        }
        return list;
    }

    /// <summary>
    /// The Lux value at <paramref name="endIndex"/> after <paramref name="warmupBars"/> RMA steps.
    /// One step per price difference, so N steps span N+1 candles — the same counting
    /// LuxIndicator.WarmupBars uses.
    /// </summary>
    private static (int overSold, int overBought) LuxAt(List<decimal> closes, int endIndex, int warmupBars)
    {
        double[] num = new double[LuxN];
        double[] den = new double[LuxN];
        // warmupBars counts price DIFFERENCES, matching LuxIndicator.CalculateNew: it walks
        // (WarmupBars + 1) candles from end - WarmupBars*duration to end, and its first iteration
        // only seeds candlePrev, so exactly WarmupBars differences reach the RMA. Starting one bar
        // earlier here would model one extra step and no longer measure the same thing.
        int start = Math.Max(1, endIndex - warmupBars + 1);

        int overbuy = 0, oversell = 0;
        for (int bar = start; bar <= endIndex; bar++)
        {
            double diff = (double)(closes[bar] - closes[bar - 1]);
            overbuy = 0;
            oversell = 0;
            for (int k = 0; k < LuxN; k++)
            {
                double alpha = 1.0 / (LuxMin + k);
                num[k] = alpha * diff + (1.0 - alpha) * num[k];
                den[k] = alpha * Math.Abs(diff) + (1.0 - alpha) * den[k];
                double rsi = den[k] == 0.0 ? 50.0 : 50.0 * num[k] / den[k] + 50.0;
                if (rsi > 70) overbuy++;
                if (rsi < 30) oversell++;
            }
        }
        return ((int)(100.0 * oversell / LuxN), (int)(100.0 * overbuy / LuxN));
    }

    /// <summary>
    /// Counts how often a given warm-up disagrees with the windowless Pine original.
    /// </summary>
    private static (int mismatches, int compared, int largest) CompareToOriginal(int warmupBars)
    {
        const int Count = 2500;
        int firstComparable = warmupBars + 40;   // the window must be fully available
        var closes = MakeCloses(Count);

        int mismatches = 0, largest = 0;
        for (int i = firstComparable; i < Count; i++)
        {
            var windowed = LuxAt(closes, i, warmupBars);
            var original = LuxAt(closes, i, i);   // from bar 1 — what the Pine script does

            if (windowed != original)
            {
                mismatches++;
                largest = Math.Max(largest,
                    Math.Max(Math.Abs(windowed.overSold - original.overSold),
                             Math.Abs(windowed.overBought - original.overBought)));
            }
        }
        return (mismatches, Count - firstComparable, largest);
    }

    [TestMethod]
    public void TheOldNinetyNineBarWarmup_DidNotMatchTheOriginal()
    {
        var (mismatches, compared, largest) = CompareToOriginal(99);
        Console.WriteLine($"99-bar warm-up vs the Pine original: {mismatches}/{compared} candles differ "
            + $"({100.0 * mismatches / compared:N2}%), largest {largest} percentage points "
            + $"(one RSI length = {100.0 / LuxN:N2})");

        // Kept as the counter-example that justifies LuxIndicator.WarmupBars. If this ever reports
        // zero the series changed, not the maths — do not use it to argue 99 is good enough.
        Assert.AreNotEqual(0, mismatches,
            "The 99-bar warm-up used to be the reason the two Lux paths disagreed");
    }

    [TestMethod]
    public void TheWarmupWeUse_MatchesTheOriginal()
    {
        var (mismatches, compared, largest) = CompareToOriginal(LuxIndicator.WarmupBars);
        Console.WriteLine($"{LuxIndicator.WarmupBars}-bar warm-up vs the Pine original: "
            + $"{mismatches}/{compared} candles differ, largest {largest} percentage points");

        // Both paths (the incremental hub and LuxIndicator.CalculateNew) now warm up on this many
        // bars, so agreeing with the original here means they also agree with each other.
        Assert.AreEqual(0, mismatches,
            $"LuxIndicator.WarmupBars={LuxIndicator.WarmupBars} must reproduce the windowless original");
    }


    /// <summary>
    /// The Pine original runs on every bar and skips none, so the 5m value belonging to a candle is
    /// the one at its LAST closed 5m sub-candle. Both callers used to pick an earlier one.
    /// </summary>
    [TestMethod]
    public void LastClosed5mCandle_PicksTheFinalSubCandle()
    {
        // A 15m candle opening at minute 60 closes at 75; the 5m sub-candles are 60, 65 and 70.
        Assert.AreEqual(new CandleTime(70), LuxIndicator.LastClosed5mCandle(new CandleTime(60), 15),
            "A 15m candle must use its third 5m sub-candle, not its first");

        // A 1h candle opening at 60 closes at 120; the last closed 5m candle opens at 115.
        Assert.AreEqual(new CandleTime(115), LuxIndicator.LastClosed5mCandle(new CandleTime(60), 60),
            "A 1h candle must use the twelfth 5m sub-candle");

        // The 5m interval itself maps to itself.
        Assert.AreEqual(new CandleTime(60), LuxIndicator.LastClosed5mCandle(new CandleTime(60), 5));

        // Below 5m: a 1m candle opening at 63 closes at 64, so the newest CLOSED 5m candle is 55-60.
        Assert.AreEqual(new CandleTime(55), LuxIndicator.LastClosed5mCandle(new CandleTime(63), 1),
            "A 1m candle cannot use a 5m candle that has not closed yet");

        // Right on a boundary: a 1m candle opening at 59 closes exactly at 60, which is the moment
        // the 55-60 candle closes — so that one counts and 60-65 does not.
        Assert.AreEqual(new CandleTime(55), LuxIndicator.LastClosed5mCandle(new CandleTime(59), 1));

        // No underflow at the very start of the epoch.
        Assert.AreEqual(new CandleTime(0), LuxIndicator.LastClosed5mCandle(new CandleTime(0), 1));
    }
}
