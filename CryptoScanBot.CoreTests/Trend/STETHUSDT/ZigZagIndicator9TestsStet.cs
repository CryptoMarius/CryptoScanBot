using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.CoreTests;


[TestClass()]
public class ZigZagIndicator9TestsStet : TestBase
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
    public void CalculateTestStetUsdt()
    {
        // We noticed STETUSDT has a strange secondary trend, see two pictures..
        // there is a low between 2 h, but its kind of a high low..

        // arrange

        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Error assembly");
        LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Trend\\STETHUSDT\\STETHUSDT-1h.json"));

        // Trend via open/close
        ZigZagIndicator9 indicator = new(symbolInterval.CandleList, false, 1.0m, interval.Duration)
        {
            ShowSecondary = true
        };


        List<DataForTest> list =
        [
            // just need to add all the other points..
            new() { I = 679, D = new DateTime(2024, 12, 09, 12, 00, 00, DateTimeKind.Utc), L = 679, H = 677, T = "a L and the box is broken" },
            new() { I = 680, D = new DateTime(2024, 12, 09, 15, 00, 00, DateTimeKind.Utc), L = 679, H = 680, T = "a created high" },
            new() { I = 681, D = new DateTime(2024, 12, 09, 16, 00, 00, DateTimeKind.Utc), L = 681, H = 680, T = "a L box broken, new high created" },
            new() { I = 682, D = new DateTime(2024, 12, 10, 01, 00, 00, DateTimeKind.Utc), L = 681, H = 680, T = "a H but this one is lower than the last L" },
            // the previous low 681 is higher than the last high 682, the high should not have been created, fixed! (the L is automaticly corrected)
            new() { I = 683, D = new DateTime(2024, 12, 10, 03, 00, 00, DateTimeKind.Utc), L = 683, H = 680, T = "a L and the box is broken" },
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
