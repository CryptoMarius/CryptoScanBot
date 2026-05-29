using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Context;

/// <summary>
/// Standalone SQLite store for candles, one DB file per exchange. Lives in the exchange's
/// own folder ({AppDataFolder}/{exchange-name}/candles.db) so size and growth can be
/// inspected per exchange. This is a first version intended to observe what storing candles
/// in SQLite costs / looks like — the existing file storage (DataStore .compressed files +
/// ZoneCandleEngine per-interval files) stays in charge of the primary read path. Writes
/// happen in parallel from DataStore.SaveCandlesAsync; reads are available via
/// <see cref="LoadCandlesForSymbol"/> for parallel verification / comparison.
///
/// Schema mirrors the in-memory CryptoCandle layout in raw-ticks form (identical to what
/// SaveVersion3 writes to the binary file), so decimals can be recomputed on read using
/// the Ticks column. No compression on this first version.
///
/// PRAGMAs at create-time:
///   - auto_vacuum = INCREMENTAL  → reclaim space after deletes without a full VACUUM
///   - journal_mode = WAL         → readers don't block the writer
///   - synchronous = NORMAL       → good performance/durability trade-off for a cache-like DB
///   - page_size = 8192           → fits typical batches of inserts more efficiently
/// </summary>
public class CandleDatabase : IDisposable
{
    private const string CandleDbFileName = "candles.db";

    // Prevent multiple save/load/clean sessions from running concurrently against the same DB.
    // SQLite serializes writers on the file lock anyway, so a single in-process gate is plenty.
    private static readonly SemaphoreSlim Semaphore = new(1);

    public SqliteConnection Connection { get; private set; }



    /// <summary>
    /// Open (or create) the candle DB for the given exchange. The DB file lives at
    /// {AppDataFolder}/{exchange.Name.ToLower()}/candles.db. The exchange folder is
    /// created if missing so the SQLite file can be written.
    /// </summary>
    public CandleDatabase(Model.CryptoExchange exchange)
    {
        string folder = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());
        Directory.CreateDirectory(folder);
        string dbFile = Path.Combine(folder, CandleDbFileName);
        Connection = new SqliteConnection($"Filename={dbFile};Mode=ReadWriteCreate;");
    }

    public void Open()
    {
        Connection.Open();

        // Per-connection PRAGMAs. journal_mode = WAL is actually file-level (persists)
        // but cheap to re-assert; synchronous and busy_timeout MUST be set per connection.
        // busy_timeout: wait up to 5s for the write-lock instead of immediately failing
        // with SQLITE_BUSY. Our DataStore writer already serializes via SemaphoreSlim;
        // this catches edge cases (WAL checkpoint collisions, an external tool like
        // DB Browser briefly holding a lock, etc.).
        Connection.Execute("PRAGMA journal_mode = WAL;");
        Connection.Execute("PRAGMA synchronous = NORMAL;");
        Connection.Execute("PRAGMA busy_timeout = 5000;");
    }

    public void Close() => Connection.Close();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Connection != null)
        {
            Connection.Close();
            Connection.Dispose();
        }
    }

    /// <summary>
    /// Initialize the candle database for one exchange:
    ///   - Apply once-only PRAGMAs (page_size, auto_vacuum) BEFORE any tables exist
    ///   - Apply per-connection PRAGMAs (journal_mode, synchronous)
    ///   - Create the Candle table + composite primary key (IF NOT EXISTS for concurrency safety)
    /// Idempotent — safe to call on every save pass.
    /// </summary>
    public static void InitializeSchema(Model.CryptoExchange exchange)
    {
        using var db = new CandleDatabase(exchange);
        db.Open(); // applies journal_mode / synchronous / busy_timeout

        // page_size and auto_vacuum must be set BEFORE any table is created to take effect.
        // For a populated DB they are silently no-ops, which is fine — we never change them.
        db.Connection.Execute("PRAGMA page_size = 8192;");
        db.Connection.Execute("PRAGMA auto_vacuum = INCREMENTAL;");

        // IF NOT EXISTS avoids races when InitializeSchema runs concurrently per exchange.
        // Composite primary key on (SymbolId, IntervalId, OpenTime) — natural lookup key,
        // also acts as the upsert conflict target. WITHOUT ROWID makes the table itself a
        // clustered index on that key, saving space and speeding up range scans.
        db.Connection.Execute(
            "CREATE TABLE IF NOT EXISTS [Candle] (" +
            "  SymbolId    INTEGER NOT NULL," +
            "  IntervalId  INTEGER NOT NULL," +
            "  OpenTime    INTEGER NOT NULL," + // CandleTime.Minutes (uint cast to long for SQLite)
            "  Ticks       INTEGER NOT NULL," + // number of decimals in tick size; decimal = Open * 10^-Ticks
            "  Open        INTEGER NOT NULL," +
            "  High        INTEGER NOT NULL," +
            "  Low         INTEGER NOT NULL," +
            "  Close       INTEGER NOT NULL," +
            "  Volume      REAL NOT NULL," +
            "  PRIMARY KEY (SymbolId, IntervalId, OpenTime)" +
            ") WITHOUT ROWID");
    }


    /// <summary>
    /// Read every candle for the given symbol from the candle DB and populate the in-memory
    /// CandleList per interval. The PriceDecimals on the symbol is preserved as TickDecimals
    /// on each candle (taken from the Ticks column so it matches what was written). LastCandle
    /// per interval is updated to the highest OpenTime found.
    ///
    /// Mirrors DataStore.LoadCandlesForSymbol in behaviour but reads from SQLite instead of
    /// the .compressed file. Intended for parallel verification — does NOT touch
    /// LastCandleSynchronized (the DB has no separate column for that yet).
    /// </summary>
    public static void LoadCandlesForSymbol(SqliteConnection connection, CryptoSymbol symbol)
    {
        // Reset the previous collected trend data (once a day is preferred)
        symbol.Data.ResetTrendData();

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT IntervalId, OpenTime, Ticks, Open, High, Low, Close, Volume " +
            "FROM Candle WHERE SymbolId = $SymbolId " +
            "ORDER BY IntervalId, OpenTime";
        var pSymbol = cmd.CreateParameter();
        pSymbol.ParameterName = "$SymbolId";
        pSymbol.Value = symbol.Id;
        cmd.Parameters.Add(pSymbol);

        // Index intervals by Id so we can route candles to the right CryptoSymbolInterval
        // without an O(N) lookup per row.
        Dictionary<int, CryptoSymbolInterval> intervalsId = [];
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            intervalsId[symbolInterval.Interval.Id] = symbolInterval;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int intervalId = reader.GetInt32(0);
            if (!intervalsId.TryGetValue(intervalId, out CryptoSymbolInterval? symbolInterval))
                continue; // interval not configured locally — skip silently

            uint openTimeMinutes = (uint)reader.GetInt64(1);
            byte ticks = (byte)reader.GetInt32(2);
            decimal tickSize = TickSizeFor(ticks);

            CryptoCandle candle = new()
            {
                OpenTime = new CandleTime(openTimeMinutes),
                TickDecimals = ticks,
                // Setting Open/High/Low/Close via decimal accessors round-trips through the
                // tick reconstruction, identical to what LoadVersion3 does for the file path.
                Open = reader.GetInt64(3) * tickSize,
                High = reader.GetInt64(4) * tickSize,
                Low = reader.GetInt64(5) * tickSize,
                Close = reader.GetInt64(6) * tickSize,
                Volume = (decimal)reader.GetDouble(7),
            };

            symbolInterval.CandleList.Lock();
            try
            {
                symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
                if (symbolInterval.LastCandle.OpenTime == 0 || candle.OpenTime >= symbolInterval.LastCandle.OpenTime)
                    symbolInterval.LastCandle = candle;
            }
            finally
            {
                symbolInterval.CandleList.Unlock();
            }
        }
    }


    /// <summary>
    /// Load route that reads candles from the per-exchange candles.db (SQLite) instead of
    /// the per-symbol .compressed file. Independent of DataStore.LoadCandlesAsync — both
    /// can be called, the second one wins because TryAdd silently skips existing keys.
    ///
    /// Intended for observation / comparison: lets you verify that the SQLite store has
    /// the same candle content as the file store. Reads only for the active exchange.
    ///
    /// Sequential on purpose: one shared connection per exchange amortises the open/close +
    /// PRAGMA cost. SQLite reads under WAL are concurrency-safe, but the open-overhead per
    /// symbol dominated when we had Parallel.ForEach with per-worker connections.
    /// </summary>
    public static async Task LoadCandlesAsync()
    {
        GlobalData.AddTextToLogTab("Loading candle information from candles.db (please wait!)");

        await Semaphore.WaitAsync();
        try
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
                return;

            InitializeSchema(exchange);

            using var db = new CandleDatabase(exchange);
            db.Open();

            // Snapshot to avoid enumerating a live collection while reading
            var symbols = exchange.SymbolListName.Values.ToList();

            foreach (var symbol in symbols)
            {
                if (!symbol.QuoteData.FetchCandles || symbol.Status != 1)
                    continue;

                // Honour the same minimal-volume gating as the file loader.
                if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
                {
                    if (symbol.ClearCandles())
                        ScannerLog.Logger.Trace($"Cleared candles for {symbol.Name}");
                    continue;
                }

                try
                {
                    LoadCandlesForSymbol(db.Connection, symbol);
                }
                catch (Exception sqliteError)
                {
                    ScannerLog.Logger.Error(sqliteError, "candles.db read failed for " + symbol.Name);
                    GlobalData.AddTextToLogTab($"candles.db read failed for {symbol.Name}: {sqliteError.Message}");
                }
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Upsert all candles for one symbol's intervals into the candle DB. Uses INSERT OR REPLACE
    /// inside a single transaction with a prepared statement for performance. Reads CryptoCandle
    /// raw ticks via the public Open/High/Low/Close decimal accessors — TickDecimals is preserved
    /// so the conversion is lossless.
    /// </summary>
    public static void SaveCandlesForSymbol(SqliteConnection connection, CryptoSymbol symbol)
    {
        using var tx = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "INSERT OR REPLACE INTO Candle " +
            "  (SymbolId, IntervalId, OpenTime, Ticks, Open, High, Low, Close, Volume) " +
            "VALUES " +
            "  ($SymbolId, $IntervalId, $OpenTime, $Ticks, $Open, $High, $Low, $Close, $Volume)";

        var pSymbol = cmd.CreateParameter(); pSymbol.ParameterName = "$SymbolId"; cmd.Parameters.Add(pSymbol);
        var pInterval = cmd.CreateParameter(); pInterval.ParameterName = "$IntervalId"; cmd.Parameters.Add(pInterval);
        var pOpenTime = cmd.CreateParameter(); pOpenTime.ParameterName = "$OpenTime"; cmd.Parameters.Add(pOpenTime);
        var pTickDecimals = cmd.CreateParameter(); pTickDecimals.ParameterName = "$Ticks"; cmd.Parameters.Add(pTickDecimals);
        var pOpen = cmd.CreateParameter(); pOpen.ParameterName = "$Open"; cmd.Parameters.Add(pOpen);
        var pHigh = cmd.CreateParameter(); pHigh.ParameterName = "$High"; cmd.Parameters.Add(pHigh);
        var pLow = cmd.CreateParameter(); pLow.ParameterName = "$Low"; cmd.Parameters.Add(pLow);
        var pClose = cmd.CreateParameter(); pClose.ParameterName = "$Close"; cmd.Parameters.Add(pClose);
        var pVolume = cmd.CreateParameter(); pVolume.ParameterName = "$Volume"; cmd.Parameters.Add(pVolume);

        cmd.Prepare();

        pSymbol.Value = symbol.Id;

        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            //int intervalId = symbolInterval.Interval.Id;
            pInterval.Value = symbolInterval.Interval.Id;

            symbolInterval.CandleList.Lock();
            try
            {
                foreach (CryptoCandle candle in symbolInterval.CandleList.Values)
                {
                    pOpenTime.Value = (long)candle.OpenTime.Minutes;
                    pTickDecimals.Value = candle.TickDecimals;
                    // Reconstruct raw ticks from the decimal accessors. TickDecimals lets us
                    // round-trip without losing precision; this matches what SaveVersion3 writes.
                    decimal tickSize = TickSizeFor(candle.TickDecimals);
                    pOpen.Value = (long)Math.Round(candle.Open / tickSize);
                    pHigh.Value = (long)Math.Round(candle.High / tickSize);
                    pLow.Value = (long)Math.Round(candle.Low / tickSize);
                    pClose.Value = (long)Math.Round(candle.Close / tickSize);
                    pVolume.Value = (double)candle.Volume;

                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                symbolInterval.CandleList.Unlock();
            }
        }

        tx.Commit();
    }



    /// <summary>
    /// Save route mirroring DataStore.SaveCandlesAsync but writing to the per-exchange
    /// candles.db instead of the per-symbol .compressed files. Sequential on purpose:
    /// SQLite serializes writers at the file level anyway, so per-symbol parallelism only
    /// added contention. One shared connection per exchange amortises the open/close cost.
    /// </summary>
    public static async Task SaveCandlesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            GlobalData.AddTextToLogTab("Saving candles.db (please wait!)");

            foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values.ToList())
            {
                InitializeSchema(exchange);

                using var db = new CandleDatabase(exchange);
                db.Open();

                // Snapshot to avoid enumerating a live collection while iterating
                var symbols = exchange.SymbolListName.Values.ToList();

                foreach (var symbol in symbols)
                {
                    try
                    {
                        // Don't save candles for symbols below the minimal volume threshold
                        if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
                        {
                            symbol.ClearCandles();
                        }

                        SaveCandlesForSymbol(db.Connection, symbol);
                    }
                    catch (Exception sqliteError)
                    {
                        ScannerLog.Logger.Error(sqliteError, "candles.db write failed for " + symbol.Name);
                        GlobalData.AddTextToLogTab($"candles.db write failed for {symbol.Name}: {sqliteError.Message}");
                    }
                }
            }

            ScannerLog.Logger.Trace("candles.db saved");
        }
        finally
        {
            // Enable analysing
            GlobalData.SetCandleTimerEnable(true);

            Semaphore.Release();
        }
    }


    // -----------------------------------------------------------------------
    // Cleanup — experimental, EXISTING file-based code paths are untouched.
    //
    // Retention model: per (symbol, interval) we keep the UNION of
    //   1) the standard window  [GetCandleFetchStart(I), now]   (matches today's behaviour)
    //   2) per OPEN zone Z on this symbol the time-range  [Z.OpenTime, Z.OpenTime+Z.Duration]
    //      for the zone-interval itself AND every lower-duration interval (potential zoom source)
    // Broken / closed zones do not contribute — their recent candles are still kept via the
    // standard window, anything older simply falls out.
    //
    // When the symbol is no longer relevant (status off, fetching disabled, below volume
    // threshold and not trading and not a barometer symbol) ALL its candles are deleted.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns true when the symbol no longer needs its candles persisted. Mirrors the
    /// "delete the file" gating that DataStore.SaveCandlesAsync already applies.
    /// </summary>
    private static bool SymbolHasNoUse(CryptoSymbol symbol)
    {
        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0)
            return true;
        if (!symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
            return true;
        return false;
    }

    /// <summary>
    /// Build the keep-ranges per interval Id for one symbol — union of the standard window
    /// and every open-zone zoom window across DLZ + FVG.
    /// </summary>
    private static Dictionary<int, List<(CandleTime start, CandleTime end)>> ComputeKeepRanges(CryptoSymbol symbol)
    {
        Dictionary<int, List<(CandleTime, CandleTime)>> result = [];
        CandleTime now = CandleTime.AlignFromDateTime(DateTime.UtcNow, 1);

        // 1) Standard window per interval (today's GetCandleFetchStart bound)
        foreach (var si in symbol.Data.SymbolIntervalList)
        {
            CandleTime start = CandleTools.GetCandleFetchStart(symbol, si.Interval, DateTime.UtcNow);
            AddRange(result, si.Interval.Id, start, now);
        }

        // 2) Per open zone — keep its time-range on the zone interval itself + all
        //    intervals with smaller duration (those are the candles DLZ might zoom into).
        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            AddOpenZoneRanges(symbolInterval.DlzZones.LongOpen, symbol, result);
            AddOpenZoneRanges(symbolInterval.DlzZones.ShortOpen, symbol, result);
            AddOpenZoneRanges(symbolInterval.FvgZones.LongOpen, symbol, result);
            AddOpenZoneRanges(symbolInterval.FvgZones.ShortOpen, symbol, result);
        }

        return result;
    }

    private static void AddOpenZoneRanges(IEnumerable<CryptoZone> zones, CryptoSymbol symbol,
        Dictionary<int, List<(CandleTime, CandleTime)>> result)
    {
        foreach (var zone in zones)
        {
            if (zone.CloseTime != null)
                continue;

            CandleTime zoneStart = zone.OpenTime;
            CandleTime zoneEnd = zone.OpenTime + zone.Interval.Duration;

            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                if (symbolInterval.Interval.Duration <= zone.Interval.Duration)
                    AddRange(result, symbolInterval.Interval.Id, zoneStart, zoneEnd);
            }
        }
    }

    private static void AddRange(Dictionary<int, List<(CandleTime, CandleTime)>> result,
        int intervalId, CandleTime start, CandleTime end)
    {
        if (!result.TryGetValue(intervalId, out var list))
        {
            list = [];
            result[intervalId] = list;
        }
        list.Add((start, end));
    }

    /// <summary>
    /// Cleanup for one symbol in one transaction. Either deletes ALL candles (symbol no
    /// longer in use) or deletes everything outside the computed keep-ranges per interval.
    /// </summary>
    public static void CleanCandlesForSymbol(SqliteConnection connection, CryptoSymbol symbol)
    {
        using var tx = connection.BeginTransaction();

        if (SymbolHasNoUse(symbol))
        {
            connection.Execute("DELETE FROM Candle WHERE SymbolId = @SymbolId",
                new { SymbolId = symbol.Id }, transaction: tx);
            tx.Commit();
            return;
        }

        var keepRanges = ComputeKeepRanges(symbol);

        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            int intervalId = symbolInterval.Interval.Id;

            if (!keepRanges.TryGetValue(intervalId, out var ranges) || ranges.Count == 0)
            {
                // No keep-range for this interval → drop everything for it
                connection.Execute(
                    "DELETE FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @IntervalId",
                    new { SymbolId = symbol.Id, IntervalId = intervalId }, transaction: tx);
                continue;
            }

            // Build:  DELETE ... WHERE NOT ( (OpenTime BETWEEN @s0 AND @e0) OR (... @s1 ...) ... )
            System.Text.StringBuilder sql = new(
                "DELETE FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @IntervalId AND NOT (");
            var p = new DynamicParameters();
            p.Add("@SymbolId", symbol.Id);
            p.Add("@IntervalId", intervalId);
            for (int i = 0; i < ranges.Count; i++)
            {
                if (i > 0) sql.Append(" OR ");
                sql.Append($"(OpenTime BETWEEN @s{i} AND @e{i})");
                p.Add($"@s{i}", (long)ranges[i].start.Minutes);
                p.Add($"@e{i}", (long)ranges[i].end.Minutes);
            }
            sql.Append(')');

            connection.Execute(sql.ToString(), p, transaction: tx);
        }

        tx.Commit();
    }

    /// <summary>
    /// Loop all symbols for one exchange, run <see cref="CleanCandlesForSymbol"/> per symbol,
    /// then reclaim freed pages with PRAGMA incremental_vacuum.
    /// Sequential on purpose — cleanup is not time-critical and SQLite serializes writes anyway.
    /// </summary>
    public static void CleanCandlesForExchange(Model.CryptoExchange exchange)
    {
        InitializeSchema(exchange);

        using var db = new CandleDatabase(exchange);
        db.Open();

        foreach (var symbol in exchange.SymbolListName.Values.ToList())
        {
            try
            {
                CleanCandlesForSymbol(db.Connection, symbol);
            }
            catch (Exception err)
            {
                ScannerLog.Logger.Error(err, "candles.db cleanup failed for " + symbol.Name);
                GlobalData.AddTextToLogTab($"candles.db cleanup failed for {symbol.Name}: {err.Message}");
            }
        }

        // Reclaim pages freed by the DELETEs above. INCREMENTAL keeps it cheap;
        // pass a generous page-budget so a large cleanup completes in one call.
        db.Connection.Execute("PRAGMA incremental_vacuum(10000);");
    }



    /// <summary>
    /// Clean stale candles in the per-exchange candles.db (SQLite). Experimental — leaves
    /// the old file-based cleanup paths fully intact. Per symbol the routine deletes either
    /// everything (symbol no longer in use) or everything outside the union of
    ///   - the standard window per interval (matches GetCandleFetchStart)
    ///   - per open zone the time-range needed for that zone's zoom-coverage
    /// See <see cref="CandleDatabase.CleanCandlesForSymbol"/> for details.
    /// </summary>
    public static async Task CleanCandlesAsync()
    {
        GlobalData.AddTextToLogTab("Cleaning candles.db (please wait!)");

        await Semaphore.WaitAsync();
        try
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
                return;

            CleanCandlesForExchange(exchange);
        }
        finally
        {
            Semaphore.Release();
        }
    }


    /// <summary>
    /// Lookup decimal tick size for a given number of decimals. Mirrors
    /// CryptoCandle.TickSizeLookup so we don't depend on its private array.
    /// </summary>
    private static decimal TickSizeFor(byte decimals)
    {
        if (decimals < PrecomputedTickSizes.Length)
            return PrecomputedTickSizes[decimals];
        return 1m / (decimal)Math.Pow(10, decimals);
    }

    private static readonly decimal[] PrecomputedTickSizes =
    [
        1.0m, 0.1m, 0.01m, 0.001m, 0.0001m,
        0.00001m, 0.000001m, 0.0000001m, 0.00000001m, 0.000000001m,
    ];
}
