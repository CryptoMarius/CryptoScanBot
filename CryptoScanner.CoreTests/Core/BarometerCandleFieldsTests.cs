using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// CryptoCandle is a struct. Storing a measurement into one therefore has to go by reference: a
/// by-value parameter fills a copy that is discarded on return, and the caller writes back an
/// all-zero candle. That happened in the live scanner and produced a barometer history of nothing
/// but zeroes - silently, because a zero is a perfectly plausible barometer value.
/// </summary>
[TestClass]
public class BarometerCandleFieldsTests
{
    private static BarometerResult CreateResult(decimal[] percentages)
    {
        BarometerResult result = new();
        result.Reset();
        foreach (decimal percentage in percentages)
            result.Add(percentage);
        Assert.IsTrue(result.Calculate(), "Calculate() should succeed when symbols took part");
        return result;
    }


    [TestMethod]
    public void StoreWritesThroughToTheCandle()
    {
        // arrange - four coins, two of them rising
        BarometerResult result = CreateResult([-2m, -1m, 1m, 4m]);
        CryptoCandle candle = new() { TickDecimals = 2 };

        // act
        BarometerCandleFields.Store(ref candle, result);

        // assert - the candle must hold the measurement, not zeroes
        Assert.AreNotEqual(0m, candle.Close, "Close (average) was not written to the candle");
        Assert.AreEqual(result.Average, candle.Close, "Close should hold the average");
        Assert.AreEqual(result.Median, candle.Open, "Open should hold the median");
        Assert.AreEqual(result.PercentageRising, candle.High, "High should hold the breadth");
        Assert.AreEqual(result.Spread, candle.Low, "Low should hold the spread");
        Assert.AreEqual(result.SymbolCount, candle.Volume, "Volume should hold the symbol count");
    }


    [TestMethod]
    public void StoreAndReadRoundTripEveryFigure()
    {
        BarometerResult result = CreateResult([-2m, -1m, 1m, 4m]);
        CryptoCandle candle = new() { TickDecimals = 2 };
        BarometerCandleFields.Store(ref candle, result);

        Assert.AreEqual(result.Average, BarometerCandleFields.Read(candle, BarometerGraphValue.Average));
        Assert.AreEqual(result.Median, BarometerCandleFields.Read(candle, BarometerGraphValue.Median));
        Assert.AreEqual(result.PercentageRising, BarometerCandleFields.Read(candle, BarometerGraphValue.Rising));
        Assert.AreEqual(result.Spread, BarometerCandleFields.Read(candle, BarometerGraphValue.Spread));
        Assert.AreEqual(result.SymbolCount, BarometerCandleFields.Read(candle, BarometerGraphValue.SymbolCount));
    }


    [TestMethod]
    public void BreadthIsShiftedForTheGraphButNotForStorage()
    {
        // Two of four coins rose, so breadth is 50% - the neutral point. On the graph that has to
        // land exactly on the zero line, while the stored value stays 50 for the panel and tooltip.
        BarometerResult neutral = CreateResult([-2m, -1m, 1m, 4m]);
        CryptoCandle candle = new() { TickDecimals = 2 };
        BarometerCandleFields.Store(ref candle, neutral);

        Assert.AreEqual(50m, BarometerCandleFields.Read(candle, BarometerGraphValue.Rising), "stored value stays 0..100");
        Assert.AreEqual(0m, BarometerCandleFields.ReadForGraph(candle, BarometerGraphValue.Rising), "50% must sit on the zero line");

        // Three of four rising is 75%, which should draw as +25 (above the line, so green).
        BarometerResult mostlyUp = CreateResult([-2m, 1m, 2m, 4m]);
        CryptoCandle up = new() { TickDecimals = 2 };
        BarometerCandleFields.Store(ref up, mostlyUp);

        Assert.AreEqual(75m, BarometerCandleFields.Read(up, BarometerGraphValue.Rising));
        Assert.AreEqual(25m, BarometerCandleFields.ReadForGraph(up, BarometerGraphValue.Rising));

        // Every other figure is drawn as it is stored.
        Assert.AreEqual(0m, BarometerCandleFields.GetOffset(BarometerGraphValue.Average));
        Assert.AreEqual(0m, BarometerCandleFields.GetOffset(BarometerGraphValue.Median));
        Assert.AreEqual(0m, BarometerCandleFields.GetOffset(BarometerGraphValue.Spread));
        Assert.AreEqual(up.Close, BarometerCandleFields.ReadForGraph(up, BarometerGraphValue.Average));
    }


    [TestMethod]
    public void BreadthExtremesAreNotDiscardedAsOutliers()
    {
        // Shifted breadth reaches exactly -50 (no coin rose) and +50 (every coin rose). Those are
        // real readings, so the scale must not carry the ceiling that the average uses.
        Assert.IsNull(BarometerCandleFields.GetScale(BarometerGraphValue.Rising).IgnoreBeyond);
        Assert.AreEqual(50m, BarometerCandleFields.GetScale(BarometerGraphValue.Average).IgnoreBeyond);
    }


    [TestMethod]
    public void CoinCountIsNotOfferedInTheGraph()
    {
        // It is stored and shown in the tooltip, but as a graph it is a flat line by definition.
        CollectionAssert.DoesNotContain(BarometerCandleFields.Names.ToList(),
            BarometerCandleFields.GetName(BarometerGraphValue.SymbolCount));
        Assert.AreEqual(4, BarometerCandleFields.Names.Count);
    }


    [TestMethod]
    public void FiguresAreCalculatedCorrectly()
    {
        // -2, -1, 1, 4: average 0.5, median between -1 and 1 = 0, two of four rising = 50%
        BarometerResult result = CreateResult([-2m, -1m, 1m, 4m]);

        Assert.AreEqual(0.5m, result.Average, "average of -2, -1, 1, 4");
        Assert.AreEqual(0m, result.Median, "median of -2, -1, 1, 4");
        Assert.AreEqual(50m, result.PercentageRising, "two out of four rose");
        Assert.AreEqual(4, result.SymbolCount);
        Assert.IsTrue(result.Spread > 0m, "spread of a non-flat cross-section is positive");
    }


    [TestMethod]
    public void CalculateFailsWithoutSymbols()
    {
        // No coin took part - the caller must skip the measurement instead of storing zeroes.
        BarometerResult result = new();
        result.Reset();

        Assert.IsFalse(result.Calculate(), "Calculate() should fail when no symbol took part");
        Assert.AreEqual(0, result.SymbolCount);
    }


    [TestMethod]
    public void LegacyLayoutIsRecognised()
    {
        // Candles written before this class existed carried the same number in all four fields.
        CryptoCandle legacy = new() { TickDecimals = 2, Open = -0.42m, High = -0.42m, Low = -0.42m, Close = -0.42m };
        Assert.IsTrue(BarometerCandleFields.IsLegacyLayout(legacy), "old layout should be recognised");

        // An all-zero candle is the shape the by-value bug produced; it has to be recomputed too.
        CryptoCandle empty = new() { TickDecimals = 2 };
        Assert.IsTrue(BarometerCandleFields.IsLegacyLayout(empty), "an empty candle should be recomputed");

        // A properly filled candle must never be mistaken for the old layout.
        BarometerResult result = CreateResult([-2m, -1m, 1m, 4m]);
        CryptoCandle current = new() { TickDecimals = 2 };
        BarometerCandleFields.Store(ref current, result);
        Assert.IsFalse(BarometerCandleFields.IsLegacyLayout(current), "a filled candle is not the old layout");
    }
}
