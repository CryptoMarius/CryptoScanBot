using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Context;

/// <summary>
/// Conversion of a candle database from schema version 1 to version 2. Runs automatically from
/// <see cref="CandleDatabase.InitializeSchema"/> — every application that opens a candle store
/// (scanner, Photino, emulator, tests) must be able to get past it, so it cannot depend on a menu
/// action that only one of them has.
///
/// Version 1 stored <c>Candle.SymbolId</c> as the primary key of the Symbol table in
/// CryptoScanBot.db. That main database is regularly deleted and rebuilt, and a symbol almost
/// never returns on the same autoincrement id — after such a rebuild every candle silently
/// belonged to a different coin, with no error anywhere. Version 2 resolves ids through the candle
/// database's own Symbol table, keyed by name, so the store no longer depends on that numbering.
///
/// The conversion does NOT renumber the candles. It writes the CURRENT name of each existing id
/// into the local Symbol table, keeping the id itself, so a multi-gigabyte file is converted in
/// seconds instead of being rewritten. From that moment on the name is the anchor.
///
/// Because it adopts the mapping as it stands, it is only correct while that mapping is intact.
/// <see cref="CheckMapping"/> establishes that first, and decides between adopting and discarding.
/// </summary>
public static class CandleDatabaseMigration
{
    /// <summary>
    /// Minimum percentage of symbols whose stored tick-decimals must match the exchange's current
    /// price tick size before the existing id mapping is trusted. Measured on a healthy database
    /// this sits around 96% (the remainder are symbols whose tick size the exchange changed after
    /// the candles were stored); a shifted or shuffled mapping scores in the low twenties, so
    /// anything below this threshold is a broken mapping rather than noise.
    /// </summary>
    public const double MinimumMappingMatchPercentage = 80.0;


    /// <summary>Outcome of <see cref="CheckMapping"/>, also used for the log line.</summary>
    public readonly record struct MappingCheck(
        int SymbolsInDatabase, int Matched, int Mismatched, int UnknownId, double MatchPercentage)
    {
        /// <summary>Number of symbols that could actually be judged (the rest have no symbol to compare against).</summary>
        public int Comparable => Matched + Mismatched;

        public bool IsTrustworthy => Comparable > 0 && MatchPercentage >= MinimumMappingMatchPercentage;

        public string Describe() =>
            $"{SymbolsInDatabase} symbol(s), {Matched} matching / {Mismatched} mismatching / " +
            $"{UnknownId} unknown id(s) → {MatchPercentage:F1}% (threshold {MinimumMappingMatchPercentage:F0}%)";
    }


    /// <summary>
    /// Judges whether the version-1 ids still point at the symbols they were written for, WITHOUT
    /// needing outside data such as a ticker snapshot.
    ///
    /// Every candle carries the tick-decimals of the symbol it was stored for (CreateCandle copies
    /// <c>symbol.PriceDecimals</c> into <c>TickDecimals</c>). Those decimals follow from the
    /// exchange's PriceTickSize, so for an intact mapping the stored value equals the current one
    /// for practically every symbol. Under a shifted mapping the two only agree by coincidence —
    /// different coins rarely share a tick size. Not conclusive per individual symbol, but decisive
    /// over hundreds of them, which is the scale this runs at.
    /// </summary>
    public static MappingCheck CheckMapping(SqliteConnection connection, Model.CryptoExchange exchange)
    {
        // Dominant tick-decimals per symbol id. An exchange that changes a tick size mid-life
        // leaves both values behind; the most common one is the representative sample.
        Dictionary<int, (long Count, int Decimals)> dominant = [];
        foreach (var row in connection.Query<(int SymbolId, int Ticks, long Count)>(
            "SELECT SymbolId, Ticks, COUNT(*) FROM Candle GROUP BY SymbolId, Ticks"))
        {
            // Low nibble only — the high bits carry the IsFilled flag (CryptoCandle.TickDecimalsRaw).
            int decimals = row.Ticks & 0x0F;
            if (!dominant.TryGetValue(row.SymbolId, out var current) || row.Count > current.Count)
                dominant[row.SymbolId] = (row.Count, decimals);
        }

        int matched = 0;
        int mismatched = 0;
        int unknown = 0;
        foreach (var (symbolId, entry) in dominant)
        {
            if (!exchange.SymbolListId.TryGetValue(symbolId, out CryptoSymbol? symbol))
            {
                unknown++;
                continue;
            }

            if (symbol.PriceDecimals == entry.Decimals)
                matched++;
            else
                mismatched++;
        }

        int comparable = matched + mismatched;
        double percentage = comparable > 0 ? 100.0 * matched / comparable : 0;
        return new MappingCheck(dominant.Count, matched, mismatched, unknown, percentage);
    }


    /// <summary>
    /// Converts an open version-1 database to version 2, on the connection the caller already has.
    /// Three outcomes:
    /// <list type="bullet">
    ///   <item>mapping intact → adopt it: register each id's current name, keep the ids, and drop
    ///         the candles of ids that no longer have a symbol (they became unreachable — nothing
    ///         can look them up by name any more);</item>
    ///   <item>mapping broken → discard the candles. They cannot be attributed to any symbol, so
    ///         keeping them has no value and re-fetching rebuilds them. Logged loudly.</item>
    ///   <item>nothing to compare against → throw, so the caller postpones. This happens when the
    ///         exchange's symbols are not loaded yet; judging (let alone discarding) on an empty
    ///         comparison would be the worst of both worlds.</item>
    /// </list>
    /// </summary>
    public static void ConvertInPlace(SqliteConnection connection, Model.CryptoExchange exchange)
    {
        MappingCheck check = CheckMapping(connection, exchange);

        if (check.Comparable == 0)
        {
            // No symbol to compare against — almost always because the exchange list has not been
            // fetched yet at this point in startup. Deciding now would mean discarding a healthy
            // database on no evidence at all.
            throw new CandleDatabaseSchemaException(
                $"Candle database for '{exchange.Name}' still uses schema version 1 and cannot be converted yet: " +
                $"none of its {check.SymbolsInDatabase} symbol id(s) could be compared against the exchange " +
                "(symbols not loaded yet). The candle store is skipped until they are.");
        }

        GlobalData.AddTextToLogTab($"candles.db {exchange.Name}: converting to schema version 2 — mapping check {check.Describe()}");

        if (!check.IsTrustworthy)
        {
            DiscardCandles(connection, exchange, check);
            return;
        }

        AdoptExistingMapping(connection, exchange);
    }


    /// <summary>
    /// Mapping is intact: register the current name for every id that occurs in Candle, keeping the
    /// id. Ids without a matching symbol are removed — after this conversion every read resolves by
    /// name, so candles under an id that has no name can never be reached again and would only take
    /// up space. AUTOINCREMENT continues above the highest adopted id, so newly fetched symbols
    /// cannot collide with one of them.
    /// </summary>
    private static void AdoptExistingMapping(SqliteConnection connection, Model.CryptoExchange exchange)
    {
        int registered = 0;
        int orphanIds = 0;
        long orphanRows = 0;

        using var tx = connection.BeginTransaction();
        foreach (int symbolId in connection.Query<int>("SELECT DISTINCT SymbolId FROM Candle").ToList())
        {
            if (exchange.SymbolListId.TryGetValue(symbolId, out CryptoSymbol? symbol))
            {
                connection.Execute(
                    "INSERT OR IGNORE INTO Symbol (SymbolId, Name) VALUES (@SymbolId, @Name)",
                    new { SymbolId = symbolId, symbol.Name }, transaction: tx);
                registered++;
            }
            else
            {
                orphanRows += connection.Execute(
                    "DELETE FROM Candle WHERE SymbolId = @SymbolId", new { SymbolId = symbolId }, transaction: tx);
                connection.Execute(
                    "DELETE FROM SymbolInterval WHERE SymbolId = @SymbolId", new { SymbolId = symbolId }, transaction: tx);
                orphanIds++;
            }
        }

        StampVersion(connection, tx, exchange);
        tx.Commit();

        CandleDatabase.ClearLocalSymbolIdCache(connection.DataSource);
        GlobalData.AddTextToLogTab(
            $"candles.db {exchange.Name}: schema version 2 — adopted {registered} symbol(s)" +
            (orphanIds > 0 ? $", removed {orphanRows} candle(s) of {orphanIds} id(s) without a symbol" : ""));
    }


    /// <summary>
    /// Mapping is broken: the ids no longer identify the symbols the candles were stored for, so
    /// there is no way to tell which coin any of it belongs to. Adopting it would make the wrong
    /// names permanent, which is worse than the current state where the damage is still detectable.
    /// The candles go; the file itself is kept (and reused) so the next fetch simply refills it.
    /// </summary>
    private static void DiscardCandles(SqliteConnection connection, Model.CryptoExchange exchange, MappingCheck check)
    {
        using var tx = connection.BeginTransaction();
        int candleRows = connection.Execute("DELETE FROM Candle", transaction: tx);
        connection.Execute("DELETE FROM SymbolInterval", transaction: tx);
        connection.Execute("DELETE FROM Symbol", transaction: tx);
        StampVersion(connection, tx, exchange);
        tx.Commit();

        CandleDatabase.ClearLocalSymbolIdCache(connection.DataSource);

        GlobalData.AddTextToLogTab(
            $"candles.db {exchange.Name}: DISCARDED {candleRows} candle(s). Only {check.MatchPercentage:F1}% of its " +
            $"symbol ids still match the exchange, so the candles could no longer be attributed to a symbol " +
            "(the main database was rebuilt after they were stored). Re-fetch the candles to rebuild the store.");
        ScannerLog.Logger.Warn(
            $"candles.db {exchange.Name}: discarded {candleRows} candles, broken id mapping — {check.Describe()}");
    }


    private static void StampVersion(SqliteConnection connection, SqliteTransaction tx, Model.CryptoExchange exchange)
    {
        // Deliberately the literal 2, not CurrentSchemaVersion: this conversion produces a Symbol
        // table keyed on the scanner name, which is exactly what version 2 is. VerifySchemaVersion
        // takes it from there to the current version.
        connection.Execute(
            "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('SchemaVersion', @Version)",
            new { Version = "2" }, transaction: tx);
        connection.Execute(
            "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('ExchangeName', @Name)",
            new { exchange.Name }, transaction: tx);
    }
}
