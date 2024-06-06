﻿using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Emulator;

public class LoadHistoricalData
{
    public static async Task Execute(CryptoSymbol symbol, DateTime start, DateTime end)
    {
        GlobalData.AddTextToLogTab($"Fetching historical candles {symbol.Name}");
        long?[] backupArray = new long?[Enum.GetValues(typeof(CryptoIntervalPeriod)).Length];

        // load all available data
        foreach (var interval in GlobalData.IntervalList)
        {
            // Add some slack for initial MA200 etc..
            long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, start);
            DateTime startExtra = CandleTools.GetUnixDate(startFetchUnix);

            // force load from this date (todo: introduce caching)
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            backupArray[(int)symbolInterval.IntervalPeriod] = symbolInterval.LastCandleSynchronized;
            symbolInterval.LastCandleSynchronized = CandleTools.GetUnixTime(startExtra, 60);
            DateTime test = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);

            DateTime loop = startExtra;
            while (loop < end)
            {
                var period = StorageHistoricalData.GetPeriod(interval, loop);
                StorageHistoricalData.LoadData(symbol, interval, period.start);
                loop = period.end;
            }
            CandleTools.UpdateCandleFetched(symbol, interval);
        }

        //// show the loaded data
        //GlobalData.AddTextToLogTab($"Historical candles {symbol.Name} loaded");
        //foreach (var interval in GlobalData.IntervalList)
        //{
        //    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        //    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} candles={symbolInterval.CandleList.Count}");
        //}


        // fetch additional candles
        await CryptoScanBot.Core.Exchange.BybitSpot.CandlesEmulator.ExecuteAsync(symbol, end);

        // restore LastCandleSynchronized
        foreach (var interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            symbolInterval.LastCandleSynchronized = backupArray[(int)symbolInterval.IntervalPeriod];
        }

        // Save candles (the whole bunch)
        foreach (var interval in GlobalData.IntervalList)
        {
            StorageHistoricalData.SaveData(symbol, interval);
        }


        // Show some results
        GlobalData.AddTextToLogTab($"Historical candles {symbol.Name} total");
        foreach (var interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} candles={symbolInterval.CandleList.Count}");
        }


        GlobalData.AddTextToLogTab($"Fetching historical candles {symbol.Name} done");
    }
}