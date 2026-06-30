using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

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
        bool fileWasRead = false;
        try
        {
            // an old uncompressed file
            if (File.Exists(oldFileName))
            {
                fileName = oldFileName;
                using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                using BinaryReader binaryReader = new(fileStream, Encoding.UTF8, false);
                await ReadCandlesFromStreamAsync(binaryReader, symbol, interval);
                fileWasRead = true;
            }
            // a new compressed file (preferred)
            else if (File.Exists(newFileName))
            {
                fileName = newFileName;
                using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                using LZ4DecoderStream lz4Stream = LZ4Stream.Decode(fileStream);
                using BinaryReader binaryReader = new(lz4Stream, Encoding.UTF8, false);
                await ReadCandlesFromStreamAsync(binaryReader, symbol, interval);
                fileWasRead = true;
            }
        }
        catch (Exception error)
        {
            GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} {error.Message}");
            File.Delete(fileName);
            throw;
        }

        if (symbol.Exchange == null)
            return;

        if (fileWasRead)
        {
            // Migration: candles for this (symbol, interval) are now in memory — push them
            // straight into the per-exchange candles.db and remove the source file. Mirrors
            // the per-symbol migration in DataStore.LoadCandlesForSymbol but for the per-interval
            // bulk files that ZoneCandleEngine owns. Crash-safety:
            //   - crash before SaveCandlesForSymbolInterval completes → file stays, retry next time
            //   - crash between save and File.Delete → file stays, re-saved (idempotent), deleted
            //   - DB write fails → file stays for the next attempt
            // Once every interval's file is migrated this branch is a no-op on every future call.
            try
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                using var candleDb = new Context.CandleDatabase(symbol.Exchange);
                candleDb.Open();
                Context.CandleDatabase.SaveCandlesForSymbolInterval(candleDb.Connection, symbol, symbolInterval);

                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
            catch (Exception migrError)
            {
                ScannerLog.Logger.Error(migrError, $"zone-candle migration to candles.db failed for {symbol.Name} {interval.Name}");
                GlobalData.AddTextToLogTab($"zone-candle migration to candles.db failed for {symbol.Name} {interval.Name}: {migrError.Message}");
                // Leave the file in place so the next read attempt retries.
            }
        }
        else
        {
            // No file (any more) → after migration the bulk lives in candles.db. Materialise
            // the full series for this (symbol, interval) into the in-memory CandleList so
            // zone-zoom logic has access to the same data it used to get from disk.
            // TryAdd in LoadCandlesForSymbolInterval guards against duplicates from the
            // bounded startup load.
            try
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                using var candleDb = new Context.CandleDatabase(symbol.Exchange);
                candleDb.Open();
                Context.CandleDatabase.LoadCandlesForSymbolInterval(candleDb.Connection, symbol, symbolInterval);
            }
            catch (Exception dbError)
            {
                ScannerLog.Logger.Error(dbError, $"candles.db read failed for {symbol.Name} {interval.Name}");
                GlobalData.AddTextToLogTab($"candles.db read failed for {symbol.Name} {interval.Name}: {dbError.Message}");
            }
        }
    }

    /*
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
    */


    public static async Task SaveCandleDataToDiskAsync(CryptoSymbol symbol, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        // Snapshot which intervals were touched so the DB work can run off-thread without
        // enumerating the shared loadedCandlesInMemory dictionary from inside Task.Run.
        List<CryptoSymbolInterval> changedIntervals = [];
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            if (loadedCandlesInMemory.TryGetValue(symbolInterval.IntervalPeriod, out bool changed) && changed)
                changedIntervals.Add(symbolInterval);
        }
        if (changedIntervals.Count == 0)
            return;

        if (symbol.Exchange == null)
            return;

        // Persist the changed intervals to the per-exchange candles.db. One shared DB
        // connection per call amortises the open/close + PRAGMA cost over multiple intervals.
        // Per-interval try/catch so one failing interval doesn't lose the "changed" flag on
        // the others — failed intervals stay marked as changed and will retry on the next save.
        // The legacy WriteCandlesToFileAsync is kept around (private) as a backup path but
        // is no longer called from here; the database is now the authoritative store.
        await Task.Run(() =>
        {
            using var candleDb = new Context.CandleDatabase(symbol.Exchange);
            candleDb.Open();

            foreach (var symbolInterval in changedIntervals)
            {
                try
                {
                    Context.CandleDatabase.SaveCandlesForSymbolInterval(candleDb.Connection, symbol, symbolInterval);
                    loadedCandlesInMemory[symbolInterval.IntervalPeriod] = false; // in memory, nothing changed
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, $"candles.db write failed for {symbol.Name} {symbolInterval.Interval.Name}");
                    GlobalData.AddTextToLogTab($"candles.db write failed for {symbol.Name} {symbolInterval.Interval.Name}: {error.Message}");
                    // Leave loadedCandlesInMemory[...] = true so the next save retries.
                }
            }
        });
    }


    /// <summary>
    /// Remove the no-longer-needed candles from the front of the list (everything older than
    /// <see cref="CandleTools.GetCandleFetchStart"/>).
    /// CandleList is a <see cref="CryptoCandleList"/> (a <see cref="System.Collections.Generic.SortedDictionary{TKey,TValue}"/>
    /// under the hood), so <c>Remove(key)</c> is O(log n) regardless of how many stale candles there
    /// are — removing them one by one is cheap even when there are tens of thousands (e.g. after the
    /// chart window loads a much larger history than the zone-calculation window needs). The previous
    /// implementation instead copied every *surviving* candle into a brand new list whenever the first
    /// key was stale — correct, but it touched the whole kept window on every call just to drop a
    /// handful of old entries at the front. Mirrors the same one-by-one removal CandleTools.CleanCandleDataAsync
    /// already uses for the live/signal candle window.
    /// </summary>
    public static async Task CleanLoadedCandlesAsync(CryptoSymbol symbol)
    {
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                if (symbolInterval.CandleList.Count == 0)
                    continue;

                CandleTime startFetchUnix = CandleTools.GetCandleFetchStart(symbol, symbolInterval.Interval, GlobalData.Clock.UtcNow);

                while (symbolInterval.CandleList.Count > 0)
                {
                    CandleTime openTime = symbolInterval.CandleList.Keys.First();
                    if (openTime >= startFetchUnix)
                        break;
                    symbolInterval.CandleList.Remove(openTime);
                }
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

        CandleTime unixNowTime = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 0);
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

        (CandleTime min, CandleTime max) = CalculateDates(interval, fetchFrom, fetchCount);
        (CandleTime loop, bool dataAllLocal) = IsDataLocal(min, max, symbol, interval);
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
                            $"{loop.ToDateTime()} .. {max.ToDateTime()}");

                    bool result = await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval, loop, max);
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
                    CandleTime nowTime = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 0);
                    nowTime = IntervalTools.StartOfIntervalCandle(nowTime, lowerInterval.Duration);
                    CandleTools.BulkCalculateCandles(symbol, lowerInterval, interval, nowTime);
                    loadedCandlesInMemory[interval.IntervalPeriod] = true;
                }
            }
        }
        catch (Exception error)
        {
            // some stupid error i need to trace..
            GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} from={fetchFrom} count={fetchCount} min={min} max={max} loop={loop} {error.Message}");
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

    //    (long min, long max) = CalculateDates(interval, fetchFrom, fetchCount);
    //    (long loop, bool dataAllLocal) = IsDataLocal(min, max, symbol, interval);
    //    if (dataAllLocal)
    //        return false;
    //    try
    //    {
    //        //if (debug)
    //        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, {fetchCount}, " +
    //        //        $"{CandleTools.GetUnixDate(min)}, {CandleTools.GetUnixDate(max)}");

    //        bool debug = GlobalData.Settings.General.DebugZoneCandles && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == "");
    //        if (debug)
    //            GlobalData.AddTextToLogTab($"CandleEngine.FetchFrom({symbol.Name}, {interval!.Name}, " +
    //                $"{CandleTools.GetUnixDate(loop)} .. {CandleTools.GetUnixDate(max)}");

    //        bool result = await symbol.Exchange.GetApiInstance().Candle.FetchFrom(symbol, interval, loop, max);

    //        //// check!!!
    //        //(loop, dataAllLocal) = IsDataLocal(min, max, symbol, interval);
    //        //if (!dataAllLocal && debug)
    //        //    GlobalData.AddTextToLogTab($"Fetch historical data FetchFrom({symbol.Name}, {interval!.Name}, {fetchCount}, " +
    //        //        $"{CandleTools.GetUnixDate(min)}, {CandleTools.GetUnixDate(max)} not everything local!");

    //        return result;
    //    }
    //    catch (Exception error)
    //    {
    //        // some stupid error i need to trace..
    //        GlobalData.AddTextToLogTab($"ERROR FetchFrom {symbol.Name} {interval.Name} from={fetchFrom} count={fetchCount} min={min} max={max} loop={loop} {error.Message}");
    //        throw;
    //    }
    //}

}
