using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

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
                CandleTime futureCandles = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow.AddHours(1), 1);
                // Minimum synchronisation date (ignore candles below)
                CandleTime startFetchUnix = CandleTools.GetCandleFetchStart(symbol, symbolInterval.Interval, GlobalData.Clock.UtcNow);

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

        // Reset the previous collected trend data (once a day is preferred). Full reset (incl.
        // per-interval cached ZigZag indicators) because CandleList objects are about to be replaced
        // below, and a cached ZigZagResult.Candle would otherwise keep referencing a stale candle.
        symbol.Data.ResetTrendDataAndCaches();

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
    /// Scoped to the ACTIVE exchange — walks its exchange/quote folders plus the legacy
    /// Pivots folder, deleting leftover candle files. Other exchanges' folders are
    /// untouched. Rules per file class:
    ///   - exchange/quote/{base}.compressed (no interval suffix): orphan-check — only
    ///     remove when the symbol is dormant / below volume / delisted on this exchange.
    ///   - exchange/quote/{base}-{interval}.compressed: migrate to candles.db via
    ///     ReadCandlesFromDiskAsync when the active symbol still uses it; otherwise drop.
    ///   - exchange/quote/*.bin and extension-less files: legacy formats that predate
    ///     the .compressed era — always remove.
    ///   - Pivots/*.bin: legacy folder. For symbols the active exchange still uses,
    ///     migrate via ReadCandlesFromDiskAsync; everything else is dropped (symbols on
    ///     other exchanges count as orphan from this run's perspective).
    ///
    /// Designed to run AFTER <see cref="CandleDatabase.CleanCandlesAsync"/> on the hourly
    /// timer.
    /// </summary>
    public static async Task CleanOrphanCandleFilesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            GlobalData.AddTextToLogTab("Cleaning orphan candle files (please wait!)");

            // Scoped to the ACTIVE exchange only — other exchanges' candle DBs and
            // folders are out of scope for this run. The Pivots folder is exchange-
            // agnostic on disk but the symbol-lookup also stays inside the active
            // exchange (anything that does not live there counts as orphan from this
            // perspective).
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
            {
                GlobalData.AddTextToLogTab("Orphan cleanup: no active exchange — skipped");
                return;
            }

            int totalMigrated = 0;
            int totalDeleted = 0;

            string exchangeFolder = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());
            if (Directory.Exists(exchangeFolder))
            {
                GlobalData.AddTextToLogTab($"Orphan cleanup: scanning {exchange.Name}");

                foreach (string quoteFolder in Directory.GetDirectories(exchangeFolder))
                {
                    string quoteUpper = Path.GetFileName(quoteFolder).ToUpperInvariant();
                    int quoteMigrated = 0;
                    int quoteDeleted = 0;

                    // 1) .compressed — split rule by interval-suffix
                    foreach (string filePath in Directory.GetFiles(quoteFolder, "*.compressed"))
                    {
                        string stem = Path.GetFileNameWithoutExtension(filePath);

                        if (HasKnownIntervalSuffix(stem, out string baseWithoutSuffix))
                        {
                            // Zone-bulk file. If the symbol is still active, migrate the
                            // file to candles.db via ReadCandlesFromDiskAsync (which reads,
                            // upserts to DB and deletes the file). Otherwise drop it.
                            string symbolName = baseWithoutSuffix.ToUpperInvariant() + quoteUpper;
                            string intervalName = stem[(stem.LastIndexOf('-') + 1)..];

                            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol)
                                && !CandleDatabase.SymbolHasNoUse(symbol)
                                && GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                            {
                                if (await TryMigrateAsync(symbol, interval))
                                    quoteMigrated++;
                            }
                            else
                            {
                                if (TryDeleteFile(filePath))
                                    quoteDeleted++;
                            }
                        }
                        else
                        {
                            // Main DataStore file → only remove when symbol is no longer in use
                            string symbolName = stem.ToUpperInvariant() + quoteUpper;
                            bool orphan = !exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol)
                                          || CandleDatabase.SymbolHasNoUse(symbol);
                            if (orphan && TryDeleteFile(filePath))
                                quoteDeleted++;
                        }
                    }

                    // 2) .bin in exchange/quote — legacy format that no code currently
                    //    knows how to read from this location, so always remove.
                    foreach (string filePath in Directory.GetFiles(quoteFolder, "*.bin"))
                    {
                        if (TryDeleteFile(filePath))
                            quoteDeleted++;
                    }

                    // 3) Extension-less files in exchange/quote — oldest uncompressed
                    //    DataStore format. Active symbols were already migrated by
                    //    DataStore.LoadCandlesForSymbol at startup, so anything left here
                    //    is genuinely orphan. Always remove.
                    foreach (string filePath in Directory.EnumerateFiles(quoteFolder))
                    {
                        if (!Path.HasExtension(filePath) && TryDeleteFile(filePath))
                            quoteDeleted++;
                    }

                    if (quoteMigrated + quoteDeleted > 0)
                        GlobalData.AddTextToLogTab(
                            $"  {exchange.Name}/{quoteUpper}: migrated={quoteMigrated} deleted={quoteDeleted}");

                    totalMigrated += quoteMigrated;
                    totalDeleted += quoteDeleted;

                    TryRemoveEmptyDir(quoteFolder);
                }
            }

            // Legacy Pivots/ — exchange-agnostic filenames "{symbol.Name}-{interval}.bin".
            // For symbols that the ACTIVE exchange still uses, route through
            // ReadCandlesFromDiskAsync so the data is migrated to candles.db before the
            // file disappears. Anything else (symbols only on other exchanges, delisted,
            // or below thresholds) gets dropped — they are orphan from the active
            // exchange's perspective.
            string pivotsFolder = Path.Combine(GlobalData.AppDataFolder, "Pivots");
            if (Directory.Exists(pivotsFolder))
            {
                int pivotsMigrated = 0;
                int pivotsDeleted = 0;
                string[] pivotFiles = Directory.GetFiles(pivotsFolder, "*.bin");
                GlobalData.AddTextToLogTab($"Orphan cleanup: scanning Pivots/ ({pivotFiles.Length} files)");

                foreach (string filePath in pivotFiles)
                {
                    string stem = Path.GetFileNameWithoutExtension(filePath);
                    int dashIdx = stem.LastIndexOf('-');
                    if (dashIdx <= 0)
                    {
                        if (TryDeleteFile(filePath))
                            pivotsDeleted++;
                        continue;
                    }

                    string symbolName = stem[..dashIdx].ToUpperInvariant();
                    string intervalName = stem[(dashIdx + 1)..];

                    if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    {
                        if (TryDeleteFile(filePath))
                            pivotsDeleted++;
                        continue;
                    }

                    // Active-exchange-only lookup.
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? sym)
                        && !CandleDatabase.SymbolHasNoUse(sym))
                    {
                        if (await TryMigrateAsync(sym, interval))
                            pivotsMigrated++;
                    }
                    else
                    {
                        if (TryDeleteFile(filePath))
                            pivotsDeleted++;
                    }
                }

                GlobalData.AddTextToLogTab(
                    $"Orphan cleanup Pivots/: migrated={pivotsMigrated} deleted={pivotsDeleted}");
                totalMigrated += pivotsMigrated;
                totalDeleted += pivotsDeleted;

                TryRemoveEmptyDir(pivotsFolder);
            }

            GlobalData.AddTextToLogTab(
                $"Orphan cleanup {exchange.Name}: total migrated={totalMigrated} deleted={totalDeleted}");
        }
        finally
        {
            Semaphore.Release();
        }
    }


    /// <summary>
    /// Returns true when <paramref name="stem"/> ends with a known interval suffix
    /// (e.g. "btc-1h" → true, baseWithoutSuffix = "btc"). The check uses
    /// <see cref="GlobalData.IntervalListPeriodName"/> so only real interval names
    /// match (avoids treating "abc-xyz" as an interval).
    /// </summary>
    private static bool HasKnownIntervalSuffix(string stem, out string baseWithoutSuffix)
    {
        int dashIdx = stem.LastIndexOf('-');
        if (dashIdx > 0)
        {
            string maybeInterval = stem[(dashIdx + 1)..];
            if (GlobalData.IntervalListPeriodName.ContainsKey(maybeInterval))
            {
                baseWithoutSuffix = stem[..dashIdx];
                return true;
            }
        }
        baseWithoutSuffix = stem;
        return false;
    }


    /// <summary>
    /// Delete a file. Returns true on success so the caller can count it. Per-file success
    /// logging is intentionally omitted — with thousands of orphan files this would flood
    /// the log; the summary lines in <see cref="CleanOrphanCandleFilesAsync"/> are enough.
    /// Errors do go to the log so the user can investigate.
    /// </summary>
    private static bool TryDeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
            return true;
        }
        catch (Exception err)
        {
            ScannerLog.Logger.Error(err, "orphan file delete failed: " + filePath);
            GlobalData.AddTextToLogTab($"orphan file delete failed: {filePath}: {err.Message}");
            return false;
        }
    }


    /// <summary>
    /// Route the (symbol, interval) through <see cref="ZoneCandleEngine.ReadCandlesFromDiskAsync"/>:
    /// it reads the legacy file from its expected location, upserts every candle into the
    /// per-exchange candles.db and deletes the source file on success. Returns true when
    /// the call completed without exception. Failures are logged and leave the file in
    /// place for the next sweep to retry.
    /// </summary>
    private static async Task<bool> TryMigrateAsync(CryptoSymbol symbol, CryptoInterval interval)
    {
        try
        {
            await ZoneCandleEngine.ReadCandlesFromDiskAsync(symbol, interval);
            return true;
        }
        catch (Exception err)
        {
            ScannerLog.Logger.Error(err, $"orphan migration failed for {symbol.Name} {interval.Name}");
            GlobalData.AddTextToLogTab($"orphan migration failed for {symbol.Name} {interval.Name}: {err.Message}");
            return false;
        }
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
