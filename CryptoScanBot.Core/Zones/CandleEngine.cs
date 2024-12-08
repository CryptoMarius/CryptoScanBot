using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using System.Text;
using System.Text.Json;

namespace CryptoScanBot.ZoneVisualisation.Zones;

public class CandleEngine
{
    public static DateTime StartupTime { get; set; }

    public static void LoadCandleDataFromDisk(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList)
    {
        // load candles (kind of quick and dirty)
        string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
        string filename = baseFolder + $"{symbol.Name}-{interval.Name}.json";
        if (File.Exists(filename))
        {
            string text = File.ReadAllText(filename);
            var list = JsonSerializer.Deserialize<CryptoCandleList>(text, JsonTools.JsonSerializerIndented);
            if (list != null)
            {
                foreach (var c in list.Values)
                    candleList.TryAdd(c.OpenTime, c);
            }
            //GlobalData.AddTextToLogTab($"{symbol.Name} {symbolInterval.Interval!.Name} Loading file {filename} {added} candles added");
        }
    }


    public static void SaveCandleDataToDisk(CryptoSymbol symbol, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
        {
            if (loadedCandlesInMemory.TryGetValue(symbolInterval.IntervalPeriod, out bool changed) && changed)
            {
                // Save candles (kind of quick and dirty)
                string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
                string filename = baseFolder + $"{symbol.Name}-{symbolInterval.Interval.Name}.json";
                Directory.CreateDirectory(baseFolder);
                string text = JsonSerializer.Serialize(symbolInterval.CandleList, JsonTools.JsonSerializerIndented);
                File.WriteAllText(filename, text);

                //log.AppendLine($"saving {filename}");
                ScannerLog.Logger.Info($"Saving {filename}");
                loadedCandlesInMemory[symbolInterval.IntervalPeriod] = false; // in memory, nothing changed
            }
        }
    }


    /// <summary>
    /// Remove the not needed candles (using a copy because that is quicker)
    /// There is another clean method which removes the candles 1 by 1, but that is slow with large amounts of candles
    /// </summary>
    public static async Task CleanLoadedCandlesAsync(CryptoSymbol symbol)
    {
        await symbol.CandleLock.WaitAsync();
        try
        {
            foreach (var symbolInterval in symbol.IntervalPeriodList)
            {
                // Remove old candles
                if (symbolInterval.CandleList.Count > 0)
                {
                    // TODO: Need end date instead of DateTime.UtcNow (works in SignalGrid, but not here)
                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

                    // investigate the first, does it need removal?
                    CryptoCandle c = symbolInterval.CandleList.Values.First();
                    if (c.OpenTime < startFetchUnix)
                    {
                        // It takes forever to delete 100.000 of candles!!
                        // There is a *huge* amount of candles, just copy them to a new list
                        // This copies worst case 500 for the higher intervals, a bit more for the 1m
                        // TODO: Use TakeLast() does not work with sortedlist (investigate)
                        CryptoCandleList newList = [];
                        int index = symbolInterval.CandleList.Count - 1;
                        while (index > 0)
                        {
                            c = symbolInterval.CandleList.Values[index];
                            if (c.OpenTime < startFetchUnix)
                                break;
                            newList.Add(c.OpenTime, c);
                            index--;
                        }
                        symbolInterval.CandleList = newList;
                    }
                }
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }
    }


    /// <summary>
    /// Check if all candles in a date range are present
    /// </summary>
    private static (long unixStartTime, bool dataAllLocal) IsDataLocal(long minTime, long maxTime, CryptoInterval interval, CryptoCandleList candleList)
    {
        while (candleList!.ContainsKey(minTime))
        {
            if (minTime >= maxTime)
            {
                //string text2 = $"available={available}, need={okayCount}";
                //log.AppendLine($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                //ScannerLog.Logger.Info($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name}, {symbolInterval.Interval!.Name} candles available, no refresh needed {text2}");
                return (minTime, true);
            }
            minTime += interval.Duration;
        }
        return (minTime, false);
    }


    /// <summary>
    /// Calculate the date range needed to get x candles from a certain date
    /// </summary>
    private static (long unixMin, long unixMax) CalculateDates(CryptoInterval interval, long startTime, int candleCount)
    {
        long unixMinTime = IntervalTools.StartOfIntervalCandle(startTime, interval.Duration);
        long unixMaxTime = unixMinTime + candleCount * interval.Duration;

        long unixNowTime = CandleTools.GetUnixTime(StartupTime, 0); // todo, emulator date?
        unixNowTime = IntervalTools.StartOfIntervalCandle(unixNowTime, interval.Duration);

        if (unixMaxTime > unixNowTime)
            return (unixMinTime, unixNowTime);
        else
            return (unixMinTime, unixMaxTime);
    }


    public static async Task<bool> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList, long fetchFrom, int fetchCount)
    {
        if (!symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod))
            throw new Exception("Not supported interval");

        (long unixMin, long unixMax) = CalculateDates(interval, fetchFrom, fetchCount);
        (long unixLoop, bool dataAllLocal) = IsDataLocal(unixMin, unixMax, interval, candleList);
        if (dataAllLocal)
            return false;
        try
        {
            //string text3 = $"need={candleCount}"; 
            //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name} {symbolInterval.Interval!.Name} need more data {text3} {minDate.ToLocalTime()} .. {maxDate.ToLocalTime()}");
            return await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval, candleList, unixLoop, unixMax);
        }
        catch (Exception error)
        {
            // some stupid error i need to trace..
            GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} from={fetchFrom} count={fetchCount} min={unixMin} max={unixMax} loop={unixLoop} {error.Message}");
            throw;
        }
    }

}
