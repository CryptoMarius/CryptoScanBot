using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Trend;

/// <summary>
/// Support/resistance (open point 46) is a port of the offline measurement in CryptoScanBot.tools
/// (studies/sr-flip/sr_flip_scan.py). These tests hold the port to that measurement: on the two
/// emulator candle files the C# version has to find exactly the breakouts and flips the Python
/// version wrote (export_reference.py), event for event. Plus a few small hand-made cases that say
/// what the rules are.
/// </summary>
[TestClass]
public class SupportResistanceTests : TestBase
{
    // The measurement uses simple fractal pivots; the scanner uses its ZigZag (28-09-2026). The
    // comparison with the measurement runs on the fractal source, everything else on the default.
    private static readonly SupportResistance.Parameters Fractal = SupportResistance.Default with { PivotSource = SupportResistancePivots.Fractal };

    private static List<CryptoCandle> Load(string file)
    {
        InitTestSession();
        CryptoCandleList list = [];
        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("Error assembly");
        LoadCandleDataFromDisk(list, Path.Combine(path, "Zones", "Data", file));
        return [.. list.Values];
    }

    private sealed record Expected(string kind, string @event, int side, int index, long time, double stop);


    [TestMethod]
    [DataRow("SOLUSDT-1h")]
    [DataRow("XRPUSDT-15m")]
    public void TheEventsAreThoseOfTheMeasurement(string name)
    {
        List<CryptoCandle> candles = Load(name + ".json");
        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        var expected = JsonSerializer.Deserialize<List<Expected>>(
            File.ReadAllText(Path.Combine(path, "Zones", "Data", name + "-sr-events.json")))!;

        SupportResistanceResult result = SupportResistance.Scan(candles, Fractal);

        int shown = 0;
        int count = Math.Min(expected.Count, result.Events.Count);
        for (int k = 0; k < count && shown < 5; k++)
        {
            var e = expected[k];
            var a = result.Events[k];
            string kind = a.Kind == SupportResistanceKind.Horizontal ? "horizontal" : "sloped";
            string type = a.Type == SupportResistanceEventType.Flip ? "flip" : "breakout";
            int side = a.Side == CryptoTradeSide.Long ? 1 : -1;
            bool same = kind == e.kind && type == e.@event && side == e.side && a.Index == e.index
                && Math.Abs(a.Stop - e.stop) <= 1e-8 * Math.Max(1, Math.Abs(e.stop));
            if (!same)
            {
                Console.WriteLine($"#{k}: expected {e.kind} {e.@event} {e.side} @{e.index} stop {e.stop}, got {kind} {type} {side} @{a.Index} stop {a.Stop}");
                shown++;
            }
        }
        Assert.AreEqual(0, shown, "events differ from the measurement, see the output");
        Assert.AreEqual(expected.Count, result.Events.Count, "number of events");
    }


    private static CryptoCandle Candle(int index, double open, double high, double low, double close)
        => new()
        {
            TickDecimals = 4,
            OpenTime = new CandleTime((uint)(10_000 + index * 60)),
            Open = (decimal)open,
            High = (decimal)high,
            Low = (decimal)low,
            Close = (decimal)close,
            Volume = 1,
        };


    /// <summary>
    /// Price bounces three times off 110 from below (three pivot highs - since 28-09-2026 a level needs
    /// three before it breaks), breaks out to 116, runs away and comes back to 110.3 and closes above
    /// it: a horizontal breakout and then a long flip on the level.
    /// </summary>
    [TestMethod]
    public void AResistanceThatBreaksAndHoldsIsALongFlip()
    {
        List<CryptoCandle> candles = [];
        int i = 0;
        void Leg(double from, double to, int steps)
        {
            for (int k = 1; k <= steps; k++)
            {
                double c = from + (to - from) * k / steps;
                double o = from + (to - from) * (k - 1) / steps;
                candles.Add(Candle(i++, o, Math.Max(o, c) + 0.3, Math.Min(o, c) - 0.3, c));
            }
        }
        // One candle with a clear high of its own: a fractal pivot needs a UNIQUE highest high
        void Top(double price)
            => candles.Add(Candle(i++, price - 1, price, price - 1.5, price - 0.8));

        Leg(100, 100, 20);   // quiet start for the ATR
        Leg(100, 108, 10);
        Top(110);            // first top at 110
        Leg(109, 101, 10);
        Leg(101, 108, 10);
        Top(110.1);          // second top at 110
        Leg(109, 101, 10);
        Leg(101, 108, 10);
        Top(110.05);         // third top at 110
        Leg(109, 102, 10);
        Leg(102, 109, 7);
        // The breakout has to be ONE candle from under the level to more than BreakAtr * ATR above
        // it; a price that creeps through (a close just above first) only moves the level's side.
        candles.Add(Candle(i++, 109, 116.3, 108.7, 116));
        Leg(116, 110.3, 6);  // retest of 110 from above
        Leg(110.3, 115, 10);

        SupportResistanceResult result = SupportResistance.Scan(candles, Fractal);

        Assert.IsTrue(result.Events.Exists(e => e.Kind == SupportResistanceKind.Horizontal
            && e.Type == SupportResistanceEventType.Breakout && e.Side == CryptoTradeSide.Long),
            "the break above the double top");
        var flip = result.Events.Find(e => e.Kind == SupportResistanceKind.Horizontal
            && e.Type == SupportResistanceEventType.Flip && e.Side == CryptoTradeSide.Long);
        Assert.AreNotEqual(default, flip, "the retest that held");
        Assert.IsTrue(Math.Abs(flip.Level - 110.3) < 1.0, $"flip on the 110 level, got {flip.Level}");
        Assert.IsTrue(flip.Stop < flip.Level, "the stop of a long flip lies under the level");
    }


    /// <summary>
    /// A sloped line the way a trader draws one (28-09-2026): at every moment, the lines that are
    /// still standing run through a higher and a lower top (bottom), and no close went beyond them
    /// from their first point on - unless the line is in a running break.
    /// </summary>
    [TestMethod]
    [DataRow("SOLUSDT-1h")]
    [DataRow("XRPUSDT-15m")]
    public void AStandingLineHasNoCloseBeyondIt(string name)
    {
        List<CryptoCandle> candles = Load(name + ".json");
        int checkedLines = 0;
        for (int end = 400; end <= candles.Count; end += 250)
        {
            var part = candles.GetRange(0, end);
            SupportResistanceResult result = SupportResistance.Scan(part);
            foreach (SupportResistanceLine line in result.Lines)
            {
                if (line.BrokenAt != null)
                    continue;
                Assert.IsTrue(line.IsResistance ? line.Price2 < line.Price1 : line.Price2 > line.Price1, "falling tops, rising bottoms");
                for (int k = line.Index1 + 1; k < part.Count; k++)
                {
                    double value = line.ValueAt(k);
                    double close = (double)part[k].Close;
                    Assert.IsTrue(line.IsResistance ? close <= value : close >= value,
                        $"{name} candle {k}: close {close} beyond the line ({value}) that runs from {line.Index1} to {line.Index2}");
                }
                checkedLines++;
            }
        }
        Assert.IsTrue(checkedLines > 0, "there were standing lines to check");
    }


    /// <summary>
    /// The scanner's source (28-09-2026): the tops and bottoms are the points of its own ZigZag,
    /// secondary, on the wicks - the ZigZag the chart draws. Every standing line runs through two of
    /// them, and never through the last point, which can still move.
    /// </summary>
    [TestMethod]
    [DataRow("SOLUSDT-1h")]
    [DataRow("XRPUSDT-15m")]
    public void TheLinesRunThroughZigZagPoints(string name)
    {
        List<CryptoCandle> candles = Load(name + ".json");
        SupportResistanceResult result = SupportResistance.Scan(candles);

        var zigZag = new ZigZagIndicator(TrendType.Secondary, useHighLow: true);
        foreach (CryptoCandle candle in candles)
            zigZag.Calculate(candle, batchProcess: true);
        var real = zigZag.ZigZagList.Where(z => !z.Dummy).ToList();
        var points = real.Take(real.Count - 1).Select(z => (z.Candle.OpenTime, z.Value)).ToHashSet();

        Console.WriteLine($"{name}: {result.Events.Count(e => e.Type == SupportResistanceEventType.Flip)} flips, "
            + $"{result.Levels.Count} levels, {result.Lines.Count} lines at the end");
        Assert.IsTrue(result.Events.Count > 0, "the ZigZag source finds breakouts and flips");
        foreach (SupportResistanceLine line in result.Lines)
        {
            Assert.IsTrue(points.Contains((candles[line.Index1].OpenTime, line.Price1)), "first point is a ZigZag point");
            Assert.IsTrue(points.Contains((candles[line.Index2].OpenTime, line.Price2)), "second point is a final ZigZag point, not the last one");
        }
    }


    /// <summary>A decision at candle i may not depend on later candles.</summary>
    [TestMethod]
    public void NothingLooksIntoTheFuture()
    {
        List<CryptoCandle> candles = Load("SOLUSDT-1h.json");
        SupportResistanceResult full = SupportResistance.Scan(candles);
        int cut = candles.Count / 2;
        SupportResistanceResult half = SupportResistance.Scan(candles.GetRange(0, cut));

        var expected = full.Events.Where(e => e.Index < cut).ToList();
        CollectionAssert.AreEqual(expected, half.Events, "the first half must give the same events on its own");
    }
}
