using Microsoft.VisualStudio.TestTools.UnitTesting;
using CryptoSbmScanner.Intern;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoSbmScanner.Model;
using CryptoSbmScanner.Enums;

namespace CryptoSbmScanner.Intern.Tests;

[TestClass()]
public class CandleToolsTests
{
    private static CryptoSymbol CreateTestSymbol()
    {
        CryptoSymbol symbol = new()
        {
            Status = 1,
            Name = "TESTUSDT",
            Base = "TEST",
            Quote = "USDT",

            QuantityTickSize = 0.0001m,
            QuantityMinimum= 0.0001m,
            QuantityMaximum = 999999m,

            PriceTickSize = 0.0001m,
            PriceMinimum = 0.0001m,
            PriceMaximum = 999999m
        };

        return symbol;
    }

    private static void SetupIntervals()
    {
        GlobalData.IntervalList.Clear();
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1m, "1m", 1 * 60, null)); //1
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2m, "2m", 2 * 60, GlobalData.IntervalList[0])); //1
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3m, "3m", 3 * 60, GlobalData.IntervalList[0])); //2
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval5m, "5m", 5 * 60, GlobalData.IntervalList[0])); //3
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval10m, "10m", 10 * 60, GlobalData.IntervalList[3])); //4
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval15m, "15m", 15 * 60, GlobalData.IntervalList[3]));  //5
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval30m, "30m", 30 * 60, GlobalData.IntervalList[5])); //6
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1h, "1h", 01 * 60 * 60, GlobalData.IntervalList[6])); //7
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2h, "2h", 02 * 60 * 60, GlobalData.IntervalList[7])); //8
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3h, "3h", 03 * 60 * 60, GlobalData.IntervalList[7])); //9
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval4h, "4h", 04 * 60 * 60, GlobalData.IntervalList[8])); //10
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval6h, "6h", 06 * 60 * 60, GlobalData.IntervalList[9])); //11
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval8h, "8h", 08 * 60 * 60, GlobalData.IntervalList[10])); //12
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval12h, "12h", 12 * 60 * 60, GlobalData.IntervalList[10])); //13
        GlobalData.IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1d, "1d", 1 * 24 * 60 * 60, GlobalData.IntervalList[11])); //14
    }

    private void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        text = text.TrimEnd();

        if (extraLineFeed)
            text += "\r\n\r\n";
        else
            text += "\r\n";

        Console.WriteLine(text);
    }


    [TestMethod()]
    public void CalculateCandleForIntervalTest()
    {
        // Description: toevoegen en mergen van candles (de happy flow)


        // arrange
        SetupIntervals();
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);
        CryptoSymbol symbol = CreateTestSymbol();


        // act

        decimal value = 19000;
        DateTime startTime = new(2023, 08, 27, 00, 00, 00, DateTimeKind.Utc);
        long startTimeUnix = CandleTools.GetUnixTime(startTime, 60);
        for (int count = 60; count <= 24*60*60; count+=60) // een complete dag
        {
            startTime = CandleTools.GetUnixDate(startTimeUnix);
            CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, GlobalData.IntervalList[0], startTime, value, value, value, value, 1, false);
            CandleTools.UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);
            string text = $"ticker(1m):" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true);
            Console.WriteLine(text);

            // Calculate higher timeframes
            long candle1mCloseTime = candle.OpenTime + 60;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                {
                    // Deze doet een call naar de TaskSaveCandles en de UpdateCandleFetched (overlappend?)
                    CryptoCandle candleX = CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle1mCloseTime);
                    CandleTools.UpdateCandleFetched(symbol, interval);
                    string text2 = $"ticker({interval.Name}):" + candleX.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true);
                    Console.WriteLine(text2);
                }
            }

            startTimeUnix += 60;


            // Assert
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                Assert.AreEqual(count / symbolPeriod.Interval.Duration, symbolPeriod.CandleList.Count, $"Aantal candles in de {symbolPeriod.Interval}");

                foreach (var c in symbolPeriod.CandleList.Values)
                {
                    long unix = c.OpenTime;
                    long diff = unix % interval.Duration;
                    Assert.AreEqual(0, diff, $"Candle.OpenTime");

                    Assert.AreEqual(value, c.Open, $"Candle.Open");
                    Assert.AreEqual(value, c.High, $"Candle.High");
                    Assert.AreEqual(value, c.Low, $"Candle.Low");
                    Assert.AreEqual(value, c.Close, $"Candle.Close");
                    
                    Assert.AreEqual(interval.Duration / 60, c.Volume, $"Candle.Volume");
                }
            }
        }
    }

}