using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.CoreTests.Analyzer;

/// <summary>
/// Sanity checks for the experimental <see cref="SlideDetector"/>: a clean steady decline should be
/// flagged as a slide, while a flat/choppy market and a clean uptrend should not.
/// </summary>
[TestClass]
public class SlideDetectorTests
{
    private const int Length = 50;

    [TestMethod]
    public void CleanDownTrendIsDetectedAsSlide()
    {
        // Steady, almost-linear decline: strong negative slope, very high R².
        var candles = BuildCandles(300, startPrice: 100m, perBarDrift: -0.20m, noise: 0.05, seed: 1);
        var results = SlideDetector.Detect(candles, Length);

        int ready = results.Count(r => r.Ready);
        int sliding = results.Count(r => r.IsSliding);
        Console.WriteLine($"down: ready={ready}, sliding={sliding}");

        // The vast majority of ready candles in a clean bleed should be flagged.
        Assert.IsTrue(sliding > ready * 0.8, $"Expected most candles to be sliding (sliding={sliding}, ready={ready})");
    }

    [TestMethod]
    public void FlatMarketIsNotSlide()
    {
        var candles = BuildCandles(300, startPrice: 100m, perBarDrift: 0m, noise: 0.30, seed: 2);
        var results = SlideDetector.Detect(candles, Length);
        int sliding = results.Count(r => r.IsSliding);
        Console.WriteLine($"flat: sliding={sliding}");
        Assert.AreEqual(0, sliding, "A flat/choppy market should not be flagged as a slide");
    }

    [TestMethod]
    public void CleanUpTrendIsNotSlide()
    {
        var candles = BuildCandles(300, startPrice: 50m, perBarDrift: +0.20m, noise: 0.05, seed: 3);
        var results = SlideDetector.Detect(candles, Length);
        int sliding = results.Count(r => r.IsSliding);
        Console.WriteLine($"up: sliding={sliding}");
        Assert.AreEqual(0, sliding, "A clean uptrend should not be flagged as a slide");
    }

    private static List<CryptoCandle> BuildCandles(int count, decimal startPrice, decimal perBarDrift, double noise, int seed)
    {
        var rnd = new Random(seed);
        var list = new List<CryptoCandle>(count);
        decimal price = startPrice;
        for (int i = 0; i < count; i++)
        {
            decimal jitter = (decimal)((rnd.NextDouble() - 0.5) * 2.0 * noise);
            decimal close = price + perBarDrift + jitter;
            if (close < 1m) close = 1m;
            decimal open = price;
            decimal high = Math.Max(open, close) + (decimal)(rnd.NextDouble() * noise);
            decimal low = Math.Min(open, close) - (decimal)(rnd.NextDouble() * noise);
            if (low < 1m) low = 1m;

            list.Add(new CryptoCandle
            {
                OpenTime = new CandleTime((uint)(1000 + i)),
                TickDecimals = 2,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = 1m,
            });
            price = close;
        }
        return list;
    }
}
