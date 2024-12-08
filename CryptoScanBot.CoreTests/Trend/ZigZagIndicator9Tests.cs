using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.CoreTests;


[TestClass()]
public class ZigZagIndicator9Tests : TestBase
{
    private class DataForTest
    {
        public required int I;
        public required DateTime D;
        public required int L;
        public required int H;
        public required string T;
    }

    [TestMethod]
    public void CalculateTest()
    {
        // We noticed ETHUSDT has a strange trend, see picture..

        // prepare

        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        // arrange

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        //string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Error assembly");
        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Error assembly");
        LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Trend\\ETHUSDT\\ETHUSDT-1h.json"));

        // Trend via open/close
        ZigZagIndicator9 indicator = new(symbolInterval.CandleList, false, 1.0m, interval.Duration);


        List<DataForTest> list =
        [
            // just need to add all the other points..
            new() { I = 661, D = new DateTime(2024, 12, 02, 18, 00, 00, DateTimeKind.Utc), L = 661, H = 660, T = "a L and the box is broken" },
            new() { I = 662, D = new DateTime(2024, 12, 03, 06, 00, 00, DateTimeKind.Utc), L = 661, H = 660, T = "a H and the box is not broken, ignored (nothing changed)" },
            new() { I = 663, D = new DateTime(2024, 12, 03, 14, 00, 00, DateTimeKind.Utc), L = 663, H = 662, T = "a L and the box is broken" },
            new() { I = 664, D = new DateTime(2024, 12, 04, 01, 00, 00, DateTimeKind.Utc), L = 663, H = 664, T = "a H and the box is broken" },
            // (it now gets interesting, an ignored point), added a fix because of this, the pivotindex was not properly set because of the reused pount..
            new() { I = 665, D = new DateTime(2024, 12, 04, 14, 00, 00, DateTimeKind.Utc), L = 663, H = 664, T = "a L and the box is not broken, ignored (nothing changed)" },
            new() { I = 666, D = new DateTime(2024, 12, 04, 21, 00, 00, DateTimeKind.Utc), L = 663, H = 666, T = "a repeated H and the box is broken" },
            new() { I = 667, D = new DateTime(2024, 12, 05, 01, 00, 00, DateTimeKind.Utc), L = 663, H = 666, T = "a L and the box is not broken, ignored (nothing changed)" },
            new() { I = 668, D = new DateTime(2024, 12, 05, 03, 00, 00, DateTimeKind.Utc), L = 663, H = 666, T = "a H but the box is not broken, ignored (nothing changed)" },
            new() { I = 669, D = new DateTime(2024, 12, 05, 12, 00, 00, DateTimeKind.Utc), L = 667, H = 669, T = "a H and the box is broken" },
            new() { I = 670, D = new DateTime(2024, 12, 06, 00, 00, 00, DateTimeKind.Utc), L = 670, H = 669, T = "a L and the box is broken" },
        ];


        // act


        long key = symbolInterval.CandleList.Values.First().OpenTime;
        foreach (var data in list)
        {
            while (indicator.PivotList.Count <= data.I)
            {
                if (symbolInterval.CandleList.TryGetValue(key, out CryptoCandle? candle))
                    indicator.Calculate(candle, true);
                key += interval.Duration;
            }

            string comment = $"{data.I} {data.D} {data.T}";
            DateTime dateLastPivot = indicator.PivotList[^1].Candle.Date;
            Assert.AreEqual(data.D, dateLastPivot, $"last pivot date not correct {comment}");
            Assert.AreEqual(data.L, indicator.LastSwingLow!.PivotIndex, $"last swing low not correct {comment}");
            Assert.AreEqual(data.H, indicator.LastSwingHigh!.PivotIndex, $"last swing high not correct {comment}");
        }
    }
}