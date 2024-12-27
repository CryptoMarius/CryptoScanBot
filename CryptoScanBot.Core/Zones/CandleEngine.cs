using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;

using System.Text;

namespace CryptoScanBot.Core.Zones;

public class CandleEngine
{
    private static async Task ReadFromBin(CryptoSymbol symbol, CryptoInterval interval, string filename)
    {
        // json does take a lot of memory
        await symbol.CandleLock.WaitAsync();
        try
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval.IntervalPeriod);
            using FileStream readStream = new(filename, FileMode.Open, FileAccess.Read, FileShare.None, 65536);
            using BinaryReader binaryReader = new(readStream, Encoding.UTF8, false);

            // Iets met een version
            int version = binaryReader.ReadInt32();
            int candleCount = binaryReader.ReadInt32();
            while (candleCount-- > 0)
            {
                CryptoCandle candle = new()
                {
                    OpenTime = binaryReader.ReadInt64(),
                    Open = binaryReader.ReadDecimal(),
                    High = binaryReader.ReadDecimal(),
                    Low = binaryReader.ReadDecimal(),
                    Close = binaryReader.ReadDecimal(),
                    Volume = binaryReader.ReadDecimal(),
                };
                symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            }
        }
        finally
        {
            symbol.CandleLock.Release();
        }
    }


    //private static async Task ReadFromTxt(CryptoSymbol symbol, CryptoInterval interval, string filename)
    //{
    //    // json does take a lot of memory
    //    await symbol.CandleLock.WaitAsync();
    //    try
    //    {
    //        CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval.IntervalPeriod);

    //        using FileStream stream = File.OpenRead(filename);
    //        var list = await JsonSerializer.DeserializeAsync<List<CryptoCandle>>(stream, JsonTools.DeSerializerOptions);
    //        //string text = File.ReadAllText(filename);
    //        //var list = JsonSerializer.Deserialize<CryptoCandleList>(text);
    //        if (list != null)
    //        {
    //            foreach (var c in list)
    //                symbolInterval.CandleList.TryAdd(c.OpenTime, c);
    //        }
    //    }
    //    finally
    //    {
    //        symbol.CandleLock.Release();
    //    }
    //}


    public static async Task LoadCandleDataFromDiskAsync(CryptoSymbol symbol, CryptoInterval interval)
    {
        // load candles (kind of quick and dirty)
        string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
        string filenameBin = baseFolder + $"{symbol.Name}-{interval.Name}.bin";
        //string filenameTxt = baseFolder + $"{symbol.Name}-{interval.Name}.json";
        if (File.Exists(filenameBin))
            await ReadFromBin(symbol, interval, filenameBin);
        //else if (File.Exists(filenameTxt))
        //    await ReadFromTxt(symbol, interval, filenameTxt);
        //GlobalData.AddTextToLogTab($"{symbol.Name} {symbolInterval.Interval!.Name} Loading file {filename} {symbolInterval.CandleList.Count} candles");
    }

    public static async Task WriteToBin(CryptoSymbol symbol, CryptoInterval interval, string filename)
    {
        // json does take a lot of memory
        CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval.IntervalPeriod);

        await symbol.CandleLock.WaitAsync();
        try
        {
            //using ZipArchive zipStream = new(writeStream, ZipArchiveMode.Create, true);

            using FileStream writeStream = new(filename, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
            using BinaryWriter binaryWriter = new(writeStream, Encoding.UTF8, false);

            // Iets met een version
            int version = 1;
            binaryWriter.Write(version);
            int count = symbolInterval.CandleList.Count;
            binaryWriter.Write(count);

            foreach (var pair in symbolInterval.CandleList)
            {
                CryptoCandle? candle = pair.Value;
                if (candle != null)
                {
                    binaryWriter.Write(candle.OpenTime);
                    binaryWriter.Write(candle.Open);
                    binaryWriter.Write(candle.High);
                    binaryWriter.Write(candle.Low);
                    binaryWriter.Write(candle.Close);
                    binaryWriter.Write(candle.Volume);
                }
            }

            //if (count > 0)
            //{
            //    long openTime = symbolInterval.CandleList.Keys.First();
            //    while (count-- > 0)
            //    {
            //        //for (int j = 0; j < symbolInterval.CandleList.Count; j++)
            //        {
            //            CryptoCandle? candle = symbolInterval.CandleList.Values[j];
            //            if (candle != null)
            //            {
            //                binaryWriter.Write(candle.OpenTime);
            //                binaryWriter.Write(candle.Open);
            //                binaryWriter.Write(candle.High);
            //                binaryWriter.Write(candle.Low);
            //                binaryWriter.Write(candle.Close);
            //                binaryWriter.Write(candle.Volume);
            //            }
            //        }
            //        openTime += interval.Duration;
            //    }
            //}
        }
        finally
        {
            symbol.CandleLock.Release();
        }
    }

    //public static async Task WriteToTxt(CryptoSymbol symbol, CryptoInterval interval, string filename)
    //{
    //    CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval.IntervalPeriod);

    //    await symbol.CandleLock.WaitAsync();
    //    try
    //    {
    //        using FileStream stream = File.OpenWrite(filename);
    //        await JsonSerializer.SerializeAsync(stream, symbolInterval.CandleList.Values, JsonTools.JsonSerializerIndented);
    //    }
    //    finally
    //    {
    //        symbol.CandleLock.Release();
    //    }
    //}


    public static async Task SaveCandleDataToDiskAsync(CryptoSymbol symbol, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
        {
            if (loadedCandlesInMemory.TryGetValue(symbolInterval.IntervalPeriod, out bool changed) && changed)
            {
                string baseFolder = GlobalData.GetBaseDir() + @"Pivots\";
                Directory.CreateDirectory(baseFolder);

                string filenameBin = baseFolder + $"{symbol.Name}-{symbolInterval.Interval.Name}.bin";
                await WriteToBin(symbol, symbolInterval.Interval, filenameBin);

                //string filenameTxt = baseFolder + $"{symbol.Name}-{symbolInterval.Interval.Name}.json";
                //await WriteToTxt(symbol, symbolInterval.Interval, filenameTxt);

                //log.AppendLine($"saving {filename}");
                //ScannerLog.Logger.Info($"Saving {filenameBin}");
                loadedCandlesInMemory[symbolInterval.IntervalPeriod] = false; // in memory, nothing changed

                //GlobalData.AddTextToLogTab($"{symbol.Name} {symbolInterval.Interval!.Name} Saving file {filename} {symbolInterval.CandleList.Count} candles");
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
                //int cleaned = symbolInterval.CandleList.Count;
                // Remove old candles
                if (symbolInterval.CandleList.Count > 0)
                {
                    // TODO: Need end date instead of DateTime.UtcNow (works in SignalGrid, but not here)
                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

                    // investigate the first, does it need removal?
                    long openTime = symbolInterval.CandleList.Keys.First();
                    if (openTime < startFetchUnix)
                    {
                        // It takes forever to delete 100.000 of candles!!
                        // There is a *huge* amount of candles, just copy them to a new list
                        // This copies worst case 500 for the higher intervals, a bit more for the 1m
                        // TODO: Use TakeLast() does not work with sortedlist (investigate)
                        CryptoCandleList newList = [];

                        long unix = symbolInterval.CandleList.Keys.Last();
                        while (unix >= startFetchUnix)
                        {
                            if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? c))
                                newList.Add(c.OpenTime, c);
                            unix -= symbolInterval.Interval.Duration;
                        }


                        //int index = symbolInterval.CandleList.Count - 1;
                        //while (index > 0)
                        //{
                        //    CryptoCandle c = symbolInterval.CandleList.Values[index];
                        //    if (c.OpenTime < startFetchUnix)
                        //        break;
                        //    newList.Add(c.OpenTime, c);
                        //    index--;
                        //}
                        symbolInterval.CandleList = newList;
                        //symbolInterval.CandleList.TrimExcess();
                    }
                }
                //GlobalData.AddTextToLogTab($"{symbol.Name} {symbolInterval.Interval!.Name} Cleaning {cleaned - symbolInterval.CandleList.Count} candles");
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
    private static (long unixStartTime, bool dataAllLocal) IsDataLocal(long minTime, long maxTime, CryptoSymbol symbol, CryptoInterval interval)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        while (symbolInterval.CandleList!.ContainsKey(minTime))
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

        long unixNowTime = CandleTools.GetUnixTime(DateTime.UtcNow, 0); // todo, emulator date?
        unixNowTime = IntervalTools.StartOfIntervalCandle(unixNowTime, interval.Duration);

        if (unixMaxTime > unixNowTime)
            return (unixMinTime, unixNowTime);
        else
            return (unixMinTime, unixMaxTime);
    }


    public static async Task<bool> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, long fetchFrom, int fetchCount)
    {
        if (!symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod))
            throw new Exception("Not supported interval");

        //CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        (long unixMin, long unixMax) = CalculateDates(interval, fetchFrom, fetchCount);
        (long unixLoop, bool dataAllLocal) = IsDataLocal(unixMin, unixMax, symbol, interval);
        if (dataAllLocal)
            return false;
        try
        {
            //string text3 = $"need={candleCount}"; 
            //GlobalData.AddTextToLogTab($"Fetch historical data {symbol.Name} {symbolInterval.Interval!.Name} need more data {text3} {minDate.ToLocalTime()} .. {maxDate.ToLocalTime()}");
            return await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval, unixLoop, unixMax);
        }
        catch (Exception error)
        {
            // some stupid error i need to trace..
            GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} from={fetchFrom} count={fetchCount} min={unixMin} max={unixMax} loop={unixLoop} {error.Message}");
            throw;
        }
    }

}
