using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;

using System.Text;

namespace CryptoScanner.Core.Zones;

public class ZoneCandleEngine
{
    private const int markerValue = 1234567890;

    private static async Task ReadCandlesFromStreamAsync(BinaryReader reader, CryptoSymbol symbol, CryptoInterval interval)
    {
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            int version = reader.ReadInt32();

            // "Synchronisation" marker (new in version 4)
            if (version != 1)
            {
                int marker = reader.ReadInt32();
                if (marker != markerValue)
                    throw new Exception($"file {symbol.Name} is corrupted");
            }

            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval.IntervalPeriod);

            // 2: Candle count
            int candleCount = reader.ReadInt32();

            symbolInterval.CandleList.Lock();
            try
            {
                // 3: The OHLCV values
                // TODO: candlelist can grow while reading
                while (candleCount > 0)
                {
                    CryptoCandle candle = new()
                    {
                        TickDecimals = symbol.PriceDecimals
                    };
                    if (version == 1)
                    {
                        candle.OpenTime = CandleTime.FromUnixSeconds(reader.ReadInt64());
                        candle.Open = reader.ReadDecimal();
                        candle.High = reader.ReadDecimal();
                        candle.Low = reader.ReadDecimal();
                        candle.Close = reader.ReadDecimal();
                        candle.Volume = reader.ReadDecimal();
                    }
                    else
                    {
                        // Delegates to the newer candle storage systen
                        candle.LoadVersion3(reader);
                    }

                    // We had some data corruption and 1 candle in the year 2150...
                    // It is not a nice solution, but skip those candles (really weird)
                    //if (candle.OpenTime >= startFetchUnix)
                    {
                        //if (candle.OpenTime < futureCandles)
                        {
                            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
                            if (symbolInterval.LastCandle.OpenTime == 0 || candle.OpenTime >= symbolInterval.LastCandle.OpenTime)
                                symbolInterval.LastCandle = candle;
                        }
                        //else
                        //    GlobalData.AddTextToLogTab($"{symbol.Name} skipped corrupted candle {candle.OpenTime}");
                    }

                    candleCount--;
                }
            }
            finally
            {
                symbolInterval.CandleList.Unlock();
            }
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
    }

    public static async Task ReadCandlesFromDiskAsync(CryptoSymbol symbol, CryptoInterval interval)
    {
        string oldFileName = Path.Combine(GlobalData.AppDataFolder, "Pivots", $"{symbol.Name}-{interval.Name}.bin");
        string newFileName = Path.Combine(GlobalData.AppDataFolder, symbol.Exchange.Name.ToLower(), symbol.Quote.ToLower(), $"{symbol.Base.ToLower()}-{interval.Name}.compressed");
        string fileName = string.Empty;
        try
        {
            // an old uncompressed file
            if (File.Exists(oldFileName))
            {
                fileName = oldFileName;
                using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                using BinaryReader binaryReader = new(fileStream, Encoding.UTF8, false);
                await ReadCandlesFromStreamAsync(binaryReader, symbol, interval);
            }
            // a new compressed file (preferred)
            else if (File.Exists(newFileName))
            {
                fileName = newFileName;
                using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                using LZ4DecoderStream lz4Stream = LZ4Stream.Decode(fileStream);
                using BinaryReader binaryReader = new(lz4Stream, Encoding.UTF8, false);
                await ReadCandlesFromStreamAsync(binaryReader, symbol, interval);
            }
        }
        catch (Exception error)
        {
            GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} {error.Message}");
            File.Delete(fileName);
            throw;
        }
    }


    private static async Task WriteCandlesToStreamAsync(BinaryWriter writer, CryptoSymbol symbol, CryptoInterval interval)
    {
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval.IntervalPeriod);

            int version = 2;
            writer.Write(version);

            int marker = markerValue;
            writer.Write(marker);

            symbolInterval.CandleList.Lock();
            try
            {
                int count = symbolInterval.CandleList.Count;
                writer.Write(count);

                foreach (var candle in symbolInterval.CandleList.Values)
                {
                    candle.SaveVersion3(writer);
                }
            }
            finally
            {
                symbolInterval.CandleList.Unlock();
            }
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
    }

    private static async Task WriteCandlesToFileAsync(CryptoSymbol symbol, CryptoInterval interval)
    {
        string quoteFolder = Path.Combine(GlobalData.AppDataFolder, symbol.Exchange.Name.ToLower());
        Directory.CreateDirectory(quoteFolder);

        quoteFolder = Path.Combine(quoteFolder, symbol.Quote.ToLower());
        Directory.CreateDirectory(quoteFolder);

        string oldFileName = Path.Combine(GlobalData.AppDataFolder, "Pivots", $"{symbol.Name}-{interval.Name}.bin");
        string newFileName = Path.Combine(quoteFolder, $"{symbol.Base.ToLower()}-{interval.Name}.compressed");
        try
        {
            // delete the old uncompressed file
            if (File.Exists(oldFileName))
                File.Delete(oldFileName);

            // a new compressed file (preferred)
            using FileStream fileStream = new(newFileName, FileMode.Create, FileAccess.Write, FileShare.None, 2 * 1024 * 1024);
            using LZ4EncoderStream lz4Stream = LZ4Stream.Encode(fileStream, LZ4Level.L00_FAST);
            using BinaryWriter binaryWriter = new(lz4Stream, Encoding.UTF8, false);
            await WriteCandlesToStreamAsync(binaryWriter, symbol, interval);
        }
        catch (Exception error)
        {
            GlobalData.AddTextToLogTab($"ERROR writing {symbol.Name} {interval.Name} {error.Message}");
            if (File.Exists(newFileName))
                File.Delete(newFileName);
            throw;
        }

    }


    public static async Task SaveCandleDataToDiskAsync(CryptoSymbol symbol, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            if (loadedCandlesInMemory.TryGetValue(symbolInterval.IntervalPeriod, out bool changed) && changed)
            {
                await WriteCandlesToFileAsync(symbol, symbolInterval.Interval);

                //log.AppendLine($"saving {filename}");
                //ScannerLog.Logger.Info($"Saving {fileName}");
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
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                //int cleaned = symbolInterval.CandleList.Count;
                // Remove old candles
                if (symbolInterval.CandleList.Count > 0)
                {
                    // TODO: Need end date instead of DateTime.UtcNow (works in SignalGrid, but not here)
                    CandleTime startFetchUnix = CandleTools.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

                    // investigate the first, does it need removal?
                    CandleTime openTime = symbolInterval.CandleList.Keys.First();
                    if (openTime < startFetchUnix)
                    {
                        // It takes forever to delete 100.000 of candles!!
                        // There is a *huge* amount of candles, just copy them to a new list
                        // This copies worst case 500 for the higher intervals, a bit more for the 1m
                        // TODO: Use TakeLast() does not work with sortedlist (investigate)
                        CryptoCandleList newList = [];

                        CandleTime unix = symbolInterval.CandleList.Keys.Last();
                        while (unix >= startFetchUnix)
                        {
                            if (symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle c))
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
            symbol.Data.CandleLock.Release();
        }
    }


    /// <summary>
    /// Check if all candles in a date range are present
    /// </summary>
    private static (CandleTime unixStartTime, bool dataAllLocal) IsDataLocal(CandleTime minTime, CandleTime maxTime, CryptoSymbol symbol, CryptoInterval interval)
    {
        bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");
        if (debug)
            ScannerLog.Logger.Info($"CandleEngine.IsDataLocal({symbol.Name}, {interval!.Name}, " +
                $"{minTime.ToDateTime()}, {maxTime.ToDateTime()} (call)");

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

        if (debug)
            GlobalData.AddTextToLogTab($"CandleEngine.IsDataLocal({symbol.Name}, {interval!.Name}, " +
                $"{minTime.ToDateTime()} not present ");

        return (minTime, false);
    }


    /// <summary>
    /// Calculate the date range needed to get x candles from a certain date
    /// </summary>
    private static (CandleTime unixMin, CandleTime unixMax) CalculateDates(
        CryptoInterval interval, CandleTime startTime, int candleCount)
    {
        CandleTime unixMinTime = IntervalTools.StartOfIntervalCandle(startTime, interval.Duration);
        CandleTime unixMaxTime = unixMinTime + candleCount * interval.Duration;

        CandleTime unixNowTime = CandleTime.AlignFromDateTime(DateTime.UtcNow, 0); // todo, emulator date?
        unixNowTime = IntervalTools.StartOfIntervalCandle(unixNowTime, interval.Duration);

        if (unixMaxTime >= unixNowTime)
            return (unixMinTime, unixNowTime); // 1 to much?
        else
            return (unixMinTime, unixMaxTime);
    }

    // ff experimenteren...
    // TODO: Limit the load from disk (we now load everything we have which can be too much)
    // TODO: CalculateDates: Can now be less candles than fetchCount if some candles where present (is this bad?)?
    public static async Task FetchFrom(SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime fetchFrom, int fetchCount)
    {
        // Load candles from disk
        if (!loadedCandlesInMemory.TryGetValue(interval.IntervalPeriod, out bool _))
        {
            await ReadCandlesFromDiskAsync(symbol, interval);
            loadedCandlesInMemory.TryAdd(interval.IntervalPeriod, true); // for now (because of klines)
        }

        (CandleTime unixMin, CandleTime unixMax) = CalculateDates(interval, fetchFrom, fetchCount);
        (CandleTime unixLoop, bool dataAllLocal) = IsDataLocal(unixMin, unixMax, symbol, interval);
        try
        {
            if (!dataAllLocal)
            {
                if (symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod))
                {
                    // Load the candles from the exchange
                    bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");
                    if (debug)
                        ScannerLog.Logger.Info($"CandleEngine.FetchFrom({symbol.Name}, {interval!.Name}, " +
                            $"{unixLoop.ToDateTime()} .. {unixMax.ToDateTime()}");

                    bool result = await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval, unixLoop, unixMax);
                    if (result)
                        loadedCandlesInMemory[interval.IntervalPeriod] = true;
                }
                else
                {
                    // Do we need to calculate them from a lower interval?
                    var lowerInterval = interval.ConstructFrom
                        ?? throw new Exception("Unable to construct candles from lower interval");
                    fetchFrom -= fetchFrom % lowerInterval.Duration;
                    fetchCount *= (int)interval.Duration / (int)lowerInterval.Duration;
                    await FetchFrom(loadedCandlesInMemory, symbol, lowerInterval!, fetchFrom, fetchCount);

                    // TODO: Calculate the needed candles in the interval from the lowerInterval...
                    CandleTime unixNowTime = CandleTime.AlignFromDateTime(DateTime.UtcNow, 0); // todo, emulator date?
                    unixNowTime = IntervalTools.StartOfIntervalCandle(unixNowTime, lowerInterval.Duration);
                    CandleTools.BulkCalculateCandles(symbol, lowerInterval, interval, unixNowTime);
                    loadedCandlesInMemory[interval.IntervalPeriod] = true;
                }
            }
        }
        catch (Exception error)
        {
            // some stupid error i need to trace..
            GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} from={fetchFrom} count={fetchCount} min={unixMin} max={unixMax} loop={unixLoop} {error.Message}");
            throw;
        }
    }

    //public static async Task<bool> FetchFrom(CryptoSymbol symbol, CryptoInterval interval, long fetchFrom, int fetchCount)
    //{
    //    //bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");

    //    //if (debug)
    //    //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, {fetchCount}, " +
    //    //        $"{CandleTools.GetUnixDate(fetchFrom)}");

    //    if (!symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod))
    //        throw new Exception("Not supported interval");

    //    (long unixMin, long unixMax) = CalculateDates(interval, fetchFrom, fetchCount);
    //    (long unixLoop, bool dataAllLocal) = IsDataLocal(unixMin, unixMax, symbol, interval);
    //    if (dataAllLocal)
    //        return false;
    //    try
    //    {
    //        //if (debug)
    //        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, {fetchCount}, " +
    //        //        $"{CandleTools.GetUnixDate(unixMin)}, {CandleTools.GetUnixDate(unixMax)}");

    //        bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");
    //        if (debug)
    //            GlobalData.AddTextToLogTab($"CandleEngine.FetchFrom({symbol.Name}, {interval!.Name}, " +
    //                $"{CandleTools.GetUnixDate(unixLoop)} .. {CandleTools.GetUnixDate(unixMax)}");

    //        bool result = await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval, unixLoop, unixMax);

    //        //// check!!!
    //        //(unixLoop, dataAllLocal) = IsDataLocal(unixMin, unixMax, symbol, interval);
    //        //if (!dataAllLocal && debug)
    //        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, {fetchCount}, " +
    //        //        $"{CandleTools.GetUnixDate(unixMin)}, {CandleTools.GetUnixDate(unixMax)} not everything local!");

    //        return result;
    //    }
    //    catch (Exception error)
    //    {
    //        // some stupid error i need to trace..
    //        GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} from={fetchFrom} count={fetchCount} min={unixMin} max={unixMax} loop={unixLoop} {error.Message}");
    //        throw;
    //    }
    //}

}
