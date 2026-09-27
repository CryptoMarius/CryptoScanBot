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

        SupportResistanceResult result = SupportResistance.Scan(candles);

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
    /// Price bounces twice off 110 from below (two pivot highs), breaks out to 116, comes back to
    /// 110.2 and closes above it: a horizontal breakout and then a long flip on the level.
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
        Leg(109, 102, 10);
        Leg(102, 109, 7);
        // The breakout has to be ONE candle from under the level to more than BreakAtr * ATR above
        // it; a price that creeps through (a close just above first) only moves the level's side.
        candles.Add(Candle(i++, 109, 116.3, 108.7, 116));
        Leg(116, 110.3, 6);  // retest of 110 from above
        Leg(110.3, 115, 10);

        SupportResistanceResult result = SupportResistance.Scan(candles);

        Assert.IsTrue(result.Events.Exists(e => e.Kind == SupportResistanceKind.Horizontal
            && e.Type == SupportResistanceEventType.Breakout && e.Side == CryptoTradeSide.Long),
            "the break above the double top");
        var flip = result.Events.Find(e => e.Kind == SupportResistanceKind.Horizontal
            && e.Type == SupportResistanceEventType.Flip && e.Side == CryptoTradeSide.Long);
        Assert.AreNotEqual(default, flip, "the retest that held");
        Assert.IsTrue(Math.Abs(flip.Level - 110.3) < 1.0, $"flip on the 110 level, got {flip.Level}");
        Assert.IsTrue(flip.Stop < flip.Level, "the stop of a long flip lies under the level");
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
