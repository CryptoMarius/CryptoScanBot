using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;

using System.Text;

namespace CryptoScanner.Core.Context;

// <summary>
// https://stackoverflow.com/questions/64799591/is-there-a-high-performance-way-to-replace-the-binaryformatter-in-net5
// </summary>

// version:
// 1: symbolname, [interval<1m .. 1d>, synched<int64>, count, ohlcv <decimal, old style>]
// 2: [marker, interval<1m .. 1w>, synched<uint32>, count, <TickDecimals+ticks>ohlcv] (LZ4)

public class DataStore
{
    private const int markerValue = 1234567890;

    // Prevent multiple save sessions
    private static readonly SemaphoreSlim Semaphore = new(1);

    private static readonly ParallelOptions ParallelOptions = new()
    {
        MaxDegreeOfParallelism = Math.Min(8, Environment.ProcessorCount)
    };

    private static void ReadCandlesFromStream(BinaryReader reader, CryptoSymbol symbol)
    {
        int version = reader.ReadInt32();

        if (version == 1)
        {
            // Name of symbol, removed in version 2
            reader.ReadString();
        }

        if (version >= 1 && version <= 2)
        {
            foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            {
                // The weekly interval was introduced in version 2 of the storage
                if (version == 1 && symbolInterval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                    continue;

                // "Synchronisation" marker (new in version 4)
                if (version != 1)
                {
                    int marker = reader.ReadInt32();
                    if (marker != markerValue)
                        throw new Exception($"file {symbol.Name} is corrupted");
                }

                // Interval enum value
                CryptoIntervalPeriod intervalPeriod = (CryptoIntervalPeriod)reader.ReadInt32();
                if (intervalPeriod != symbolInterval.IntervalPeriod)
                    throw new Exception($"file {symbol.Name} is corrupted (interval {intervalPeriod} does not match)");

                // 3: Last candle Last synchronised date with the exchange
                if (version == 1)
                {
                    long unix = reader.ReadInt64();
                    if (unix == 0)
                        symbolInterval.LastCandleSynchronized = null;
                    else
                        symbolInterval.LastCandleSynchronized = CandleTime.FromUnixSeconds(unix);
                    if (symbolInterval.LastCandleSynchronized == CandleTime.MinValue)
                        symbolInterval.LastCandleSynchronized = null;
                }
                else
                {
                    uint unix = reader.ReadUInt32();
                    if (unix == CandleTime.MinValue)
                        symbolInterval.LastCandleSynchronized = null;
                    else
                        symbolInterval.LastCandleSynchronized = new(unix);
                    if (symbolInterval.LastCandleSynchronized == CandleTime.MinValue)
                        symbolInterval.LastCandleSynchronized = null;
                }

                // max candle date
                // For some reason we can have corrupted candles in the system.
                // This killed the scanner because it had a loop until maxLong!
                CandleTime futureCandles = CandleTime.AlignFromDateTime(DateTime.UtcNow.AddHours(1), 1);
                // Minimum synchronisation date (ignore candles below)
                CandleTime startFetchUnix = CandleTools.GetCandleFetchStart(symbol, symbolInterval.Interval, DateTime.UtcNow);

                // 4: Candle count
                int candleCount = reader.ReadInt32();

                symbolInterval.CandleList.Lock();
                try
                {
                    // 5: The OHLCV values
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
                            // Delegates to the newer candle storage system
                            candle.LoadVersion3(reader);
                        }

                        // We had some data corruption and 1 candle in the year 2150...
                        // It is not a nice solution, but skip those candles (really weird)
                        if (candle.OpenTime >= startFetchUnix)
                        {
                            if (candle.OpenTime < futureCandles)
                            {
                                symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
                                if (symbolInterval.LastCandle.OpenTime == 0 || candle.OpenTime >= symbolInterval.LastCandle.OpenTime)
                                    symbolInterval.LastCandle = candle;
                            }
                            else
                                GlobalData.AddTextToLogTab($"{symbol.Name} skipped corrupted candle {candle.OpenTime}");
                        }

                        candleCount--;
                    }
                }
                finally
                {
                    symbolInterval.CandleList.Unlock();
                }
            }
        }
    }

    private static void LoadCandlesForSymbol(string exchangeStoragePath, CryptoSymbol symbol)
    {
        symbol.LastPrice = null;
        string oldFileName = Path.Combine(exchangeStoragePath, symbol.Quote.ToLower(), symbol.Base.ToLower());
        string newFileName = Path.ChangeExtension(oldFileName, ".compressed");

        // Reset the previous collected trend data (once a day is preferred)
        symbol.Data.ResetTrendData();

        string fileName = string.Empty;
        bool fileWasRead = false;
        try
        {
            // an old uncompressed file
            if (File.Exists(oldFileName))
            {
                // Ancient format: uncompressed
                fileName = oldFileName;
                using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                using BinaryReader binaryReader = new(fileStream, Encoding.UTF8, false);
                ReadCandlesFromStream(binaryReader, symbol);
                fileWasRead = true;
            }
            else if (File.Exists(newFileName))
            {
                // New lz4 compressed file (only 2.5.x)
                fileName = newFileName;
                using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 2 * 1024 * 1024);
                using LZ4DecoderStream lz4Stream = LZ4Stream.Decode(fileStream);
                using BinaryReader binaryReader = new(lz4Stream, Encoding.UTF8, false);
                ReadCandlesFromStream(binaryReader, symbol);
                fileWasRead = true;
            }
        }
        catch (Exception error)
        {
            GlobalData.AddTextToLogTab("Problem " + symbol.Name);
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
            File.Delete(fileName);
        }

        // Migration: candles are now in memory — push them straight into the per-exchange
        // candles.db and remove the source file. Per-symbol, atomic enough to survive a crash:
        //   - crash before SaveCandlesForSymbol completes → file stays, next start retries
        //   - crash between SaveCandlesForSymbol and File.Delete → file stays, next start
        //     re-saves to DB (INSERT OR REPLACE is idempotent) and deletes the file
        //   - DB write fails → file stays for the next attempt
        // Once every symbol's file has been migrated, this branch becomes a no-op on every
        // future startup (nothing to read, nothing to migrate).
        if (fileWasRead && symbol.Exchange != null)
        {
            try
            {
                using var candleDb = new CandleDatabase(symbol.Exchange);
                candleDb.Open();
                CandleDatabase.SaveCandlesForSymbol(candleDb.Connection, symbol);
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
            catch (Exception migrError)
            {
                ScannerLog.Logger.Error(migrError, "candle migration to candles.db failed for " + symbol.Name);
                GlobalData.AddTextToLogTab($"candle migration to candles.db failed for {symbol.Name}: {migrError.Message}");
                // Leave the file in place so the next startup retries.
            }
        }
    }


    /// <summary>
    /// Only here because of migration from file to database candles.db
    /// </summary>
    /// <returns></returns>
    public static async Task LoadCandlesAsync()
    {
        GlobalData.AddTextToLogTab("Loading candle information (please wait!)");

        // Use the same semaphore as SaveCandlesAsync to prevent concurrent file access
        await Semaphore.WaitAsync();
        try
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange != null)
            {
                string folderName = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());

                // Ensure the candle DB schema exists once — the parallel workers below
                // open their own CandleDatabase per call during the per-symbol migration.
                CandleDatabase.InitializeSchema(exchange);

                // Snapshot to avoid enumerating a live collection in parallel
                var symbols = exchange.SymbolListName.Values.ToList();

                Parallel.ForEach(symbols, ParallelOptions, symbol =>
                {
                    if (!symbol.QuoteData.FetchCandles || symbol.Status != 1)
                        return;

                    // Don't load candles for symbols below the minimal volume threshold
                    if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
                    {
                        if (symbol.ClearCandles())
                            ScannerLog.Logger.Trace($"Cleared candles for {symbol.Name}");
                        return;
                    }

                    LoadCandlesForSymbol(folderName, symbol);
                });
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }


    /// <summary>
    /// Walk every exchange/quote folder and the legacy Pivots folder, deleting leftover
    /// candle files for symbols that are no longer active or no longer present (delisted).
    /// Covers DataStore main files ({base}.compressed), per-interval bulk files
    /// ({base}-{interval}.compressed) and the old uncompressed equivalents (.bin), plus
    /// the exchange-agnostic Pivots/ folder ({symbol.Name}-{interval}.bin).
    ///
    /// Designed to run AFTER <see cref="CandleDatabase.CleanCandlesAsync"/> on the hourly
    /// timer — by then the in-memory state is settled and we know which symbols are still
    /// valuable. Sequential per folder; file deletes are cheap and we don't want to hammer
    /// the filesystem with parallel deletes.
    /// </summary>
    public static async Task CleanOrphanCandleFilesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            GlobalData.AddTextToLogTab("Cleaning orphan candle files (please wait!)");

            // Per-exchange / per-quote sweep: filename is "{base}[-{interval}].(compressed|bin)".
            // Symbol lookup is local to this exchange, key = base.ToUpper() + quote.ToUpper().
            foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values.ToList())
            {
                string exchangeFolder = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());
                if (!Directory.Exists(exchangeFolder))
                    continue;

                foreach (string quoteFolder in Directory.GetDirectories(exchangeFolder))
                {
                    string quoteUpper = Path.GetFileName(quoteFolder).ToUpperInvariant();

                    bool IsOrphanInExchange(string stemWithoutInterval)
                    {
                        string symbolName = stemWithoutInterval.ToUpperInvariant() + quoteUpper;
                        return !exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol)
                               || CandleDatabase.SymbolHasNoUse(symbol);
                    }

                    SweepOrphanFiles(quoteFolder, "*.compressed", IsOrphanInExchange);
                    SweepOrphanFiles(quoteFolder, "*.bin", IsOrphanInExchange);
                    TryRemoveEmptyDir(quoteFolder);
                }
            }

            // Legacy Pivots/ sweep: filename is "{symbol.Name}[-{interval}].bin", exchange-
            // agnostic. Symbol counts as active when ANY exchange still has it as in-use.
            string pivotsFolder = Path.Combine(GlobalData.AppDataFolder, "Pivots");
            if (Directory.Exists(pivotsFolder))
            {
                bool IsOrphanAnyExchange(string stemWithoutInterval)
                {
                    string symbolName = stemWithoutInterval.ToUpperInvariant();
                    foreach (var exch in GlobalData.ExchangeListName.Values)
                    {
                        if (exch.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? sym)
                            && !CandleDatabase.SymbolHasNoUse(sym))
                            return false;
                    }
                    return true;
                }

                SweepOrphanFiles(pivotsFolder, "*.bin", IsOrphanAnyExchange);
                TryRemoveEmptyDir(pivotsFolder);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }


    /// <summary>
    /// Scan <paramref name="folder"/> for files matching <paramref name="searchPattern"/>,
    /// strip the optional "-{interval}" suffix from each filename stem, and ask
    /// <paramref name="isOrphan"/> whether the file belongs to a symbol that no longer
    /// needs it. Orphans are deleted; failures are logged but do not stop the sweep.
    /// </summary>
    private static void SweepOrphanFiles(string folder, string searchPattern, Func<string, bool> isOrphan)
    {
        foreach (string filePath in Directory.GetFiles(folder, searchPattern))
        {
            string stem = Path.GetFileNameWithoutExtension(filePath);
            string stemNoInterval = StripIntervalSuffix(stem);

            if (!isOrphan(stemNoInterval))
                continue;

            try
            {
                File.Delete(filePath);
            }
            catch (Exception err)
            {
                ScannerLog.Logger.Error(err, "orphan file delete failed: " + filePath);
                GlobalData.AddTextToLogTab($"orphan file delete failed: {filePath}: {err.Message}");
            }
        }
    }


    /// <summary>
    /// Remove the trailing "-{interval}" suffix from <paramref name="stem"/> when the suffix
    /// matches a known interval name (1m, 5m, 1h, …). Returns <paramref name="stem"/>
    /// unchanged when no such suffix is present (i.e. the file is a main {base}.compressed).
    /// </summary>
    private static string StripIntervalSuffix(string stem)
    {
        int dashIdx = stem.LastIndexOf('-');
        if (dashIdx > 0)
        {
            string maybeInterval = stem[(dashIdx + 1)..];
            if (GlobalData.IntervalListPeriodName.ContainsKey(maybeInterval))
                return stem[..dashIdx];
        }
        return stem;
    }


    /// <summary>
    /// Best-effort delete of an empty folder. Silently ignores any failure — keeping the
    /// folder around is harmless.
    /// </summary>
    private static void TryRemoveEmptyDir(string folder)
    {
        try
        {
            if (Directory.GetFileSystemEntries(folder).Length == 0)
                Directory.Delete(folder);
        }
        catch
        {
            // Silently ignore — directory delete is best-effort cleanup.
        }
    }


    /*
    /// <summary>
    /// No longer needed, for now private
    /// </summary>
    /// <returns></returns>
    private static async Task SaveCandlesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            GlobalData.AddTextToLogTab("Saving candle information (please wait!)");

            foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values.ToList())
            {
                string folderName = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());

                // Snapshot to avoid enumerating a live collection in parallel
                var symbols = exchange.SymbolListName.Values.ToList();

                await Parallel.ForEachAsync(symbols, ParallelOptions, async (symbol, cancellationToken) =>
                {
                    string quoteFolder = Path.Combine(folderName, symbol.Quote.ToLower());
                    try
                    {
                        // Delete any uncompressed file
                        string oldfileName = Path.Combine(quoteFolder, symbol.Base.ToLower());
                        if (File.Exists(oldfileName))
                            File.Delete(oldfileName);


                        string fileName = Path.ChangeExtension(oldfileName, ".compressed");

                        // Don't save candles for symbols below the minimal volume threshold
                        if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
                        {
                            symbol.ClearCandles();
                        }

                        long count = 0;
                        foreach (CryptoSymbolInterval cryptoSymbolInterval in symbol.Data.SymbolIntervalList)
                            count += cryptoSymbolInterval.CandleList.Count;

                        // Delete the file if there is no data
                        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0 || count == 0)
                        {
                            if (File.Exists(fileName))
                                File.Delete(fileName);
                            return;
                        }

                        Directory.CreateDirectory(quoteFolder);
                        ScannerLog.Logger.Trace($"Saving candle information for {symbol.Name} candle count={count}");

                        await symbol.Data.CandleLock.WaitAsync(cancellationToken);
                        try
                        {
                            using FileStream fileStream = new(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 2 * 1024 * 1024);
                            using LZ4EncoderStream lz4Stream = LZ4Stream.Encode(fileStream, LZ4Level.L00_FAST);
                            using BinaryWriter writer = new(lz4Stream, Encoding.UTF8, false);

                            int version = 2;
                            writer.Write(version);

                            foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
                            {
                                // 1: "Synchronisation" marker (new in version 4)
                                writer.Write(markerValue);

                                // 2: Interval enum value
                                writer.Write((int)symbolInterval.Interval.IntervalPeriod);

                                // 3: Last sync date with exchange
                                uint value = CandleTime.MinValue.Minutes;
                                if (symbolInterval.LastCandleSynchronized.HasValue)
                                    value = symbolInterval.LastCandleSynchronized.Value.Minutes;
                                writer.Write(value);

                                symbolInterval.CandleList.Lock();
                                try
                                {
                                    // 4: Candle count
                                    writer.Write(symbolInterval.CandleList.Count);

                                    // 5: OHLCV values
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
                        }
                        finally
                        {
                            symbol.Data.CandleLock.Release();
                        }
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab($"Problem {symbol.Name}");
                        GlobalData.AddTextToLogTab(error.ToString());
                    }
                });
            }

            ScannerLog.Logger.Trace("Candle information saved");
        }
        finally
        {
            // Enable analysing
            GlobalData.SetCandleTimerEnable(true);

            Semaphore.Release();
        }
    }
    */
}
