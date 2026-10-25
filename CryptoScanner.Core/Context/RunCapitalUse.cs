using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;

using System.Data;
using System.Text.Json;

namespace CryptoScanner.Core.Context;

/// <summary>
/// How much of the account was at work in trades during an emulator run - the Exposure of
/// AmiBroker's backtest report - stored on the run row since database version 110.
/// <list type="bullet">
/// <item><see cref="AvgInvested"/>: the capital in trades averaged over the whole run period. Every
/// filled entry and DCA order counts from the moment it filled until its position closed (or the
/// period ended), so a DCA only counts once it was bought.</item>
/// <item><see cref="ExposurePercentage"/>: that average as a percentage of the average account value
/// (the equity curve), 0..100 scale.</item>
/// <item><see cref="TimeInMarketPercentage"/>: the part of the period with at least one position open,
/// however small.</item>
/// </list>
/// <para>
/// The exact figures need the fills, so only a run that still has its positions gets them. For the
/// older runs the position digest gives an estimate (<see cref="Estimated"/>): it only knows the full
/// invested amount and the moment the entry order was PLACED, so it counts every DCA from the entry
/// on and lands too high - on HL run 117 3.036 against 2.396 exactly, 27% over.
/// </para>
/// </summary>
public readonly record struct RunCapitalUse(decimal AvgInvested, decimal? ExposurePercentage, decimal TimeInMarketPercentage, bool Estimated)
{
    /// <summary>
    /// Every filled entry and DCA order of the traded positions of one run: the buys of a long, the
    /// sells of a short. Only the filled statuses - a cancelled DCA step can carry a
    /// QuoteQuantityFilled without ever having been bought.
    /// </summary>
    private const string FilledEntriesSql =
        "select s.CloseTime as FilledAt, p.CloseTime as PositionClosedAt, CAST(s.QuoteQuantityFilled as REAL) as Quote " +
        "from PositionStep s join Position p on p.Id = s.PositionId " +
        "where p.EmulatorRunId = @id and CAST(p.Invested as REAL) > 0 " +
        "  and s.Status in (@partiallyAndClosed, @filled) and CAST(s.QuoteQuantityFilled as REAL) > 0 " +
        "  and ((p.Side = @longSide and s.Side = @buy) or (p.Side = @shortSide and s.Side = @sell))";


    /// <summary>The exact figures from the fills of a run that still has its positions.</summary>
    public static RunCapitalUse? FromFills(IDbConnection connection, int runId, DateTime fromDate, DateTime toDate, decimal? averageAccountValue)
    {
        List<Piece> pieces = [];
        foreach (FillRow row in connection.Query<FillRow>(FilledEntriesSql, new
        {
            id = runId,
            partiallyAndClosed = (int)CryptoOrderStatus.PartiallyAndClosed,
            filled = (int)CryptoOrderStatus.Filled,
            longSide = (int)CryptoTradeSide.Long,
            shortSide = (int)CryptoTradeSide.Short,
            buy = (int)CryptoOrderSide.Buy,
            sell = (int)CryptoOrderSide.Sell,
        }))
        {
            if (row.FilledAt == null || row.Quote == null)
                continue;
            pieces.Add(new Piece(row.FilledAt.Value, row.PositionClosedAt ?? toDate, row.Quote.Value));
        }

        return Compute(pieces, fromDate, toDate, averageAccountValue, estimated: false);
    }


    /// <summary>The estimate from the position digest, for a run whose positions are gone; null without a digest.</summary>
    public static RunCapitalUse? FromDigest(string? digestJson, DateTime fromDate, DateTime toDate, decimal? averageAccountValue)
    {
        if (string.IsNullOrEmpty(digestJson))
            return null;

        using JsonDocument document = JsonDocument.Parse(digestJson);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("cols", out JsonElement cols) || !root.TryGetProperty("rows", out JsonElement rows))
            return null;

        List<string?> names = [.. cols.EnumerateArray().Select(c => c.GetString())];
        int open = names.IndexOf("open"), close = names.IndexOf("close"), investedAt = names.IndexOf("invested");
        if (open < 0 || close < 0 || investedAt < 0)
            return null;

        List<Piece> pieces = [];
        foreach (JsonElement row in rows.EnumerateArray())
        {
            double? openMinutes = Value(row, open), closeMinutes = Value(row, close), invested = Value(row, investedAt);
            if (openMinutes == null || invested == null || invested <= 0)
                continue;
            DateTime until = closeMinutes == null ? toDate : CandleTime.Epoch.AddMinutes(closeMinutes.Value);
            pieces.Add(new Piece(CandleTime.Epoch.AddMinutes(openMinutes.Value), until, invested.Value));
        }

        return Compute(pieces, fromDate, toDate, averageAccountValue, estimated: true);
    }


    /// <summary>The average of the daily values of a stored equity curve (CryptoEmulatorRun.EquityCurveJson); null without one.</summary>
    public static decimal? AverageAccountValue(string? equityCurveJson)
    {
        if (string.IsNullOrEmpty(equityCurveJson))
            return null;

        using JsonDocument document = JsonDocument.Parse(equityCurveJson);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("cols", out JsonElement cols) || !root.TryGetProperty("rows", out JsonElement rows))
            return null;

        int valueAt = cols.EnumerateArray().Select(c => c.GetString()).ToList().IndexOf("value");
        if (valueAt < 0)
            return null;

        List<double> values = [];
        foreach (JsonElement row in rows.EnumerateArray())
        {
            double? value = Value(row, valueAt);
            if (value != null)
                values.Add(value.Value);
        }
        return values.Count == 0 ? null : (decimal)values.Average();
    }


    /// <summary>One amount of capital in a trade, from a moment until a later one.</summary>
    internal readonly record struct Piece(DateTime From, DateTime Until, double Amount);


    /// <summary>
    /// Average capital, exposure and time in market over the period from a set of pieces, each cut
    /// to the period. Null for an empty period. A run without trades gives zeros: nothing was at work.
    /// </summary>
    internal static RunCapitalUse? Compute(IEnumerable<Piece> pieces, DateTime fromDate, DateTime toDate, decimal? averageAccountValue, bool estimated)
    {
        double periodDays = (toDate - fromDate).TotalDays;
        if (periodDays <= 0)
            return null;

        double capitalDays = 0;
        List<(DateTime From, DateTime Until)> spans = [];
        foreach (Piece piece in pieces)
        {
            DateTime from = piece.From < fromDate ? fromDate : piece.From;
            DateTime until = piece.Until > toDate ? toDate : piece.Until;
            if (until <= from)
                continue;
            capitalDays += piece.Amount * (until - from).TotalDays;
            spans.Add((from, until));
        }

        // Time in market: the spans merged, so two positions open at the same time count once.
        double busyDays = 0;
        DateTime? start = null, end = null;
        foreach ((DateTime from, DateTime until) in spans.OrderBy(s => s.From))
        {
            if (end == null || from > end)
            {
                if (end != null)
                    busyDays += (end.Value - start!.Value).TotalDays;
                start = from;
                end = until;
            }
            else if (until > end)
                end = until;
        }
        if (end != null)
            busyDays += (end.Value - start!.Value).TotalDays;

        decimal average = (decimal)(capitalDays / periodDays);
        decimal? exposure = averageAccountValue > 0 ? Math.Round(100m * average / averageAccountValue.Value, 4) : null;
        return new RunCapitalUse(Math.Round(average, 2), exposure, Math.Round((decimal)(100.0 * busyDays / periodDays), 2), estimated);
    }


    private static double? Value(JsonElement row, int index)
    {
        if (index >= row.GetArrayLength())
            return null;
        JsonElement value = row[index];
        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
    }


    private sealed class FillRow
    {
        public DateTime? FilledAt { get; set; }
        public DateTime? PositionClosedAt { get; set; }
        public double? Quote { get; set; }
    }
}
