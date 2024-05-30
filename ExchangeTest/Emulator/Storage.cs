﻿using System.Text.Json;

using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace ExchangeTest.Emulator;

public class Storage
{

    /// <summary>
    /// What is the startdate for the filename
    /// </summary>
    public static (DateTime start, DateTime end) GetPeriod(CryptoInterval interval, DateTime date)
    {

        // 1m .. 30m
        if (interval.IntervalPeriod < CryptoIntervalPeriod.interval1h)
        {
            DateTime start = new(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = date.AddDays(1);
            return (start, end);
        }

        // 1h .. 3h
        if (interval.IntervalPeriod < CryptoIntervalPeriod.interval4h)
        {
            DateTime start = new(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = date.AddMonths(1);
            //int duration = DateTime.DaysInMonth(date.Year, date.Month);
            return (start, end);
        }

        // 4h and bigger
        {
            DateTime start = new(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime end = date.AddYears(1);
            //int duration = 365 + Convert.ToInt32(DateTime.IsLeapYear(date.Year));
            return (start, end);
        }
    }



    /// <summary>
    /// Foldername for this symbol/interval
    /// </summary>
    public static string GetFolder(CryptoSymbol symbol, CryptoInterval interval)
    {
        string folder = $@"{GlobalData.GetBaseDir()}\Emulator\Data\{symbol.Quote.ToLower()}\{symbol.Base.ToLower()}\{interval.Name}\";
        return folder;
    }

    public static string GetFolder(CryptoSymbol symbol, CryptoInterval interval, DateTime date)
    {
        var period = GetPeriod(interval, date);

        string s = period.start.Year.ToString("D4");
        s += period.start.Month.ToString("D2");
        s += period.start.Day.ToString("D2");

        return GetFolder(symbol, interval) + $"{s}.json";
    }

    public static void LoadData(CryptoSymbol symbol, CryptoInterval interval, DateTime date)
    {
        string filename = GetFolder(symbol, interval, date);
        if (File.Exists(filename))
        {
            try
            {
                string text = File.ReadAllText(filename);
                List<CryptoCandle> candleList = JsonSerializer.Deserialize<List<CryptoCandle>>(text, GlobalData.JsonSerializerIndented);

                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                foreach (var candle in candleList)
                {
                    if (!symbolInterval.CandleList.ContainsKey(candle.OpenTime))
                        symbolInterval.CandleList.Add(candle.OpenTime, candle);
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"Error loading {filename} {error}", false);
            }
        }
    }


    public static void SaveData(CryptoSymbol symbol, CryptoInterval interval)
    {

        string filename = "";
        string folder = GetFolder(symbol, interval);
        Directory.CreateDirectory(folder);
        try
        {
            (DateTime start, DateTime end) period;
            List<CryptoCandle> candleList = [];

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            if (symbolInterval.CandleList.Values.Count > 0)
            {
                // The storage name depends on the date (not very optimal)
                {
                    CryptoCandle candle = symbolInterval.CandleList.Values[0];
                    period = GetPeriod(interval, candle.Date);
                    filename = GetFolder(symbol, interval, candle.Date);
                }

                foreach (var candle in symbolInterval.CandleList.Values)
                {
                    if (candle.Date >= period.end)
                    {
                        // flush what we have collected
                        string text = JsonSerializer.Serialize<List<CryptoCandle>>(candleList, GlobalData.JsonSerializerIndented);
                        File.WriteAllText(filename, text);

                        // reset
                        candleList = [];
                        period = GetPeriod(interval, candle.Date);
                        filename = GetFolder(symbol, interval, candle.Date);
                    }
                    candleList.Add(candle);
                }

                // remainder
                if (filename != "" && candleList.Count > 0)
                {
                    // flush what we have collected
                    string text = JsonSerializer.Serialize<List<CryptoCandle>>(candleList, GlobalData.JsonSerializerIndented);
                    File.WriteAllText(filename, text);
                }
            }

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"Error saving {filename} {error}", false);
        }
    }

}