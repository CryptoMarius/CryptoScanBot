using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Context;

/// <summary>
/// The funding settlements of the perpetuals, next to their candles: market data of the same symbol,
/// keyed on the same local symbol id. FundingTime is unix milliseconds (UTC), Rate and MarkPrice are
/// stored as text so the decimals come back exactly as the exchange sent them.
/// </summary>
public partial class CandleDatabase
{
    /// <summary>Creates the Funding table when it is not there yet. Cheap, safe to call on every fetch.</summary>
    public static void EnsureFundingTable(SqliteConnection connection)
    {
        connection.Execute(
            "CREATE TABLE IF NOT EXISTS [Funding] (" +
            "  SymbolId     INTEGER NOT NULL," +
            "  FundingTime  INTEGER NOT NULL," + // unix milliseconds, UTC
            "  Rate         TEXT NOT NULL," +
            "  MarkPrice    TEXT NOT NULL," +
            "  PRIMARY KEY (SymbolId, FundingTime)" +
            ") WITHOUT ROWID");
    }


    /// <summary>The newest settlement stored for the symbol, null when there is none.</summary>
    public static DateTime? LastFundingTime(SqliteConnection connection, CryptoSymbol symbol)
    {
        if (!TryGetLocalSymbolId(connection, symbol, out int localSymbolId))
            return null;
        long? ms = connection.ExecuteScalar<long?>("SELECT MAX(FundingTime) FROM Funding WHERE SymbolId = $SymbolId",
            new { SymbolId = localSymbolId });
        return ms.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(ms.Value).UtcDateTime : null;
    }


    /// <summary>Stores the settlements; one that is already there is overwritten.</summary>
    public static void SaveFunding(SqliteConnection connection, CryptoSymbol symbol, IReadOnlyList<CryptoFundingRate> rates)
    {
        if (rates.Count == 0)
            return;

        // Registered before the transaction, see ResolveLocalSymbolId
        int localSymbolId = ResolveLocalSymbolId(connection, symbol);
        using var transaction = connection.BeginTransaction();
        foreach (CryptoFundingRate rate in rates)
        {
            connection.Execute("INSERT OR REPLACE INTO Funding (SymbolId, FundingTime, Rate, MarkPrice) VALUES ($SymbolId, $FundingTime, $Rate, $MarkPrice)",
                new
                {
                    SymbolId = localSymbolId,
                    FundingTime = new DateTimeOffset(DateTime.SpecifyKind(rate.FundingTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                    Rate = rate.Rate.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    MarkPrice = rate.MarkPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                }, transaction);
        }
        transaction.Commit();
    }


    /// <summary>
    /// The close and the volume of every candle of <paramref name="interval"/> from
    /// <paramref name="fromMinutes"/> on, for EVERY symbol in this database, keyed on the instrument
    /// (Symbol.ExchangeName). For a calculation over the whole market (the ranking of a cross-sectional
    /// strategy), not for a replay: it reads past the emulator clock on purpose, the caller decides which
    /// candles it may use.
    /// </summary>
    public static Dictionary<string, List<(uint OpenTime, decimal Close, decimal Volume)>> LoadCloseAndVolumeForAll(
        SqliteConnection connection, CryptoInterval interval, uint fromMinutes)
    {
        Dictionary<string, List<(uint OpenTime, decimal Close, decimal Volume)>> result = new(StringComparer.OrdinalIgnoreCase);
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT s.ExchangeName, c.OpenTime, c.Ticks, c.Close, c.Volume " +
            "FROM Candle c JOIN Symbol s ON s.SymbolId = c.SymbolId " +
            "WHERE c.IntervalId = $IntervalId AND c.OpenTime >= $From ORDER BY c.SymbolId, c.OpenTime";
        var pInterval = cmd.CreateParameter();
        pInterval.ParameterName = "$IntervalId";
        pInterval.Value = interval.Id;
        cmd.Parameters.Add(pInterval);
        var pFrom = cmd.CreateParameter();
        pFrom.ParameterName = "$From";
        pFrom.Value = (long)fromMinutes;
        cmd.Parameters.Add(pFrom);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string instrument = reader.GetString(0);
            uint openTime = (uint)reader.GetInt64(1);
            decimal tickSize = TickSizeFor((byte)(reader.GetInt32(2) & 0x0F));
            decimal close = reader.GetInt64(3) * tickSize;
            decimal volume = (decimal)reader.GetDouble(4);
            if (!result.TryGetValue(instrument, out var list))
            {
                list = [];
                result[instrument] = list;
            }
            list.Add((openTime, close, volume));
        }
        return result;
    }


    /// <summary>
    /// The key a symbol is stored under in this database (its exchange instrument), for matching the
    /// result of <see cref="LoadCloseAndVolumeForAll"/> with the symbols of the exchange.
    /// </summary>
    public static string InstrumentKey(CryptoSymbol symbol) => InstrumentKeyFor(symbol);


    /// <summary>
    /// The settlements of the symbol from <paramref name="fromTime"/> up to <paramref name="toTime"/>
    /// (both inclusive), oldest first. Empty when the symbol has none.
    /// </summary>
    public static List<CryptoFundingRate> LoadFunding(SqliteConnection connection, CryptoSymbol symbol, DateTime fromTime, DateTime toTime)
    {
        List<CryptoFundingRate> result = [];
        if (!TryGetLocalSymbolId(connection, symbol, out int localSymbolId))
            return result;

        var rows = connection.Query<(long FundingTime, string Rate, string MarkPrice)>(
            "SELECT FundingTime, Rate, MarkPrice FROM Funding WHERE SymbolId = $SymbolId AND FundingTime BETWEEN $From AND $To ORDER BY FundingTime",
            new
            {
                SymbolId = localSymbolId,
                From = new DateTimeOffset(DateTime.SpecifyKind(fromTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                To = new DateTimeOffset(DateTime.SpecifyKind(toTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
            });
        foreach (var (fundingTime, rate, markPrice) in rows)
        {
            result.Add(new CryptoFundingRate(DateTimeOffset.FromUnixTimeMilliseconds(fundingTime).UtcDateTime,
                decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture),
                decimal.Parse(markPrice, System.Globalization.CultureInfo.InvariantCulture)));
        }
        return result;
    }
}
