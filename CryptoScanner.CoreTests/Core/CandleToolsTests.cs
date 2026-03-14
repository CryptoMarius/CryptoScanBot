using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

[TestClass]
public class CandleToolsTests : TestBase
{
    [TestMethod()]
    public async Task CalculateCandleForIntervalTestAsync()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        // arrange
        CryptoSymbol symbol = CreateTestSymbol(database);

        // act
        decimal value = 19000;
        DateTime startDateTime = new(2023, 08, 27, 00, 00, 00, DateTimeKind.Utc);
        CandleTime startTime = CandleTime.AlignFromDateTime(startDateTime, 1);
        for (int count = 1; count <= 24 * 60; count += 1) // 1 single day
        {
            startDateTime = startTime.ToDateTime();

            CryptoCandle candle = await CandleTools.Process1mCandleAsync(symbol, startDateTime, value, value, value, value, 1);
            CandleTools.UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);
            string text = $"ticker(1m):" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true);
            Console.WriteLine(text);

            //// Calculate higher timeframes
            //long candle1mCloseTime = candle.OpenTime + 1;
            //foreach (CryptoInterval interval in GlobalData.IntervalList)
            //{
            //    if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
            //    {
            //        // Deze doet een call naar de TaskSaveCandles en de UpdateCandleFetched (overlappend?)
            //        CryptoCandle? candleX = CandleTools.CalculateCandleForInterval(symbol, interval.ConstructFrom, interval, candle1mCloseTime);
            //        CandleTools.UpdateCandleFetched(symbol, interval);
            //        if (candleX != null)
            //        {
            //            string text2 = $"ticker({interval.Name}):" + candleX.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true);
            //            Console.WriteLine(text2);
            //        }
            //    }
            //}

            startTime += 1; // 1m


            // Assert
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                Assert.AreEqual(count / symbolPeriod.Interval?.Duration, symbolPeriod.CandleList.Count, $"Aantal candles in de {symbolPeriod.Interval}");

                foreach (var c in symbolPeriod.CandleList.Values)
                {
                    CandleTime minutes = c.OpenTime;
                    long diff = minutes % interval.Duration;
                    Assert.AreEqual(0, diff, $"Candle.OpenTime");

                    Assert.AreEqual(value, c.Open, $"Candle.Open");
                    Assert.AreEqual(value, c.High, $"Candle.High");
                    Assert.AreEqual(value, c.Low, $"Candle.Low");
                    Assert.AreEqual(value, c.Close, $"Candle.Close");

                    Assert.AreEqual(interval.Duration, c.Volume, $"Candle.Volume");
                }
            }
        }
    }

}