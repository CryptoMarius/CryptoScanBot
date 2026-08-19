using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Context;

/// <summary>
/// The candle database on disk does not match the schema this build expects. Its own type so
/// callers can distinguish "this file needs converting" from a genuine SQLite failure: loading
/// and saving then skip the candle store and say so, instead of aborting the whole startup.
/// </summary>
public class CandleDatabaseSchemaException(string message) : Exception(message)
{
}

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
    //private const string CandleDbFileName = "candles.db";

    // Prevent multiple save/load/clean sessions from running concurrently against the same DB.
    // SQLite serializes writers on the file lock anyway, so a single in-process gate is plenty.
    private static readonly SemaphoreSlim Semaphore = new(1);

    // Per-worker degree of parallelism. WAL-mode SQLite allows concurrent readers freely;
    // concurrent writers serialise on the file lock but queue politely via busy_timeout, so
    // parallel still pays off through overlapped connection-open + prepare + commit-fsync.
    private static readonly ParallelOptions ParallelOptions = new()
    {
        MaxDegreeOfParallelism = Math.Min(8, Environment.ProcessorCount)
    };

    public SqliteConnection Connection { get; private set; }

    private readonly string connectionString;



    /// <summary>
    /// Resolves the base folder for candle DB files: CandleDataFolder when set, otherwise AppDataFolder.
    /// </summary>
    public static string ResolveCandleFolder()
    {
        return string.IsNullOrWhiteSpace(GlobalData.CandleDataFolder)
            ? GlobalData.AppDataFolder
            : GlobalData.CandleDataFolder;
    }


    /// <summary>
    /// Open (or create) the candle DB for the given exchange. The DB file lives at
    /// {CandleDataFolder}/{exchange.Name}.db (or {AppDataFolder} when no separate candle folder
    /// is configured). The folder is created if missing so the SQLite file can be written.
    /// </summary>
    public CandleDatabase(Model.CryptoExchange exchange)
    {
        string baseFolder = ResolveCandleFolder();
        Directory.CreateDirectory(baseFolder);
        string dbFile = Path.Combine(baseFolder, exchange.Name + ".db");
        connectionString = $"Filename={dbFile};Mode=ReadWriteCreate;";
        Connection = new SqliteConnection(connectionString);
        //string folder = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());
        //Directory.CreateDirectory(folder);
        //string dbFile = Path.Combine(folder, CandleDbFileName);
        //Connection = new SqliteConnection($"Filename={dbFile};Mode=ReadWriteCreate;");
    }

    public void Open()
    {
        // See CryptoDatabase.Open: a handle leased from the Microsoft.Data.Sqlite pool can come back
        // already disposed under heavy open/close churn. Retry on a fresh connection; no data is at
        // risk because only the PRAGMAs below have run at that point.
        const int maxAttempts = 3;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                Connection.Open();

                // Per-connection PRAGMAs. journal_mode = WAL is actually file-level (persists)
                // but cheap to re-assert; synchronous and busy_timeout MUST be set per connection.
                // busy_timeout: wait up to 30s for the write-lock instead of immediately failing
                // with SQLITE_BUSY. SaveCandlesAsync already serialises writers via SemaphoreSlim;
                // 30s is a generous safety net for edge cases (WAL checkpoint collisions, an
                // external tool like DB Browser briefly holding a lock, exceptionally large
                // INSERT transactions for symbols with tens of thousands of candles).
                Connection.Execute("PRAGMA journal_mode = WAL;");
                Connection.Execute("PRAGMA synchronous = NORMAL;");
                Connection.Execute("PRAGMA busy_timeout = 30000;");
                return;
            }
            catch (ObjectDisposedException) when (attempt < maxAttempts)
            {
                try
                {
                    Connection.Dispose();
                }
                catch (Exception)
                {
                    // ignore, the handle is gone anyway
                }
                Connection = new SqliteConnection(connectionString);
            }
        }
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
    /// Schema version stored in the Meta table. Version 2 introduced the local Symbol
    /// registry: Candle.SymbolId no longer refers to CryptoScanBot.db's Symbol table but
    /// to this database's own Symbol table, keyed by symbol NAME. Version 1 databases
    /// (no Meta table) carry foreign ids and need an explicit migration.
    /// </summary>
    public const int CurrentSchemaVersion = 3;


    /// <summary>
    /// Initialize the candle database for one exchange:
    ///   - Apply once-only PRAGMAs (page_size, auto_vacuum) BEFORE any tables exist
    ///   - Apply per-connection PRAGMAs (journal_mode, synchronous)
    ///   - Create the Candle table + composite primary key (IF NOT EXISTS for concurrency safety)
    ///   - Create the local Symbol registry + Meta table and verify the schema version
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

        // Per (symbol, interval) sync-bookkeeping that the exchange fetcher uses to know
        // where to continue. Used to live as a uint32 per interval inside the .compressed
        // file header that DataStore wrote. Without persisting it here every restart would
        // refetch the full GetCandleFetchStart window from the exchange.
        //   LastSync = CandleTime.Minutes  →  null means "never synced"
        // Table name matches the in-memory CryptoSymbolInterval model.
        db.Connection.Execute(
            "CREATE TABLE IF NOT EXISTS [SymbolInterval] (" +
            "  SymbolId    INTEGER NOT NULL," +
            "  IntervalId  INTEGER NOT NULL," +
            "  LastSync    INTEGER NULL," +
            "  PRIMARY KEY (SymbolId, IntervalId)" +
            ") WITHOUT ROWID");

        // Local symbol registry. Candle.SymbolId / SymbolInterval.SymbolId point HERE, not at
        // the Symbol table in CryptoScanBot.db. That main database is regularly thrown away and
        // rebuilt (faster than cleaning it), and a symbol almost never comes back on the same
        // autoincrement id — which silently re-labelled every candle in this store. Keying on the
        // name instead makes this database self-describing and independent of that rebuild.
        //
        // The key is the exchange INSTRUMENT, not the scanner name (version 3). A scanner name does
        // not identify an instrument: Binance publishes BTCUSDT and BTCUSDT_261225, both with base
        // BTC and quote USDT, and Okx moved its futures from spot instruments to swap instruments.
        // Keyed on the instrument those simply get their own row, so their candles can never mix.
        // Name is kept alongside it for readability when querying the file by hand.
        //
        // The key is stored ONCE here rather than in every Candle row: Candle is WITHOUT ROWID
        // with the primary key clustered into each row, so a TEXT key would add its full length
        // to all (tens of millions of) rows. The local id keeps the rows byte-identical in size.
        //
        // Deliberately created in the VERSION 2 layout here. A version-1 file has no Symbol table at
        // all and is filled by CandleDatabaseMigration.ConvertInPlace, which writes (SymbolId, Name);
        // VerifySchemaVersion takes it to the version-3 layout right after. An existing table is left
        // alone by IF NOT EXISTS, so a file that is already version 3 keeps its own layout.
        db.Connection.Execute(
            "CREATE TABLE IF NOT EXISTS [Symbol] (" +
            "  SymbolId  INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  Name      TEXT NOT NULL UNIQUE" +
            ")");


        // Schema bookkeeping. Also holds the exchange name so a file copied into the wrong
        // folder cannot silently be read as a different exchange.
        db.Connection.Execute(
            "CREATE TABLE IF NOT EXISTS [Meta] (" +
            "  Key    TEXT NOT NULL PRIMARY KEY," +
            "  Value  TEXT NOT NULL" +
            ") WITHOUT ROWID");

        VerifySchemaVersion(db.Connection, exchange);
    }


    /// <summary>
    /// Establishes (or verifies) the schema version of an already-created database.
    /// <list type="bullet">
    ///   <item>No version recorded and no candles → brand new or emptied file, stamp it as
    ///         <see cref="CurrentSchemaVersion"/>. Nothing to convert.</item>
    ///   <item>No version recorded but candles present → a version-1 file whose SymbolIds still
    ///         refer to the main database. Converted right here by
    ///         <see cref="CandleDatabaseMigration.ConvertInPlace"/>: every application that opens a
    ///         candle store has to get past this point, so it cannot be left to a menu action that
    ///         only the Avalonia scanner has.</item>
    ///   <item>Version 2 recorded → keyed on the scanner name, converted to version 3 by
    ///         <see cref="MigrateToVersion3"/>.</item>
    ///   <item>Version recorded → must match, otherwise the code and the file disagree.</item>
    /// </list>
    /// </summary>
    private static void VerifySchemaVersion(SqliteConnection connection, Model.CryptoExchange exchange)
    {
        string? version = connection.QueryFirstOrDefault<string>(
            "SELECT Value FROM Meta WHERE Key = 'SchemaVersion'");

        if (string.IsNullOrEmpty(version))
        {
            long candleCount = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM (SELECT 1 FROM Candle LIMIT 1)");
            if (candleCount > 0)
            {
                // Throws (and leaves the file untouched) when there is nothing to judge the old
                // mapping against yet — the caller then skips the candle store for now.
                // Produces a version-2 file, which the step below then takes to version 3.
                CandleDatabaseMigration.ConvertInPlace(connection, exchange);
                version = "2";
            }
            else
            {
                // Brand new or emptied file. The table above was created in the version-2 layout for
                // the benefit of a version-1 conversion; with no candles there is nothing to keep, so
                // replace it with the version-3 layout instead of converting it afterwards.
                connection.Execute("DROP TABLE IF EXISTS [Symbol]");
                connection.Execute(
                    "CREATE TABLE [Symbol] (" +
                    "  SymbolId      INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "  ExchangeName  TEXT NOT NULL COLLATE NOCASE UNIQUE," +
                    "  Name          TEXT NULL" +
                    ")");

                connection.Execute(
                    "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('SchemaVersion', $Version)",
                    new { Version = CurrentSchemaVersion.ToString() });
                connection.Execute(
                    "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('ExchangeName', $Name)",
                    new { Name = exchange.Name });
                return;
            }
        }

        if (version == "2")
        {
            MigrateToVersion3(connection, exchange);
            version = CurrentSchemaVersion.ToString();
        }

        if (version != CurrentSchemaVersion.ToString())
        {
            throw new InvalidOperationException(
                $"Candle database for '{exchange.Name}' has schema version {version}, " +
                $"this build expects {CurrentSchemaVersion}.");
        }
    }


    /// <summary>
    /// Converts a version-2 file (local Symbol table keyed on the SCANNER name) to version 3, where
    /// it is keyed on the exchange INSTRUMENT. A scanner name does not identify an instrument: an
    /// exchange can publish a perpetual and a dated contract that both parse to "BTCUSDT", or move an
    /// instrument from spot to swap. Keying on the instrument makes mixing them impossible instead of
    /// something to detect afterwards.
    ///
    /// Every existing row is adopted by looking its name up in the current symbol list and recording
    /// that symbol's instrument. Two groups are NOT adopted, and lose their candles here:
    /// <list type="bullet">
    ///   <item>names the exchange covers with more than one instrument
    ///         (<see cref="CryptoExchangeData.AmbiguousSymbolNames"/>) — those candles cannot be
    ///         attributed to either instrument, so they are fetched again;</item>
    ///   <item>names the exchange no longer lists — orphans, which the normal cleanup never reached
    ///         because it iterates the live symbol list rather than the file.</item>
    /// </list>
    /// </summary>
    private static void MigrateToVersion3(SqliteConnection connection, Model.CryptoExchange exchange)
    {
        // Without the instruments every row would look like an orphan and the whole file would be
        // emptied. Refuse; the callers already treat this as "skip the candle store for now".
        if (exchange.SymbolListName.Count == 0)
        {
            throw new CandleDatabaseSchemaException(
                $"the symbol list of '{exchange.Name}' is not loaded yet, conversion to version " +
                $"{CurrentSchemaVersion} needs it to resolve the instruments");
        }

        using var tx = connection.BeginTransaction();

        connection.Execute(
            "CREATE TABLE IF NOT EXISTS [SymbolVersion3] (" +
            "  SymbolId      INTEGER PRIMARY KEY AUTOINCREMENT," +
            "  ExchangeName  TEXT NOT NULL COLLATE NOCASE UNIQUE," +
            "  Name          TEXT NULL" +
            ")", transaction: tx);

        List<LocalSymbolRow> rows = [.. connection.Query<LocalSymbolRow>(
            "SELECT SymbolId, Name FROM Symbol", transaction: tx)];

        int adopted = 0;
        int ambiguous = 0;
        int orphan = 0;
        foreach (LocalSymbolRow row in rows)
        {
            bool isAmbiguous = !string.IsNullOrEmpty(row.Name)
                && exchange.Data.AmbiguousSymbolNames.Contains(row.Name);

            if (!isAmbiguous && !string.IsNullOrEmpty(row.Name)
                && exchange.SymbolListName.TryGetValue(row.Name, out CryptoSymbol? symbol)
                && !string.IsNullOrEmpty(symbol.ExchangeName))
            {
                // Keep the SymbolId so none of the (millions of) Candle rows have to be rewritten
                connection.Execute(
                    "INSERT OR IGNORE INTO SymbolVersion3 (SymbolId, ExchangeName, Name) " +
                    "VALUES ($SymbolId, $ExchangeName, $Name)",
                    new { row.SymbolId, symbol.ExchangeName, row.Name }, transaction: tx);
                adopted++;
                continue;
            }

            if (isAmbiguous)
                ambiguous++;
            else
                orphan++;

            connection.Execute("DELETE FROM Candle WHERE SymbolId = $SymbolId",
                new { row.SymbolId }, transaction: tx);
            connection.Execute("DELETE FROM SymbolInterval WHERE SymbolId = $SymbolId",
                new { row.SymbolId }, transaction: tx);
        }

        connection.Execute("DROP TABLE [Symbol]", transaction: tx);
        connection.Execute("ALTER TABLE [SymbolVersion3] RENAME TO [Symbol]", transaction: tx);

        connection.Execute(
            "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('SchemaVersion', $Version)",
            new { Version = CurrentSchemaVersion.ToString() }, transaction: tx);
        tx.Commit();

        // The cache maps instrument -> id from here on, the old entries map name -> id
        ClearLocalSymbolIdCache(connection.DataSource);

        GlobalData.AddTextToLogTab($"candles.db {exchange.Name}: converted to version {CurrentSchemaVersion} — " +
            $"{adopted} symbol(s) kept, {ambiguous} fetched again (the name covers more than one " +
            $"instrument), {orphan} orphan(s) removed");
    }


    private sealed class LocalSymbolRow
    {
        public int SymbolId { get; set; }
        public string Name { get; set; } = "";
    }


    private sealed class LocalInstrumentRow
    {
        public int SymbolId { get; set; }
        public string ExchangeName { get; set; } = "";
    }


    /// <summary>
    /// Removes registrations, and everything stored under them, for instruments this exchange no
    /// longer offers. Driven by what is IN the file rather than by the scanner's symbol list: the
    /// per-symbol cleanup iterates the live symbols and can therefore never reach a row that no
    /// longer has one, so those rows survived every cleanup. Measured on 2026-08-14: 1153 of the
    /// 1586 rows in the Okx futures store, holding 10% of its candles, plus a row with an empty name.
    /// </summary>
    private static int CleanOrphanSymbols(SqliteConnection connection, Model.CryptoExchange exchange)
    {
        // An empty symbol list would make every row look like an orphan and empty the entire file
        if (exchange.SymbolListName.Count == 0)
            return 0;

        HashSet<string> live = new(StringComparer.OrdinalIgnoreCase);
        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            live.Add(InstrumentKeyFor(symbol));

        List<LocalInstrumentRow> rows = [.. connection.Query<LocalInstrumentRow>(
            "SELECT SymbolId, ExchangeName FROM Symbol")];

        List<LocalInstrumentRow> orphans = [.. rows.Where(x => !live.Contains(x.ExchangeName))];
        if (orphans.Count == 0)
            return 0;

        using var tx = connection.BeginTransaction();
        foreach (LocalInstrumentRow row in orphans)
        {
            connection.Execute("DELETE FROM Candle WHERE SymbolId = $SymbolId",
                new { row.SymbolId }, transaction: tx);
            connection.Execute("DELETE FROM SymbolInterval WHERE SymbolId = $SymbolId",
                new { row.SymbolId }, transaction: tx);
            connection.Execute("DELETE FROM Symbol WHERE SymbolId = $SymbolId",
                new { row.SymbolId }, transaction: tx);
        }
        tx.Commit();

        // Those instruments must not keep resolving to a now deleted id
        ClearLocalSymbolIdCache(connection.DataSource);
        return orphans.Count;
    }


    // Local symbol-id cache, per database FILE (not global): the scanner and the emulator run
    // against different candle databases, each with its own autoincrement numbering, so a shared
    // cache would hand out an id from the wrong file. SqliteConnection.DataSource is the resolved
    // path, which is exactly the identity we need. Both levels are concurrent because
    // SaveCandlesAsync/LoadCandlesAsync resolve from parallel workers.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string,
        System.Collections.Concurrent.ConcurrentDictionary<string, int>> LocalSymbolIdCache =
        new(StringComparer.OrdinalIgnoreCase);


    private static System.Collections.Concurrent.ConcurrentDictionary<string, int> CacheFor(SqliteConnection connection)
        => LocalSymbolIdCache.GetOrAdd(connection.DataSource ?? "", _ => new(StringComparer.OrdinalIgnoreCase));


    /// <summary>
    /// Drops the cached name→local-id mapping for one database file, or for every file when
    /// <paramref name="dataSource"/> is null. Needed after a migration or after the file is
    /// deleted and recreated, since the autoincrement numbering starts over.
    /// </summary>
    public static void ClearLocalSymbolIdCache(string? dataSource = null)
    {
        if (dataSource == null)
            LocalSymbolIdCache.Clear();
        else
            LocalSymbolIdCache.TryRemove(dataSource, out _);
    }


    /// <summary>
    /// Local id for a symbol, registering the name when this database has not seen it before.
    /// Use for WRITE paths only — a read must not create rows for a symbol that has no candles.
    ///
    /// Must be called BEFORE the caller opens its transaction: Microsoft.Data.Sqlite requires
    /// every command on a connection with a pending transaction to carry that transaction, and
    /// the registration is deliberately its own tiny statement so a parallel writer that races
    /// on the same new name resolves to the same row (INSERT OR IGNORE against the UNIQUE index).
    /// </summary>
    private static int ResolveLocalSymbolId(SqliteConnection connection, CryptoSymbol symbol)
    {
        string instrument = InstrumentKeyFor(symbol);

        var cache = CacheFor(connection);
        if (cache.TryGetValue(instrument, out int cached))
            return cached;

        connection.Execute("INSERT OR IGNORE INTO Symbol (ExchangeName, Name) VALUES ($ExchangeName, $Name)",
            new { ExchangeName = instrument, symbol.Name });
        int localId = connection.ExecuteScalar<int>(
            "SELECT SymbolId FROM Symbol WHERE ExchangeName = $ExchangeName", new { ExchangeName = instrument });

        cache[instrument] = localId;
        return localId;
    }


    /// <summary>
    /// The key a symbol's candles are stored under: its exchange instrument. Falls back to the
    /// scanner name for the rare symbol that has no instrument id yet — such a symbol cannot be
    /// fetched either, and this keeps it from colliding with a real instrument.
    /// </summary>
    private static string InstrumentKeyFor(CryptoSymbol symbol)
        => string.IsNullOrEmpty(symbol.ExchangeName) ? symbol.Name : symbol.ExchangeName;


    /// <summary>
    /// Local id for a symbol without registering it. Returns false when this database holds no
    /// data for the symbol at all — the READ paths then simply produce nothing, which is the
    /// same outcome as a symbol whose candles were never fetched.
    /// </summary>
    private static bool TryGetLocalSymbolId(SqliteConnection connection, CryptoSymbol symbol, out int localSymbolId)
    {
        string instrument = InstrumentKeyFor(symbol);

        var cache = CacheFor(connection);
        if (cache.TryGetValue(instrument, out localSymbolId))
            return true;

        int? found = connection.ExecuteScalar<int?>(
            "SELECT SymbolId FROM Symbol WHERE ExchangeName = $ExchangeName", new { ExchangeName = instrument });
        if (found == null)
        {
            localSymbolId = 0;
            return false;
        }

        localSymbolId = found.Value;
        cache[instrument] = localSymbolId;
        return true;
    }


    /// <summary>
    /// Upsert <see cref="CryptoSymbolInterval.LastCandleSynchronized"/> for one (symbol,
    /// interval) into the SymbolInterval table. Runs inside the caller's transaction
    /// so it commits atomically together with the candle inserts.
    /// </summary>
    private static void SaveSymbolInterval(SqliteConnection connection, SqliteTransaction tx, int localSymbolId,
        CryptoSymbolInterval symbolInterval)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "INSERT OR REPLACE INTO SymbolInterval (SymbolId, IntervalId, LastSync) " +
            "VALUES ($SymbolId, $IntervalId, $LastSync)";

        var pSymbol = cmd.CreateParameter();
        pSymbol.ParameterName = "$SymbolId";
        pSymbol.Value = localSymbolId;
        cmd.Parameters.Add(pSymbol);

        var pInterval = cmd.CreateParameter();
        pInterval.ParameterName = "$IntervalId";
        pInterval.Value = symbolInterval.Interval.Id;
        cmd.Parameters.Add(pInterval);

        var pLastSync = cmd.CreateParameter();
        pLastSync.ParameterName = "$LastSync";
        pLastSync.Value = symbolInterval.LastCandleSynchronized.HasValue
            ? (long)symbolInterval.LastCandleSynchronized.Value.Minutes
            : (object)DBNull.Value;
        cmd.Parameters.Add(pLastSync);

        cmd.ExecuteNonQuery();
    }


    /// <summary>
    /// Restore <see cref="CryptoSymbolInterval.LastCandleSynchronized"/> for every interval
    /// of the symbol from the SymbolInterval table. Called by <see cref="LoadCandlesForSymbol"/>
    /// after the candles themselves have been loaded.
    /// </summary>
    private static void LoadSymbolIntervals(SqliteConnection connection, int localSymbolId, CryptoSymbol symbol)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT IntervalId, LastSync FROM SymbolInterval WHERE SymbolId = $SymbolId";

        var pSymbol = cmd.CreateParameter();
        pSymbol.ParameterName = "$SymbolId";
        pSymbol.Value = localSymbolId;
        cmd.Parameters.Add(pSymbol);

        Dictionary<int, CryptoSymbolInterval> intervalsId = [];
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            intervalsId[symbolInterval.Interval.Id] = symbolInterval;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int intervalId = reader.GetInt32(0);
            if (!intervalsId.TryGetValue(intervalId, out CryptoSymbolInterval? symbolInterval))
                continue;

            if (reader.IsDBNull(1))
                symbolInterval.LastCandleSynchronized = null;
            else
                symbolInterval.LastCandleSynchronized = new CandleTime((uint)reader.GetInt64(1));
        }
    }


    /// <summary>
    /// Read every candle for the given symbol from the candle DB and populate the in-memory
    /// CandleList per interval. The PriceDecimals on the symbol is preserved as TickDecimals
    /// on each candle (taken from the Ticks column so it matches what was written). LastCandle
    /// per interval is updated to the highest OpenTime found.
    ///
    /// Mirrors DataStore.LoadCandlesForSymbol in behaviour but reads from SQLite instead of
    /// the .compressed file. Also restores LastCandleSynchronized per interval from the
    /// SymbolIntervalState table so the exchange fetcher continues from where it left off.
    /// </summary>
    public static void LoadCandlesForSymbol(SqliteConnection connection, CryptoSymbol symbol)
    {
        // Reset the previous collected trend data (once a day is preferred). Full reset (incl.
        // per-interval cached ZigZag indicators) because CandleList objects are about to be replaced
        // below, and a cached ZigZagResult.Candle would otherwise keep referencing a stale candle.
        symbol.Data.ResetTrendDataAndCaches();

        // Unknown to this candle database = no candles were ever stored for it. Nothing to read.
        if (!TryGetLocalSymbolId(connection, symbol, out int localSymbolId))
            return;

        // Per-interval SELECT bounded by GetCandleFetchStart so we don't materialise the
        // bulk DLZ-zoom candles at startup — those stay in the DB and only flow into memory
        // when the zone calculation explicitly asks for them. The PK (SymbolId, IntervalId,
        // OpenTime) makes each per-interval range scan a direct B-tree seek. One prepared
        // statement, executed once per interval, parameters rebound between iterations.
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT OpenTime, Ticks, Open, High, Low, Close, Volume " +
            "FROM Candle " +
            "WHERE SymbolId = $SymbolId AND IntervalId = $IntervalId AND OpenTime >= $MinOpenTime ";
        // Do not read future candles
        if (GlobalData.IsEmulatorMode)
            cmd.CommandText += " and OpenTime <= $OpenTime ";
        cmd.CommandText += "ORDER BY OpenTime";

        var pSymbol = cmd.CreateParameter();
        pSymbol.ParameterName = "$SymbolId";
        pSymbol.Value = localSymbolId;
        cmd.Parameters.Add(pSymbol);

        var pInterval = cmd.CreateParameter();
        pInterval.ParameterName = "$IntervalId";
        cmd.Parameters.Add(pInterval);

        var pMinOpenTime = cmd.CreateParameter();
        pMinOpenTime.ParameterName = "$MinOpenTime";
        cmd.Parameters.Add(pMinOpenTime);

        if (GlobalData.IsEmulatorMode)
        {
            var pOpenTime = cmd.CreateParameter();
            pOpenTime.ParameterName = "$OpenTime";
            pOpenTime.Value = (long)CandleTime.FromDateTime(GlobalData.Clock.UtcNow).Minutes;
            cmd.Parameters.Add(pOpenTime);
        }

        cmd.Prepare();

        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            pInterval.Value = symbolInterval.Interval.Id;
            // Same per-interval bound that the file-based DataStore applied during read.
            CandleTime startFetch = CandleTools.GetCandleFetchStart(symbol, symbolInterval.Interval, GlobalData.Clock.UtcNow);
            pMinOpenTime.Value = (long)startFetch.Minutes;

            symbolInterval.CandleList.Lock();
            try
            {
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    uint openTimeMinutes = (uint)reader.GetInt64(0);
                    // Raw byte (decimals + IsFilled bit, see CryptoCandle.TickDecimalsRaw) -
                    // mask off the flag bit before computing the tick size.
                    byte ticksRaw = (byte)reader.GetInt32(1);
                    decimal tickSize = TickSizeFor((byte)(ticksRaw & 0x0F));

                    CryptoCandle candle = new()
                    {
                        OpenTime = new CandleTime(openTimeMinutes),
                        TickDecimalsRaw = ticksRaw,
                        // Setting Open/High/Low/Close via decimal accessors round-trips through the
                        // tick reconstruction, identical to what LoadVersion3 does for the file path.
                        Open = reader.GetInt64(2) * tickSize,
                        High = reader.GetInt64(3) * tickSize,
                        Low = reader.GetInt64(4) * tickSize,
                        Close = reader.GetInt64(5) * tickSize,
                        Volume = (decimal)reader.GetDouble(6),
                    };

                    symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
                }
            }
            finally
            {
                symbolInterval.CandleList.Unlock();
            }
        }

        // Restore LastCandleSynchronized per interval so the exchange fetcher continues
        // from where it left off instead of refetching the full GetCandleFetchStart window.
        LoadSymbolIntervals(connection, localSymbolId, symbol);
    }


    /// <summary>
    /// Read ALL candles for one (symbol, interval) from the candle DB into the in-memory
    /// CandleList. No OpenTime filter — counterpart to the per-interval bulk file that
    /// ZoneCandleEngine used to read on demand for DLZ zoom-refinement. Uses TryAdd so
    /// candles that are already in memory (loaded earlier via the bounded startup path)
    /// are silently skipped.
    /// </summary>
    public static void LoadCandlesForSymbolInterval(SqliteConnection connection, CryptoSymbol symbol,
        CryptoSymbolInterval symbolInterval)
    {
        // Unknown to this candle database = no candles were ever stored for it. Nothing to read.
        if (!TryGetLocalSymbolId(connection, symbol, out int localSymbolId))
            return;

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT OpenTime, Ticks, Open, High, Low, Close, Volume " +
            "FROM Candle " +
            "WHERE SymbolId = $SymbolId AND IntervalId = $IntervalId ";
        // Do not read future candles
        if (GlobalData.IsEmulatorMode)
            cmd.CommandText += " and OpenTime <= $OpenTime ";
        cmd.CommandText += "ORDER BY OpenTime";

        var pSymbol = cmd.CreateParameter();
        pSymbol.ParameterName = "$SymbolId";
        pSymbol.Value = localSymbolId;
        cmd.Parameters.Add(pSymbol);

        var pInterval = cmd.CreateParameter();
        pInterval.ParameterName = "$IntervalId";
        pInterval.Value = symbolInterval.Interval.Id;
        cmd.Parameters.Add(pInterval);

        if (GlobalData.IsEmulatorMode)
        {
            var pOpenTime = cmd.CreateParameter();
            pOpenTime.ParameterName = "$OpenTime";
            pOpenTime.Value = (long)CandleTime.FromDateTime(GlobalData.Clock.UtcNow).Minutes;
            cmd.Parameters.Add(pOpenTime);
        }


        symbolInterval.CandleList.Lock();
        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                uint openTimeMinutes = (uint)reader.GetInt64(0);
                byte ticksRaw = (byte)reader.GetInt32(1);
                decimal tickSize = TickSizeFor((byte)(ticksRaw & 0x0F));

                CryptoCandle candle = new()
                {
                    OpenTime = new CandleTime(openTimeMinutes),
                    TickDecimalsRaw = ticksRaw,
                    Open = reader.GetInt64(2) * tickSize,
                    High = reader.GetInt64(3) * tickSize,
                    Low = reader.GetInt64(4) * tickSize,
                    Close = reader.GetInt64(5) * tickSize,
                    Volume = (decimal)reader.GetDouble(6),
                };

                symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            }
        }
        finally
        {
            symbolInterval.CandleList.Unlock();
        }
    }


    /// <summary>
    /// Loads candles for a single symbol+interval restricted to an OpenTime range.
    /// Returned list is in ascending OpenTime order; both bounds are inclusive in minutes
    /// (matching <see cref="CandleTime.Minutes"/>). Used by the emulator's CandleSource
    /// to materialise a fixed replay window without filling the global CandleList.
    /// </summary>
    public static List<CryptoCandle> LoadCandlesInRange(SqliteConnection connection,
        CryptoSymbol symbol, CryptoInterval interval, uint fromMinutes, uint toMinutes)
    {
        // Unknown to this candle database = no candles were ever stored for it. Nothing to read.
        if (!TryGetLocalSymbolId(connection, symbol, out int localSymbolId))
            return [];

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT OpenTime, Ticks, Open, High, Low, Close, Volume " +
            "FROM Candle " +
            "WHERE SymbolId = $SymbolId AND IntervalId = $IntervalId " +
            "  AND OpenTime BETWEEN $From AND $To ";
        // Do not read future candles, but only while a run is actually replaying. CurrentEmulatorRunId
        // is null once a run finishes (or before one starts), and GlobalData.Clock then stays frozen at
        // the last replayed minute. Without this guard, opening a position's chart after the run ended
        // (or for an older run, with the clock parked at a different run's end time) would silently clip
        // or empty the requested range against a stale "now" that has nothing to do with the query.
        if (GlobalData.IsEmulatorMode && GlobalData.CurrentEmulatorRunId.HasValue)
            cmd.CommandText += " and OpenTime <= $OpenTime ";
        cmd.CommandText += " ORDER BY OpenTime";

        var pSymbol = cmd.CreateParameter();
        pSymbol.ParameterName = "$SymbolId";
        pSymbol.Value = localSymbolId;
        cmd.Parameters.Add(pSymbol);

        var pInterval = cmd.CreateParameter();
        pInterval.ParameterName = "$IntervalId";
        pInterval.Value = interval.Id;
        cmd.Parameters.Add(pInterval);

        var pFrom = cmd.CreateParameter();
        pFrom.ParameterName = "$From";
        pFrom.Value = (long)fromMinutes;
        cmd.Parameters.Add(pFrom);

        var pTo = cmd.CreateParameter();
        pTo.ParameterName = "$To";
        pTo.Value = (long)toMinutes;
        cmd.Parameters.Add(pTo);

        if (GlobalData.IsEmulatorMode && GlobalData.CurrentEmulatorRunId.HasValue)
        {
            var pOpenTime = cmd.CreateParameter();
            pOpenTime.ParameterName = "$OpenTime";
            pOpenTime.Value = (long)CandleTime.FromDateTime(GlobalData.Clock.UtcNow).Minutes;
            cmd.Parameters.Add(pOpenTime);
        }

        var list = new List<CryptoCandle>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            uint openTimeMinutes = (uint)reader.GetInt64(0);
            byte ticksRaw = (byte)reader.GetInt32(1);
            decimal tickSize = TickSizeFor((byte)(ticksRaw & 0x0F));

            list.Add(new CryptoCandle
            {
                OpenTime = new CandleTime(openTimeMinutes),
                TickDecimalsRaw = ticksRaw,
                Open = reader.GetInt64(2) * tickSize,
                High = reader.GetInt64(3) * tickSize,
                Low = reader.GetInt64(4) * tickSize,
                Close = reader.GetInt64(5) * tickSize,
                Volume = (decimal)reader.GetDouble(6),
            });
        }
        return list;
    }


    /// <summary>
    /// Parallel load route that reads candles from the per-exchange candles.db (SQLite).
    /// Each worker opens its own connection — SqliteConnection is not thread-safe but
    /// WAL allows multiple concurrent readers on the same DB file, so this scales nearly
    /// linearly up to MaxDegreeOfParallelism. Mirrors the per-symbol gating that the
    /// file-based DataStore.LoadCandlesAsync uses.
    /// </summary>
    public static async Task LoadCandlesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
            {
                GlobalData.AddTextToLogTab("candles.db load: no active exchange — skipped");
                return;
            }

            // Name the exchange in the message: the candles.db is per exchange, so after switching
            // exchanges the user must be able to see which file is actually being read.
            GlobalData.AddTextToLogTab($"Loading candle information from candles.db for {exchange.Name} (please wait!)");

            // An unconverted (version 1) file must not be read: its ids refer to a Symbol table
            // that may since have been rebuilt, so the candles would end up on the wrong symbols.
            // Skip the candle store and keep starting up — the migration is a menu action.
            try
            {
                InitializeSchema(exchange);
            }
            catch (CandleDatabaseSchemaException error)
            {
                GlobalData.AddErrorToLogTab($"candles.db load {exchange.Name}: SKIPPED — {error.Message}");
                return;
            }

            // Snapshot to avoid enumerating a live collection from parallel workers
            var symbols = exchange.SymbolListName.Values.ToList();
            GlobalData.AddTextToLogTab($"candles.db load {exchange.Name}: {symbols.Count} symbols");

            int loaded = 0;
            int skippedInactive = 0;
            int skippedLowVolume = 0;
            int failed = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Parallel.ForEach(symbols, ParallelOptions, symbol =>
            {
                if (!symbol.QuoteData.FetchCandles || symbol.Status != 1)
                {
                    Interlocked.Increment(ref skippedInactive);
                    return;
                }

                // Honour the same minimal-volume gating as the file loader.
                // Skipped in the emulator (the user explicitly chose these symbols) so a low-volume
                // symbol's candles are kept on load instead of being cleared.
                if (!GlobalData.IsEmulatorMode && !symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
                {
                    if (symbol.ClearCandles())
                        ScannerLog.Logger.Trace($"Cleared candles for {symbol.Name}");
                    Interlocked.Increment(ref skippedLowVolume);
                    return;
                }

                try
                {
                    using var db = new CandleDatabase(exchange);
                    db.Open();
                    LoadCandlesForSymbol(db.Connection, symbol);
                    Interlocked.Increment(ref loaded);
                }
                catch (Exception sqliteError)
                {
                    Interlocked.Increment(ref failed);
                    ScannerLog.Logger.Error(sqliteError, "candles.db read failed for " + symbol.Name);
                    GlobalData.AddErrorToLogTab($"candles.db read failed for {symbol.Name}: {sqliteError.Message}");
                }
            });

            sw.Stop();
            GlobalData.AddTextToLogTab(
                $"candles.db load {exchange.Name}: done loaded={loaded} skippedInactive={skippedInactive} " +
                $"skippedLowVolume={skippedLowVolume} failed={failed} in {sw.ElapsedMilliseconds} ms");
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
        // Registers the name if this database has not seen it yet. Deliberately before
        // BeginTransaction — see ResolveLocalSymbolId.
        int localSymbolId = ResolveLocalSymbolId(connection, symbol);

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

        var pOpenTime = cmd.CreateParameter();
        pOpenTime.ParameterName = "$OpenTime";
        cmd.Parameters.Add(pOpenTime);

        var pTickDecimals = cmd.CreateParameter(); pTickDecimals.ParameterName = "$Ticks"; cmd.Parameters.Add(pTickDecimals);
        var pOpen = cmd.CreateParameter(); pOpen.ParameterName = "$Open"; cmd.Parameters.Add(pOpen);
        var pHigh = cmd.CreateParameter(); pHigh.ParameterName = "$High"; cmd.Parameters.Add(pHigh);
        var pLow = cmd.CreateParameter(); pLow.ParameterName = "$Low"; cmd.Parameters.Add(pLow);
        var pClose = cmd.CreateParameter(); pClose.ParameterName = "$Close"; cmd.Parameters.Add(pClose);
        var pVolume = cmd.CreateParameter(); pVolume.ParameterName = "$Volume"; cmd.Parameters.Add(pVolume);

        cmd.Prepare();

        pSymbol.Value = localSymbolId;

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
                    pTickDecimals.Value = candle.TickDecimalsRaw;
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

        // Persist LastCandleSynchronized for every interval in the same transaction so the
        // exchange fetcher can continue from where it left off after a restart.
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            SaveSymbolInterval(connection, tx, localSymbolId, symbolInterval);

        tx.Commit();

    }


    /// <summary>
    /// Upsert candles for a single (symbol, interval) into the candle DB in one transaction
    /// with a prepared statement. Used by ZoneCandleEngine.SaveCandleDataToDiskAsync and the
    /// per-interval migration in ReadCandlesFromDiskAsync so we don't touch other intervals
    /// while persisting the one whose CandleList was just modified.
    /// </summary>
    public static void SaveCandlesForSymbolInterval(SqliteConnection connection, CryptoSymbol symbol, CryptoSymbolInterval symbolInterval)
    {
        // Registers the name if this database has not seen it yet. Deliberately before
        // BeginTransaction — see ResolveLocalSymbolId.
        int localSymbolId = ResolveLocalSymbolId(connection, symbol);

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

        pSymbol.Value = localSymbolId;
        pInterval.Value = symbolInterval.Interval.Id;

        symbolInterval.CandleList.Lock();
        try
        {
            foreach (CryptoCandle candle in symbolInterval.CandleList.Values)
            {
                pOpenTime.Value = (long)candle.OpenTime.Minutes;
                pTickDecimals.Value = candle.TickDecimalsRaw;
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

        // Persist LastCandleSynchronized for this single interval in the same transaction.
        SaveSymbolInterval(connection, tx, localSymbolId, symbolInterval);

        tx.Commit();
    }



    /// <summary>
    /// Parallel save route mirroring DataStore.SaveCandlesAsync but writing to the
    /// per-exchange candles.db. Scoped to the ACTIVE exchange only — consistent with
    /// LoadCandlesAsync, CleanCandlesAsync and DataStore.CleanOrphanCandleFilesAsync.
    /// Saving across every configured exchange would create empty {Name}.db files for
    /// inactive exchanges (their SymbolListName is empty, but InitializeSchema still
    /// opens a SqliteConnection in ReadWriteCreate mode).
    ///
    /// Each worker opens its own connection. Writes serialise on the SQLite write-lock
    /// (one writer at a time, even in WAL); the SemaphoreSlim below makes that
    /// serialisation explicit so we don't rely on busy_timeout races for large symbols.
    /// </summary>
    public static async Task SaveCandlesAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
            {
                GlobalData.AddTextToLogTab("candles.db save: no active exchange — skipped");
                return;
            }

            // Name the exchange in the message: the candles.db is per exchange, so after switching
            // exchanges the user must be able to see which file is actually being written.
            GlobalData.AddTextToLogTab($"Saving candles.db for {exchange.Name} (please wait!)");
            var swTotal = System.Diagnostics.Stopwatch.StartNew();

            // Writing into an unconverted (version 1) file would mix candles resolved through the
            // local registry with candles stored under foreign ids. Refuse until it is migrated.
            try
            {
                InitializeSchema(exchange);
            }
            catch (CandleDatabaseSchemaException error)
            {
                GlobalData.AddErrorToLogTab($"candles.db save {exchange.Name}: SKIPPED — {error.Message}");
                return;
            }

            // Snapshot to avoid enumerating a live collection from parallel workers
            var symbols = exchange.SymbolListName.Values.ToList();
            GlobalData.AddTextToLogTab($"candles.db save {exchange.Name}: {symbols.Count} symbols");

            int saved = 0;
            int failed = 0;

            // SQLite serialises writers on the file write-lock anyway. Letting multiple
            // workers race on BeginTransaction would just rely on busy_timeout, which
            // breaks down for large symbols where one transaction holds the lock longer
            // than the timeout window. The SemaphoreSlim makes the serialisation
            // explicit and queues workers cleanly. Parallelism still pays off because
            // a worker can prepare its next CandleList batch while another is committing.
            using var sqliteWriteLock = new SemaphoreSlim(1, 1);

            await Parallel.ForEachAsync(symbols, ParallelOptions, async (symbol, cancellationToken) =>
            {
                try
                {
                    // Don't save candles for symbols below the minimal volume threshold.
                    // Skipped in the emulator: the user explicitly picks the symbols to replay, so
                    // their fetched candles must be kept regardless of the live volume heuristic
                    // (a freshly-added symbol has no 24h volume yet, so this would clear the just-
                    // fetched candles and force a full re-fetch on every run).
                    bool releaseCandles = !GlobalData.IsEmulatorMode
                        && !symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading();

                    await sqliteWriteLock.WaitAsync(cancellationToken);
                    try
                    {
                        using var db = new CandleDatabase(exchange);
                        db.Open();
                        SaveCandlesForSymbol(db.Connection, symbol);
                        Interlocked.Increment(ref saved);
                    }
                    finally
                    {
                        sqliteWriteLock.Release();
                    }

                    // Release the memory only AFTER the write. Clearing first threw away everything that
                    // had arrived since the previous save — the websocket keeps delivering for a symbol
                    // that just dropped below the threshold, because the ticker subscription is only
                    // rebuilt at startup. That silently cost up to a full hour of candles, which then had
                    // to be filled in flat by BulkAddMissingCandles (ARBUSDT, 2026-08-09 21:35-22:34).
                    if (releaseCandles)
                        symbol.ClearCandles();
                }
                catch (Exception sqliteError)
                {
                    Interlocked.Increment(ref failed);
                    ScannerLog.Logger.Error(sqliteError, "candles.db write failed for " + symbol.Name);
                    GlobalData.AddErrorToLogTab($"candles.db write failed for {symbol.Name}: {sqliteError.Message}");
                }
            });

            swTotal.Stop();
            GlobalData.AddTextToLogTab(
                $"candles.db save {exchange.Name}: done saved={saved} failed={failed} in {swTotal.ElapsedMilliseconds} ms");
            ScannerLog.Logger.Trace("candles.db saved");
        }
        finally
        {
            Semaphore.Release();
        }
    }


    // -----------------------------------------------------------------------
    // Cleanup — two-phase, respects the "zoom principle".
    //
    // The zone recalc rediscovers ALL pivots (open + closed) inside the scan-window and
    // re-zooms them on every cycle. So "visible == within scan-window" — closed status by
    // itself does NOT mean a zone can be discarded. Cleanup therefore proceeds in two
    // phases against the AUTHORITATIVE state stored in CryptoDatabase.Zone:
    //
    //   Phase A: in CryptoDatabase, DELETE FROM Zone
    //              WHERE SymbolId = ?
    //                AND CloseTime IS NOT NULL
    //                AND OpenTime  < scan-window-start
    //            Closed-and-out-of-window zones are gone from the user's view and won't be
    //            rediscovered, so their persistence has no value anymore.
    //
    //   Phase B: query the REMAINING zones (open + still-visible closed) from CryptoDatabase
    //            and use each one's [OpenTime, OpenTime+Z.Interval.Duration] as a keep-window
    //            for candles in lower-or-equal-duration intervals (the candles DLZ may zoom
    //            into). Together with the standard per-interval window, this is the union
    //            that must be kept in candles.db.
    //
    // scan-window-start = MIN over (DLZ + FVG enabled intervals) of GetCandleFetchStart(I).
    //
    // When the symbol is no longer relevant (status off, fetching disabled, below volume
    // threshold and not trading and not a barometer symbol) ALL its candles are deleted.
    // -----------------------------------------------------------------------

    /// <summary>
    /// A pivot zone loaded from the main CryptoDatabase Zone table. Just enough info to
    /// build the candle keep-ranges — full CryptoZone hydration is unnecessary here.
    /// CloseTime = null means the zone is still open and its zoom-window should run all
    /// the way up to "now".
    /// </summary>
    private readonly record struct PivotZone(int IntervalId, CandleTime OpenTime, CandleTime? CloseTime);

    /// <summary>
    /// Returns true when the symbol no longer needs its candles persisted. Mirrors the
    /// "delete the file" gating that DataStore.SaveCandlesAsync already applies.
    /// Internal so the file-based orphan cleanup in DataStore can reuse the same rule.
    /// </summary>
    internal static bool SymbolHasNoUse(CryptoSymbol symbol)
    {
        if (!symbol.QuoteData.FetchCandles || symbol.Status == 0)
            return true;
        // In the emulator the user explicitly picks the symbols, so a low-volume symbol still has a
        // use (its candles must be kept); only apply the volume gate for the live scanner.
        if (!GlobalData.IsEmulatorMode
            && !symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
            return true;
        return false;
    }

    /// <summary>
    /// Earliest CandleTime that is still "visible" for this symbol — the MIN of
    /// GetCandleFetchStart over every interval enabled for DLZ or FVG. Used by Phase A
    /// to decide which closed zones can be deleted from the main DB.
    /// </summary>
    private static CandleTime ComputeScanWindowStart(CryptoSymbol symbol)
    {
        CandleTime earliest = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1);
        bool any = false;

        // Union of DLZ + FVG + SMC enabled intervals — these are the only ones the zone recalc
        // scans, so anything that opened before the earliest of their windows can never be
        // rediscovered as a pivot.
        foreach (var intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList
            .Concat(GlobalData.Settings.Signal.ZonesFvg.IntervalList)
            .Concat(GlobalData.Settings.Signal.ZonesSmc.IntervalList))
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
                continue;

            CandleTime start = CandleTools.GetCandleFetchStartForZones(symbol, interval, GlobalData.Clock.UtcNow);
            if (!any || start.Minutes < earliest.Minutes)
            {
                earliest = start;
                any = true;
            }
        }

        return earliest;
    }

    /// <summary>
    /// Phase A — delete from the main CryptoDatabase Zone table every zone for this symbol
    /// that is both closed AND opened before the scan-window. Those zones are out of the
    /// user's view and the recalc would not rediscover them, so retaining them serves no
    /// purpose. Returns the number of zone rows actually removed.
    /// </summary>
    private static int CleanOldZonesFromMainDb(SqliteConnection mainConn, CryptoSymbol symbol,
        CandleTime scanWindowStart)
    {
        // OpenTime column is TEXT affinity but the CandleTimeTypeHandler stores values as
        // INTEGER (uint Minutes). CAST forces a numeric comparison regardless of how the
        // value was inserted historically (older rows might have been written as text).
        const string sql =
            "DELETE FROM Zone " +
            "WHERE SymbolId = @SymbolId " +
            "  AND CloseTime IS NOT NULL " +
            "  AND CAST(OpenTime AS INTEGER) < @Cutoff";

        return mainConn.Execute(sql, new
        {
            SymbolId = symbol.Id,
            Cutoff = (long)scanWindowStart.Minutes,
        });
    }

    /// <summary>
    /// Phase B preload — read every remaining (open + still-visible closed) zone for the
    /// symbol from the main CryptoDatabase. Each pivot contributes a candle keep-window
    /// equal to [OpenTime, OpenTime+Z.Interval.Duration] across all intervals with
    /// duration ≤ zone.Interval.Duration.
    /// </summary>
    private static List<PivotZone> LoadPivotZonesFromMainDb(SqliteConnection mainConn, CryptoSymbol symbol)
    {
        List<PivotZone> result = [];

        using var cmd = mainConn.CreateCommand();
        // CASE ... THEN NULL ELSE CAST AS INTEGER preserves NULL CloseTime (= open zone)
        // while still forcing a numeric value for closed zones (CloseTime is TEXT affinity
        // but CandleTimeTypeHandler writes integer values).
        cmd.CommandText =
            "SELECT IntervalId, " +
            "       CAST(OpenTime  AS INTEGER), " +
            "       CASE WHEN CloseTime IS NULL THEN NULL ELSE CAST(CloseTime AS INTEGER) END " +
            "FROM Zone WHERE SymbolId = $SymbolId";
        var pSymbol = cmd.CreateParameter(); pSymbol.ParameterName = "$SymbolId"; pSymbol.Value = symbol.Id; cmd.Parameters.Add(pSymbol);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int intervalId = reader.GetInt32(0);
            if (reader.IsDBNull(1))
                continue;
            uint openMinutes = (uint)reader.GetInt64(1);
            CandleTime? close = reader.IsDBNull(2) ? null : new CandleTime((uint)reader.GetInt64(2));
            result.Add(new PivotZone(intervalId, new CandleTime(openMinutes), close));
        }

        return result;
    }

    /// <summary>
    /// Build the keep-ranges per interval Id for one symbol — union of:
    ///   1) standard window per interval ([GetCandleFetchStart, now])
    ///   2) per pivot zone (loaded from CryptoDatabase) the time-range
    ///      [OpenTime, CloseTime ?? now]  — the zone's "lifetime"
    ///      for the zone interval itself AND every interval with smaller-or-equal duration.
    ///
    /// The full lifetime (not just one zone duration) is what the DLZ zoom-engine needs:
    /// while a zone is alive the recalc may at any moment re-zoom into the lower-TF series
    /// between OpenTime and the current price action to re-evaluate touches / mitigation,
    /// just like the old per-interval .compressed files used to retain.
    /// </summary>
    private static Dictionary<int, List<(CandleTime start, CandleTime end)>> ComputeKeepRanges(
        CryptoSymbol symbol, List<PivotZone> pivots)
    {
        Dictionary<int, List<(CandleTime, CandleTime)>> result = [];
        CandleTime now = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1);

        // 1) Standard window per interval (today's GetCandleFetchStart bound).
        //    GetCandleFetchStart works at minute precision (align(now, 1m) - 500*Duration),
        //    but stored candles have OpenTime aligned to interval.Duration. For sub-day
        //    intervals the difference is negligible, but for 1d the keep-range start can
        //    sit in the middle of a day while the oldest stored 1d candle is on the day
        //    boundary just before — it then falls outside the range and gets deleted
        //    every cleanup pass, only to be re-fetched on the next cycle.
        //
        //    Align the start DOWN to the interval boundary so candle timestamps and the
        //    keep-range boundary live on the same grid, and subtract one extra interval
        //    of buffer: the exchange typically returns the candle whose period contains
        //    fetcher startTime (one period before the floored boundary), so without the
        //    buffer that candle still falls just outside on every midnight rollover.
        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            CandleTime start = CandleTools.GetCandleFetchStartForZones(symbol, symbolInterval.Interval, GlobalData.Clock.UtcNow);
            uint duration = symbolInterval.Interval.Duration;
            if (duration > 1)
            {
                uint aligned = start.Minutes - (start.Minutes % duration);
                if (aligned >= duration)
                    aligned -= duration;
                start = new CandleTime(aligned);
            }
            AddRange(result, symbolInterval.Interval.Id, start, now);
        }

        // 2) Per pivot zone — keep its entire LIFETIME on the zone interval itself + all
        //    intervals with smaller-or-equal duration. Open zones run until "now"; closed
        //    zones until their CloseTime. Open OR still-visible-closed zones both count.
        foreach (var pivot in pivots)
        {
            if (!GlobalData.IntervalListId.TryGetValue(pivot.IntervalId, out var zoneInterval))
                continue;

            CandleTime zoneStart = pivot.OpenTime;
            CandleTime zoneEnd = pivot.CloseTime ?? now;

            // Sanity: if CloseTime is somehow before OpenTime (corrupt row) fall back to
            // OpenTime+Duration so we still keep at least the zone's own candle.
            if (zoneEnd.Minutes < zoneStart.Minutes)
                zoneEnd = pivot.OpenTime + zoneInterval.Duration;

            foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
            {
                if (symbolInterval.Interval.Duration <= zoneInterval.Duration)
                    AddRange(result, symbolInterval.Interval.Id, zoneStart, zoneEnd);
            }
        }

        return result;
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
    /// Two-phase cleanup for one symbol:
    ///   Phase A (main DB): delete closed zones outside the scan-window from CryptoDatabase.
    ///   Phase B (candles.db): delete candles outside the union of standard window per
    ///   interval AND every remaining pivot zone's zoom window.
    /// Either deletes ALL candles (symbol no longer in use) or deletes everything outside
    /// the computed keep-ranges per interval. Logs a per-symbol summary to LogTab when at
    /// least one row was actually deleted.
    /// </summary>
    public static void CleanCandlesForSymbol(SqliteConnection connection, SqliteConnection mainConn, CryptoSymbol symbol)
    {
        // Two different databases, two different symbol ids — do not mix them up. Everything
        // touching `connection` (candles.db) uses localSymbolId; Phase A below talks to the main
        // database via mainConn and keeps using symbol.Id. A symbol this database never stored
        // has nothing to clean.
        if (!TryGetLocalSymbolId(connection, symbol, out int localSymbolId))
            return;

        using var tx = connection.BeginTransaction();

        if (SymbolHasNoUse(symbol))
        {
            int candleRows = connection.Execute(
                "DELETE FROM Candle WHERE SymbolId = @SymbolId",
                new { SymbolId = localSymbolId }, transaction: tx);

            // Sync-bookkeeping has no value without candles either — drop it too so the next
            // start treats the symbol as never-synced when it eventually comes back into use.
            int stateRows = connection.Execute(
                "DELETE FROM SymbolInterval WHERE SymbolId = @SymbolId",
                new { SymbolId = localSymbolId }, transaction: tx);

            // Same for the in-memory "already asked the exchange for this period": with the candles
            // gone it would claim history that is no longer there, and nothing would ever fetch it
            // again. Whoever deletes candles shortens that period — see CryptoSymbolInterval.
            foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
                symbolInterval.ForgetHistory();

            tx.Commit();

            if (candleRows > 0)
            {
                GlobalData.AddTextToLogTab(
                    $"candles.db cleanup [no-use] {symbol.Name}: deleted candles={candleRows}");
            }
            return;
        }

        // Phase A: drop closed-and-out-of-scan zones from the main DB. They can no longer
        // be rediscovered as pivots, so their candle data also doesn't need to be kept.
        CandleTime scanWindowStart = ComputeScanWindowStart(symbol);
        int zonesDeleted = CleanOldZonesFromMainDb(mainConn, symbol, scanWindowStart);

        // Phase B: read what's left (open + still-visible closed zones) and build the
        // candle keep-ranges from that pivot list + the standard per-interval window.
        var pivots = LoadPivotZonesFromMainDb(mainConn, symbol);
        var keepRanges = ComputeKeepRanges(symbol, pivots);
        int totalDeleted = 0;
        int intervalsWithDeletes = 0;

        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            int intervalId = symbolInterval.Interval.Id;
            int deleted;

            if (!keepRanges.TryGetValue(intervalId, out var ranges) || ranges.Count == 0)
            {
                // No keep-range for this interval → drop everything for it
                deleted = connection.Execute(
                    "DELETE FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @IntervalId",
                    new { SymbolId = localSymbolId, IntervalId = intervalId }, transaction: tx);
                if (deleted > 0)
                    symbolInterval.ForgetHistory();
            }
            else
            {
                // For symbols with many keep-ranges (seen in production for ARKMUSDT, PUNDIXUSDT
                // and others with hundreds of pivot zones across intervals), inlining the ranges
                // as a single 'WHERE NOT (... OR ... OR ...)' blew past SQLite's expression-tree
                // depth limit (default SQLITE_MAX_EXPR_DEPTH = 1000) and threw
                //   "Expression tree is too large (maximum depth 1000)".
                //
                // Push the ranges into a TEMP table and use a NOT EXISTS join. Expression depth
                // stays constant regardless of how many ranges there are. The temp table is
                // recreated empty per symbol-interval pass; it's automatically dropped when the
                // connection closes, but we reset it explicitly so consecutive intervals on the
                // same connection cannot see each other's data.
                connection.Execute(
                    "CREATE TEMP TABLE IF NOT EXISTS keep_ranges (s INTEGER NOT NULL, e INTEGER NOT NULL)",
                    transaction: tx);
                connection.Execute("DELETE FROM keep_ranges", transaction: tx);

                // SQLite limits parameters to 999 per statement, so cap the batch at 400 ranges
                // (= 800 parameters) per multi-VALUES insert.
                const int batchSize = 400;
                for (int offset = 0; offset < ranges.Count; offset += batchSize)
                {
                    int count = Math.Min(batchSize, ranges.Count - offset);
                    var sb = new System.Text.StringBuilder("INSERT INTO keep_ranges (s, e) VALUES ");
                    var batchParams = new DynamicParameters();
                    for (int i = 0; i < count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append($"(@s{i}, @e{i})");
                        batchParams.Add($"@s{i}", (long)ranges[offset + i].start.Minutes);
                        batchParams.Add($"@e{i}", (long)ranges[offset + i].end.Minutes);
                    }
                    connection.Execute(sb.ToString(), batchParams, transaction: tx);
                }

                // Ask first WHICH candles are about to go: the newest one that falls outside every
                // keep-range. Everything below it becomes unknown again, so the "already asked the
                // exchange" period has to resume after it — otherwise the zone engine would trust a
                // period whose candles this very statement removes and never fetch them again.
                long? newestRemoved = connection.ExecuteScalar<long?>(
                    "SELECT MAX(OpenTime) FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @IntervalId " +
                    "AND NOT EXISTS (SELECT 1 FROM keep_ranges WHERE Candle.OpenTime BETWEEN s AND e)",
                    new { SymbolId = localSymbolId, IntervalId = intervalId }, transaction: tx);

                deleted = connection.Execute(
                    "DELETE FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @IntervalId " +
                    "AND NOT EXISTS (SELECT 1 FROM keep_ranges WHERE Candle.OpenTime BETWEEN s AND e)",
                    new { SymbolId = localSymbolId, IntervalId = intervalId }, transaction: tx);

                if (deleted > 0 && newestRemoved.HasValue)
                    symbolInterval.ForgetHistoryUpTo(new CandleTime((uint)newestRemoved.Value));
            }

            if (deleted > 0)
            {
                totalDeleted += deleted;
                intervalsWithDeletes++;
            }
        }

        tx.Commit();

        if (totalDeleted > 0 || zonesDeleted > 0)
        {
            GlobalData.AddTextToLogTab(
                $"candles.db cleanup {symbol.Name}: zonesPurged={zonesDeleted} pivotsKept={pivots.Count} " +
                $"candlesDeleted={totalDeleted} (across {intervalsWithDeletes} intervals)");
        }
    }

    /// <summary>
    /// Loop all symbols for one exchange, run <see cref="CleanCandlesForSymbol"/> per symbol,
    /// then reclaim freed pages with PRAGMA incremental_vacuum.
    /// Sequential on purpose — cleanup is not time-critical and SQLite serializes writes anyway.
    /// </summary>
    public static void CleanCandlesForExchange(Model.CryptoExchange exchange)
    {
        // Deleting from an unconverted (version 1) file would apply the keep-ranges of one symbol
        // to the candles of another. Refuse until it is migrated.
        try
        {
            InitializeSchema(exchange);
        }
        catch (CandleDatabaseSchemaException error)
        {
            GlobalData.AddErrorToLogTab($"candles.db cleanup {exchange.Name}: SKIPPED — {error.Message}");
            return;
        }

        using var db = new CandleDatabase(exchange);
        db.Open();

        // Phase A of cleanup runs against the main CryptoDatabase Zone table. Open one
        // shared connection for the whole exchange pass so we don't pay the open/close
        // cost per symbol. Both DBs are local SQLite files; opening two connections in
        // the same thread is fine.
        using var mainDb = new CryptoDatabase();
        mainDb.Open();

        var symbols = exchange.SymbolListName.Values.ToList();
        GlobalData.AddTextToLogTab($"candles.db cleanup {exchange.Name}: scanning {symbols.Count} symbols");

        int processed = 0;
        int failed = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        foreach (var symbol in symbols)
        {
            try
            {
                CleanCandlesForSymbol(db.Connection, mainDb.Connection, symbol);
                processed++;
            }
            catch (Exception err)
            {
                failed++;
                ScannerLog.Logger.Error(err, "candles.db cleanup failed for " + symbol.Name);
                GlobalData.AddErrorToLogTab($"candles.db cleanup failed for {symbol.Name}: {err.Message}");
            }
        }

        // Then the rows the loop above cannot reach: instruments the exchange no longer offers
        int orphans = 0;
        try
        {
            orphans = CleanOrphanSymbols(db.Connection, exchange);
        }
        catch (Exception err)
        {
            ScannerLog.Logger.Error(err, "candles.db orphan cleanup failed");
            GlobalData.AddErrorToLogTab($"candles.db orphan cleanup failed: {err.Message}");
        }

        // Reclaim pages freed by the DELETEs above. INCREMENTAL keeps it cheap;
        // pass a generous page-budget so a large cleanup completes in one call.
        db.Connection.Execute("PRAGMA incremental_vacuum(10000);");

        sw.Stop();
        GlobalData.AddTextToLogTab(
            $"candles.db cleanup {exchange.Name}: done processed={processed} failed={failed} " +
            $"orphans={orphans} in {sw.ElapsedMilliseconds} ms");
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
        await Semaphore.WaitAsync();
        try
        {
            var exchange = GlobalData.ActiveExchange;
            if (exchange == null)
                return;

            // Name the exchange in the message: the candles.db is per exchange, so after switching
            // exchanges the user must be able to see which file is actually being cleaned.
            GlobalData.AddTextToLogTab($"Cleaning candles.db for {exchange.Name} (please wait!)");

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
