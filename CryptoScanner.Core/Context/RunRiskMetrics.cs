using CryptoScanner.Core.Model;

using System.Globalization;
using System.Text.Json;

namespace CryptoScanner.Core.Context;

/// <summary>
/// The three risk checks of the run report, stored on the run row as plain columns.
/// <para>
/// They were computed from the position digest by the report script and by the query
/// beste-runs.sql (json_each over the digest). Since database version 104 the digest is stored
/// compressed, and SQLite cannot read that - so the numbers are worked out once, here, and kept on
/// <see cref="Model.CryptoEmulatorRun"/>. Same rules as emulator_report.py (risk_checks):
/// </para>
/// <list type="bullet">
/// <item>closed trades are the digest rows with a close time, a profit and a positive invested amount;</item>
/// <item>a month is "in profit" when the trades that CLOSED in it add up to more than zero, counted
/// over every calendar month the run period touches;</item>
/// <item>the worst position is the lowest profit as a percentage of its own invested amount (0..100 scale);</item>
/// <item>the longest loser is the longest a losing position was open, in days (0 when nothing lost);</item>
/// <item>the profit without the ten best trades says how many trades the result rests on.</item>
/// </list>
/// </summary>
public readonly record struct RunRiskMetrics(int MonthsInProfit, int MonthsTotal, decimal? LongestLoserDays, decimal? WorstPositionPercentage,
    decimal ProfitWithoutBestTen)
{
    /// <summary>The metrics of one run, or null when its digest has no closed trades to judge.</summary>
    public static RunRiskMetrics? FromDigest(string? digestJson, DateTime fromDate, DateTime toDate)
    {
        if (string.IsNullOrEmpty(digestJson))
            return null;

        using JsonDocument document = JsonDocument.Parse(digestJson);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("cols", out JsonElement cols) || !root.TryGetProperty("rows", out JsonElement rows))
            return null;

        List<string?> names = [.. cols.EnumerateArray().Select(c => c.GetString())];
        int open = names.IndexOf("open"), close = names.IndexOf("close");
        int profitAt = names.IndexOf("profit"), investedAt = names.IndexOf("invested");
        if (open < 0 || close < 0 || profitAt < 0 || investedAt < 0)
            return null;

        List<string> months = MonthsInPeriod(fromDate, toDate);
        Dictionary<string, double> perMonth = [];
        double? worst = null;
        double longestLoser = 0;
        int trades = 0;
        List<double> profits = [];

        foreach (JsonElement row in rows.EnumerateArray())
        {
            double? closeMinutes = Value(row, close), profit = Value(row, profitAt), invested = Value(row, investedAt);
            if (closeMinutes == null || profit == null || invested == null || invested <= 0)
                continue;

            trades++;
            profits.Add(profit.Value);
            DateTime closedAt = CandleTime.Epoch.AddMinutes(closeMinutes.Value);
            string key = closedAt.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            perMonth[key] = perMonth.GetValueOrDefault(key) + profit.Value;

            double percentage = 100.0 * profit.Value / invested.Value;
            if (worst == null || percentage < worst)
                worst = percentage;

            double? openMinutes = Value(row, open);
            if (profit < 0 && openMinutes != null)
                longestLoser = Math.Max(longestLoser, (closeMinutes.Value - openMinutes.Value) / 1440.0);
        }

        if (trades == 0)
            return null;

        int monthsInProfit = months.Count(m => perMonth.GetValueOrDefault(m) > 0);
        double withoutBestTen = profits.Sum() - profits.OrderByDescending(p => p).Take(10).Sum();
        return new RunRiskMetrics(monthsInProfit, months.Count,
            Math.Round((decimal)longestLoser, 1), worst == null ? null : Math.Round((decimal)worst.Value, 1),
            Math.Round((decimal)withoutBestTen, 4));
    }


    /// <summary>Every calendar month from the month of <paramref name="fromDate"/> up to (not including) <paramref name="toDate"/>.</summary>
    internal static List<string> MonthsInPeriod(DateTime fromDate, DateTime toDate)
    {
        List<string> months = [];
        for (DateTime moment = new(fromDate.Year, fromDate.Month, 1); moment < toDate; moment = moment.AddMonths(1))
            months.Add(moment.ToString("yyyy-MM", CultureInfo.InvariantCulture));
        return months;
    }


    private static double? Value(JsonElement row, int index)
    {
        if (index >= row.GetArrayLength())
            return null;
        JsonElement value = row[index];
        return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
    }
}
