using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Trend;

/// <summary>
/// The short filter "no short just above a support" (open point 46,
/// SettingsEntryConditions.SkipShortAboveSupport). The levels it reads have to be the levels of the
/// measurement it comes from (levels_at() in CryptoScanBot.tools, studies/sr-flip), and the filter
/// has to decide on exactly those levels.
/// </summary>
[DoNotParallelize]
[TestClass]
public class SupportResistanceShortFilterTests : TestBase
{
    private sealed record ExpectedLevels(int index, double atr, List<double> levels);

    private static string DataPath(string file)
        => Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "Zones", "Data", file);


    [TestMethod]
    public void TheLevelsAreThoseOfTheMeasurement()
    {
        InitTestSession();
        CryptoCandleList list = [];
        LoadCandleDataFromDisk(list, DataPath("SOLUSDT-1h.json"));
        List<CryptoCandle> candles = [.. list.Values];
        var expected = JsonSerializer.Deserialize<List<ExpectedLevels>>(File.ReadAllText(DataPath("SOLUSDT-1h-sr-levels.json")))!;

        foreach (var e in expected)
        {
            var levels = SupportResistance.CurrentLevels(candles.GetRange(0, e.index + 1), out double atr);
            Assert.AreEqual(e.atr, atr, 1e-9 * e.atr, $"ATR at candle {e.index}");
            CollectionAssert.AreEqual(e.levels.Select(x => Math.Round(x, 8)).ToList(),
                levels.Select(l => Math.Round(l.Price, 8)).ToList(), $"levels at candle {e.index}");
        }
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


    [TestMethod]
    public void TheRoomIsMeasuredToTheNearestSupportUnderThePrice()
    {
        // Two clear bottoms at 100 (one candle each with a unique low), then price at 106
        List<CryptoCandle> candles = [];
        int i = 0;
        for (int k = 0; k < 20; k++)
            candles.Add(Candle(i++, 105, 105.5, 104.5, 105));
        candles.Add(Candle(i++, 101, 101.5, 100, 101));
        for (int k = 0; k < 10; k++)
            candles.Add(Candle(i++, 105, 105.5, 104.5, 105));
        candles.Add(Candle(i++, 101, 101.5, 100.05, 101));
        for (int k = 0; k < 10; k++)
            candles.Add(Candle(i++, 106, 106.5, 105.5, 106));

        double? room = SupportResistance.RoomToNextLevel(candles, 106, CryptoTradeSide.Short, out double level);
        Assert.IsNotNull(room, "the double bottom is a support");
        Assert.AreEqual(100.025, level, 1e-9);
        Assert.IsTrue(room > 1, $"six points above the support is more than an ATR, got {room}");

        room = SupportResistance.RoomToNextLevel(candles, 100.3, CryptoTradeSide.Short, out _);
        Assert.IsTrue(room < 0.5, $"just above the support, got {room}");

        room = SupportResistance.RoomToNextLevel(candles, 99, CryptoTradeSide.Short, out _);
        Assert.IsNull(room, "under the support there is no support in the way of a short");
    }


    /// <summary>Opens the protected check of SignalCreateBase for the test.</summary>
    private sealed class Probe : SignalCreateBase
    {
        public bool RoomAboveSupport(decimal minimum) => CheckRoomAboveSupport(minimum);
    }


    [TestMethod]
    public void TheFilterDecidesOnThoseLevels()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval hour = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(hour.IntervalPeriod);
        LoadCandleDataFromDisk(symbolInterval.CandleList, DataPath("SOLUSDT-1h.json"));
        List<CryptoCandle> candles = [.. symbolInterval.CandleList.Values];

        int skipped = 0, allowed = 0;
        for (int k = 600; k < candles.Count; k += 37)
        {
            var probe = new Probe
            {
                Symbol = symbol,
                Interval = hour,
                SymbolInterval = symbolInterval,
                SignalSide = CryptoTradeSide.Short,
                SignalStrategy = "test",
                CandleLast = new MyData { Candle = candles[k], CandleData = new CryptoData() },
            };

            // The same window the filter takes: the last 500 closed 1h candles
            int from = Math.Max(0, k - CandleTools.CandleCountFetch + 1);
            double? room = SupportResistance.RoomToNextLevel(candles.GetRange(from, k - from + 1),
                (double)candles[k].Close, CryptoTradeSide.Short, out _);
            bool expected = room == null || room.Value >= 0.5;

            bool actual = probe.RoomAboveSupport(0.5m);
            Assert.AreEqual(expected, actual, $"candle {k}: room {room}, {probe.ExtraText}");
            if (actual)
                allowed++;
            else
                skipped++;
        }
        Assert.IsTrue(skipped > 0 && allowed > 0, $"both outcomes occur (skipped {skipped}, allowed {allowed})");
    }
}
