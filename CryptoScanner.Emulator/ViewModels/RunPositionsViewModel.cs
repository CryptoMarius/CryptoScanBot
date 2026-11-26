using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper;

using System.Collections.ObjectModel;
using System.Text.Json;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// One position row in the run-detail grid. Only the columns the user typically scans first;
/// the full CryptoPosition has dozens of fields most of which aren't useful here.
/// </summary>
public class PositionRow
{
    public int Id { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? CloseTime { get; set; }
    public string? Symbol { get; set; }
    public string? Interval { get; set; }
    // The interval the chart opens on: the zone interval of the position when it has one (fvgladder)
    public string? ChartInterval { get; set; }
    public int Side { get; set; }
    public string? Strategy { get; set; }
    public int Status { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }
    public decimal? SignalPrice { get; set; }
    public decimal? SlPercentage { get; set; }
    public string? EventText { get; set; }
    public decimal Invested { get; set; }
    // The funding settled on the position (received +, paid -), already part of Profit
    public decimal? Funding { get; set; }

    // What a position still open at the end of the run is worth, valued at the price of the run's
    // last snapshot day like the run's Unrealized PnL (PositionDigest "openPnl"). Null for a closed
    // position, an entry that never filled, and runs written before 26-11-2026.
    public decimal? OpenPnl { get; set; }

    // The moment the replay stopped (the exclusive ToDate of a completed run), so an open position
    // gets a duration up to the end of the run. Null for a run that was stopped before its end.
    public DateTime? RunEnd { get; set; }

    // Part composition, persisted on Position: PartCount = number of filled DCA parts (entry excluded,
    // a pending unfilled DCA excluded); ActiveDca = an open DCA is still standing. Used by PartsText.
    public int PartCount { get; set; }
    public bool ActiveDca { get; set; }

    // .NET numeric format of the quote currency (e.g. "N8"), set from the symbol's QuoteData when the
    // rows are loaded. Profit is in quote currency, so it is shown with the quote's own decimals.
    public string QuoteDisplayFormat { get; set; } = "N8";

    // Pre-formatted timestamp strings for the grid. Bound as plain strings instead of a StringFormat
    // on the column binding: a DataGridTextColumn.Binding with a StringFormat over a DateTime source
    // sets up a converting binding that tries to parse the formatted text back to DateTime on every
    // cell realization, throwing a caught FormatException / TypeConverter ArgumentException per cell —
    // which floods the debugger and stutters scrolling. Same approach as the other *Text columns here.
    // CreateTime/CloseTime are stored as UTC (candle close time), but SQLite/Dapper returns them
    // with DateTimeKind.Unspecified. SpecifyKind(..., Utc) marks them so ToLocalTime() shifts to the
    // machine's timezone instead of treating the value as already-local. Matches the signals/results
    // grids so every grid shows local time consistently.
    public string CreatedText => DateTime.SpecifyKind(CreateTime, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string ClosedText => CloseTime.HasValue
        ? DateTime.SpecifyKind(CloseTime.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        : "—";

    //public string Duration => CloseTime.HasValue
    //    ? (CloseTime.Value - CreateTime).ToString(@"hh\:mm\:ss")
    //    : "—";
    // "hh" is only the hours part (0-23), so a position of 3 days and 2 hours read 02:00:00. Since
    // 31-10-2026 the days are shown the way the results grid shows its durations (d.hh:mm:ss).
    public string Duration
    {
        get
        {
            if (DurationSpan is not TimeSpan span)
                return "—";
            return span.Days > 0
                ? span.ToString(@"d\.hh\:mm\:ss")
                : span.ToString(@"hh\:mm\:ss");
        }
    }

    // The sort key of the Duration column: sorting on the text put 1.00:00:00 before 23:00:00.
    //public TimeSpan? DurationSpan => CloseTime.HasValue ? CloseTime.Value - CreateTime : null;
    // An open position counts up to the end of the run since 26-11-2026: how long it has been stuck
    // is what the end of a run is read for. The Status column (Trading) says it is still open.
    public TimeSpan? DurationSpan => CloseTime.HasValue
        ? CloseTime.Value - CreateTime
        : RunEnd.HasValue && RunEnd.Value > CreateTime ? RunEnd.Value - CreateTime : null;

    // Side is stored as the CryptoTradeSide enum value (Long = 0, Short = 1) — show its name.
    public string SideText => ((CryptoTradeSide)Side).ToString();

    // Strategy holds the strategy name; database version 77 replaced the numeric column with it.
    public string StrategyText => Strategy ?? "?";

    // Status is stored as the CryptoPositionStatus enum value (Waiting/Trading/Ready/Timeout/
    // TakeOver/Altrady) — show its name instead of the raw int.
    public string StatusText => ((CryptoPositionStatus)Status).ToString();

    // Percentage with 2 decimals; Profit in the quote currency's own decimals.
    //public string PercentageText => Percentage.ToString("N2");
    //public string ProfitText => Profit.ToString(QuoteDisplayFormat);
    // A position still open at the end of the run has sold nothing yet, so its stored profit is
    // minus the whole stake (Returned 0 - Invested - Commission) and read as a loss of 100%.
    // Neither the Position table nor the digest holds a price at the end of the run, so no value
    // is shown for an open position; its worth is in Unrealized PnL of the results grid.
    // Note 23-11-2026: for an open SHORT it is the other way round - Invested - Returned 0 - Commission
    // is plus the whole stake, so the Percentage read about 200 (run 2127, every open short).
    public bool IsOpen => !CloseTime.HasValue;
    // A position whose entry never filled (Timeout, Cancelled) put nothing in: Percentage stays 0, which on
    // the 100 = break-even scale reads as a total loss, so it gets a dash as well. Its Profit (0) is true.
    //public bool HasPercentage => !IsOpen && Invested > 0;
    //public string PercentageText => HasPercentage ? Percentage.ToString("N2") : "—";
    //public string ProfitText => IsOpen ? "—" : Profit.ToString(QuoteDisplayFormat);
    // Since 26-11-2026 an open position shows its value at the end of the run (OpenPnl) instead of a
    // dash, so the worst of the open positions can be sorted to the top. Still a dash when that value
    // is not known (runs from before that day). Status stays Trading, so it is not read as a TP or stop.
    public decimal? ShownProfit => IsOpen ? OpenPnl : Profit;
    public bool HasPercentage => Invested > 0 && ShownProfit.HasValue;
    public decimal ShownPercentage => IsOpen && OpenPnl.HasValue && Invested > 0
        ? 100m + (100m * OpenPnl.Value / Invested)
        : Percentage;
    public string PercentageText => HasPercentage ? ShownPercentage.ToString("N2") : "—";
    public string ProfitText => ShownProfit.HasValue ? ShownProfit.Value.ToString(QuoteDisplayFormat) : "—";

    // Sort keys of the Profit and Percentage columns: a position without a value sorts apart
    //public decimal? ProfitSort => IsOpen ? null : Profit;
    //public decimal? PercentageSort => HasPercentage ? Percentage : null;
    public decimal? ProfitSort => ShownProfit;
    public decimal? PercentageSort => HasPercentage ? ShownPercentage : null;

    // Per-signal stop-loss distance (% from entry) carried over from the signal; blank when not set.
    public string SlPercentageText => SlPercentage.HasValue ? SlPercentage.Value.ToString("N2") : "—";

    // What the position put in, entry plus filled DCA's, in the quote currency
    public string InvestedText => Invested.ToString("N2");

    // The funding in the quote currency's own decimals; a dash when the position had none
    public string FundingText => Funding.HasValue && Funding.Value != 0 ? Funding.Value.ToString(QuoteDisplayFormat) : "—";

    // Per-signal stop-loss distance (% from entry) carried over from the signal; blank when not set.
    // Note 31-10-2026: the line above is a copy of the stop-loss comment; this is the signal price the
    // position was opened on, a dash when not stored.
    // Formatted with the quote's display format since 31-10-2026, like the Signals window: with "N2" a
    // coin of 0.0000123 read 0.00.
    //public string SignalPriceText => SignalPrice.HasValue ? SignalPrice.Value.ToString("N2") : "—";
    public string SignalPriceText => SignalPrice.HasValue ? SignalPrice.Value.ToString(QuoteDisplayFormat) : "—";



    // Number of parts (entry + filled DCAs), with a trailing "+" when an open DCA is still pending.
    // Mirrors CryptoPosition.PartCountText() so the emulator overview matches the live open-positions grid.
    public string PartsText
    {
        get
        {
            string text = (PartCount + 1).ToString(); // entry counts as 1
            if (ActiveDca && !CloseTime.HasValue)
                text += "+";
            return text;
        }
    }
}


/// <summary>
/// Backs <c>RunPositionsWindow</c>. Joins Position with Symbol + Interval so the user sees
/// names instead of FK integers. Filters on EmulatorRunId so each window only ever shows
/// positions of one run.
/// </summary>
public partial class RunPositionsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _header = "";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private ObservableCollection<PositionRow> _positions = [];

    /// <summary>The run this grid belongs to; passed to the chart so it only shows this run's data.</summary>
    public int RunId { get; }


    public RunPositionsViewModel(RunRow run)
    {
        RunId = run.Id;
        // Local time like the Started column of the results grid (StartedAt is stored in UTC), since 31-10-2026
        //Header = $"Positions for run #{run.Id}  ({run.Label}, started {run.StartedAt:yyyy-MM-dd HH:mm})";
        Header = $"Positions for run #{run.Id}  ({run.Label}, started {run.StartedLocal})";
        //Load(run.Id);
        // The end of the replay is the exclusive ToDate (ReplayRunner), but only for a run that got
        // there; a cancelled or failed run stopped somewhere before it.
        DateTime? runEnd = run.Result == "completed" ? run.ToDate : null;
        Load(run.Id, runEnd);
    }


    private void Load(int runId, DateTime? runEnd)
    {
        try
        {
            using var database = new CryptoDatabase();
            database.Open();

            var rows = database.Connection.Query<PositionRow>(
                "SELECT p.Id, p.CreateTime, p.CloseTime, s.Name as Symbol, i.Name as Interval, coalesce(zi.Name, i.Name) as ChartInterval, " +
                "       p.Side, p.Strategy, p.Status, p.Profit, p.Percentage, " +
                "       p.SignalPrice, p.SlPercentage, p.EventText, " +
                "       p.PartCount, p.ActiveDca, p.Invested, p.Funding " +
                "FROM Position p " +
                "LEFT JOIN Symbol s ON s.Id = p.SymbolId " +
                "LEFT JOIN Interval i ON i.Id = p.IntervalId " +
                "LEFT JOIN Interval zi ON zi.Id = p.ZoneIntervalId " +
                "WHERE p.EmulatorRunId = @runId " +
                "ORDER BY p.CreateTime",
                new { runId });

            // The Position table only holds the positions of the latest run: it is cleared before the
            // next one starts. For an older run the positions come from the digest that is stored on
            // the run row - fewer columns, but the same positions.
            bool fromDigest = false;
            if (rows.AsList().Count == 0)
            {
                rows = LoadFromDigest(database, runId);
                fromDigest = rows.AsList().Count != 0;
            }
            else
            {
                // The latest run: the value of the open positions with the digest's own query
                Dictionary<int, decimal> openPnl = PositionDigest.OpenPnlByPosition(database, runId);
                foreach (var row in rows)
                {
                    if (openPnl.TryGetValue(row.Id, out decimal value))
                        row.OpenPnl = value;
                }
            }

            // Attach the quote currency's display format per row so Profit shows the quote's own
            // decimals. The symbols of the run's exchange are loaded in memory (with their QuoteData);
            // fall back to the default format when the symbol isn't found.
            var exchange = GlobalData.ActiveExchange;
            foreach (var row in rows)
            {
                if (exchange != null && row.Symbol != null
                    && exchange.TryGetSymbolByPair(row.Symbol, out var symbol) && symbol.QuoteData != null)
                {
                    row.QuoteDisplayFormat = symbol.QuoteData.DisplayFormat;
                }
                row.RunEnd = runEnd;
                Positions.Add(row);
            }

            Status = Positions.Count == 0
                ? "This run produced no positions."
                : fromDigest
                    //? $"{Positions.Count} position(s), read from the digest of this run (it is not the latest run): " +
                    //  "price, stop and orders are not stored there, the percentage is worked out from profit and invested (100 = break-even)."
                    //: $"{Positions.Count} position(s).";
                    ? $"{Positions.Count} position(s), read from the digest of this run (it is not the latest run): " +
                      "orders are not stored there, the percentage is worked out from profit and invested (100 = break-even). " +
                      OpenPositionsNote + " Runs from before 26-11-2026 have no price, stop-loss or value of the open positions in their digest."
                    : $"{Positions.Count} position(s). " + OpenPositionsNote;
        }
        catch (Exception ex)
        {
            Status = $"Failed to load positions: {ex.Message}";
        }
    }


    // What Profit, Percentage and Duration mean for a position still open (status Trading)
    private const string OpenPositionsNote =
        "Open positions (Trading) show their value at the end of the run, at the price of the last replayed minute " +
        "(the same as Unrealized PnL), and their duration up to the end of the run.";


    /// <summary>
    /// The positions of a run from its PositionDigestJson (see Core PositionDigest): symbol, side,
    /// times, profit, invested, filled DCA's, status, strategy, interval and the signal text. The
    /// columns are looked up by name, so a digest with more or fewer columns still reads. Empty when
    /// the run has no digest.
    /// </summary>
    private static List<PositionRow> LoadFromDigest(CryptoDatabase database, int runId)
    {
        List<PositionRow> result = [];
        string? stored = database.Connection.ExecuteScalar<string?>(
            "SELECT PositionDigestJson FROM EmulatorRun WHERE Id = @runId", new { runId });
        string? json = PositionDigest.Unpack(stored);
        if (string.IsNullOrEmpty(json))
            return result;

        Dictionary<int, string> symbols = database.Connection
            .Query<(int Id, string Name)>("SELECT Id, Name FROM Symbol").ToDictionary(s => s.Id, s => s.Name);
        Dictionary<int, string> intervals = database.Connection
            .Query<(int Id, string Name)>("SELECT Id, Name FROM Interval").ToDictionary(i => i.Id, i => i.Name);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        List<string> columns = root.GetProperty("cols").EnumerateArray().Select(c => c.GetString() ?? "").ToList();
        int Column(string name) => columns.IndexOf(name);
        int sym = Column("sym"), side = Column("side"), open = Column("open"), close = Column("close"),
            profit = Column("profit"), invested = Column("invested"), dca = Column("dca"), status = Column("status"),
            strat = Column("strat"), iv = Column("iv"), eventText = Column("event"), funding = Column("funding"),
            // Appended 26-11-2026; -1 (absent) in older digests, which leaves the columns on a dash
            price = Column("price"), sl = Column("sl"), openPnl = Column("openPnl");

        static bool Has(JsonElement row, int index) =>
            index >= 0 && index < row.GetArrayLength() && row[index].ValueKind != JsonValueKind.Null;

        int number = 0;
        foreach (JsonElement row in root.GetProperty("rows").EnumerateArray())
        {
            number++;
            PositionRow position = new()
            {
                // The digest keeps no position id; the order is the order of the run
                Id = number,
                Side = Has(row, side) ? row[side].GetInt32() : 0,
                Status = Has(row, status) ? row[status].GetInt32() : 0,
                Strategy = Has(row, strat) ? row[strat].GetString() : null,
                Profit = Has(row, profit) ? row[profit].GetDecimal() : 0m,
                Invested = Has(row, invested) ? row[invested].GetDecimal() : 0m,
                Funding = Has(row, funding) ? row[funding].GetDecimal() : null,
                PartCount = Has(row, dca) ? row[dca].GetInt32() : 0,
                EventText = Has(row, eventText) ? row[eventText].GetString() : null,
                SignalPrice = Has(row, price) ? row[price].GetDecimal() : null,
                SlPercentage = Has(row, sl) ? row[sl].GetDecimal() : null,
                OpenPnl = Has(row, openPnl) ? row[openPnl].GetDecimal() : null,
            };
            if (Has(row, sym) && symbols.TryGetValue(row[sym].GetInt32(), out string? symbolName))
                position.Symbol = symbolName;
            if (Has(row, iv) && intervals.TryGetValue(row[iv].GetInt32(), out string? intervalName))
                position.Interval = intervalName;
            // Minutes since CandleTime.Epoch, stored as UTC like the Position table
            if (Has(row, open))
                position.CreateTime = DateTime.SpecifyKind(CandleTime.Epoch.AddMinutes(row[open].GetInt64()), DateTimeKind.Unspecified);
            if (Has(row, close))
                position.CloseTime = DateTime.SpecifyKind(CandleTime.Epoch.AddMinutes(row[close].GetInt64()), DateTimeKind.Unspecified);
            //if (position.Invested > 0)
            //    position.Percentage = position.Profit / position.Invested * 100m;
            // On the scale of the Position column since 31-10-2026 (TradeTools: 100 = break-even, the
            // hint of the column says so): the digest gave 0 = break-even, so 1.50 read as a loss of 98.5%.
            if (position.Invested > 0)
                position.Percentage = 100m + (100m * position.Profit / position.Invested);
            result.Add(position);
        }
        return result;
    }
}
