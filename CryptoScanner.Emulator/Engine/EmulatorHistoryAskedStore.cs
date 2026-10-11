using CryptoScanner.Core.Model;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Keeps, per symbol and interval, which periods the emulator's candle fetch has already asked the
/// exchange for (CryptoSymbolInterval.HistoryAsked), between sessions. Without it every fetch asked
/// again for what the exchange simply does not have: history before a symbol's listing (the 1w
/// interval of nearly every symbol, the higher intervals of young ones such as ROBO and BEAT) and
/// real holes in the exchange's data - about 180 requests per fetch (11-10-2026). A period that was
/// asked once counts as accepted, whatever came back.
/// <para>
/// Emulator only: its own table in the emulator's candle database, read and written by nothing else.
/// The live scanner never sees it. A new, earlier period (a replay window moved back in time) was
/// never asked, so it is still fetched.
/// </para>
/// </summary>
public static class EmulatorHistoryAskedStore
{
    public static void EnsureTable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "CREATE TABLE IF NOT EXISTS [EmulatorHistoryAsked] (" +
            "  SymbolName  TEXT    NOT NULL," +
            "  IntervalId  INTEGER NOT NULL," +
            "  FromTime    INTEGER NOT NULL," +
            "  ToTime      INTEGER NOT NULL," +
            "  PRIMARY KEY (SymbolName, IntervalId, FromTime)" +
            ") WITHOUT ROWID";
        cmd.ExecuteNonQuery();
    }


    /// <summary>Puts the stored periods of this symbol back into its intervals.</summary>
    public static void Load(SqliteConnection connection, CryptoSymbol symbol)
    {
        Dictionary<int, CryptoSymbolInterval> byIntervalId = [];
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            byIntervalId[symbolInterval.Interval.Id] = symbolInterval;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT IntervalId, FromTime, ToTime FROM EmulatorHistoryAsked WHERE SymbolName = $Name";
        cmd.Parameters.AddWithValue("$Name", symbol.Name);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (!byIntervalId.TryGetValue(reader.GetInt32(0), out CryptoSymbolInterval? symbolInterval))
                continue;
            symbolInterval.RememberHistoryAsked(new CandleTime((uint)reader.GetInt64(1)), new CandleTime((uint)reader.GetInt64(2)));
        }
    }


    /// <summary>Stores the periods of this symbol as they are in memory now (replacing what was stored).</summary>
    public static void Save(SqliteConnection connection, CryptoSymbol symbol)
    {
        using var tx = connection.BeginTransaction();
        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = tx;
            delete.CommandText = "DELETE FROM EmulatorHistoryAsked WHERE SymbolName = $Name";
            delete.Parameters.AddWithValue("$Name", symbol.Name);
            delete.ExecuteNonQuery();
        }

        using var insert = connection.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = "INSERT OR REPLACE INTO EmulatorHistoryAsked (SymbolName, IntervalId, FromTime, ToTime) VALUES ($Name, $Interval, $From, $To)";
        var pName = insert.Parameters.Add("$Name", SqliteType.Text);
        var pInterval = insert.Parameters.Add("$Interval", SqliteType.Integer);
        var pFrom = insert.Parameters.Add("$From", SqliteType.Integer);
        var pTo = insert.Parameters.Add("$To", SqliteType.Integer);
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            foreach (var (from, to) in symbolInterval.HistoryAsked.Snapshot())
            {
                pName.Value = symbol.Name;
                pInterval.Value = symbolInterval.Interval.Id;
                pFrom.Value = (long)from.Minutes;
                pTo.Value = (long)to.Minutes;
                insert.ExecuteNonQuery();
            }
        }
        tx.Commit();
    }
}
