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
    public int Side { get; set; }
    public string? Strategy { get; set; }
    public int Status { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }
    public decimal? SignalPrice { get; set; }
    public decimal? SlPercentage { get; set; }
    public string? EventText { get; set; }
    public decimal Invested { get; set; }

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

    public string Duration => CloseTime.HasValue
        ? (CloseTime.Value - CreateTime).ToString(@"hh\:mm\:ss")
        : "—";

    // Side is stored as the CryptoTradeSide enum value (Long = 0, Short = 1) — show its name.
    public string SideText => ((CryptoTradeSide)Side).ToString();

    // Strategy holds the strategy name; database version 77 replaced the numeric column with it.
    public string StrategyText => Strategy ?? "?";

    // Status is stored as the CryptoPositionStatus enum value (Waiting/Trading/Ready/Timeout/
    // TakeOver/Altrady) — show its name instead of the raw int.
    public string StatusText => ((CryptoPositionStatus)Status).ToString();

    // Percentage with 2 decimals; Profit in the quote currency's own decimals.
    public string PercentageText => Percentage.ToString("N2");
    public string ProfitText => Profit.ToString(QuoteDisplayFormat);

    // Per-signal stop-loss distance (% from entry) carried over from the signal; blank when not set.
    public string SlPercentageText => SlPercentage.HasValue ? SlPercentage.Value.ToString("N2") : "—";

    // What the position put in, entry plus filled DCA's, in the quote currency
    public string InvestedText => Invested.ToString("N2");

    // Per-signal stop-loss distance (% from entry) carried over from the signal; blank when not set.
    public string SignalPriceText => SignalPrice.HasValue ? SignalPrice.Value.ToString("N2") : "—";



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
        Header = $"Positions for run #{run.Id}  ({run.Label}, started {run.StartedAt:yyyy-MM-dd HH:mm})";
        Load(run.Id);
    }


    private void Load(int runId)
    {
        try
        {
            using var database = new CryptoDatabase();
            database.Open();

            var rows = database.Connection.Query<PositionRow>(
                "SELECT p.Id, p.CreateTime, p.CloseTime, s.Name as Symbol, i.Name as Interval, " +
                "       p.Side, p.Strategy, p.Status, p.Profit, p.Percentage, " +
                "       p.SignalPrice, p.SlPercentage, p.EventText, " +
                "       p.PartCount, p.ActiveDca, p.Invested " +
                "FROM Position p " +
                "LEFT JOIN Symbol s ON s.Id = p.SymbolId " +
                "LEFT JOIN Interval i ON i.Id = p.IntervalId " +
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
                Positions.Add(row);
            }

            Status = Positions.Count == 0
                ? "This run produced no positions."
                : fromDigest
                    ? $"{Positions.Count} position(s), read from the digest of this run (it is not the latest run): " +
                      "price, stop and orders are not stored there, the percentage is the profit on what was invested."
                    : $"{Positions.Count} position(s).";
        }
        catch (Exception ex)
        {
            Status = $"Failed to load positions: {ex.Message}";
        }
    }


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
            strat = Column("strat"), iv = Column("iv"), eventText = Column("event");

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
                PartCount = Has(row, dca) ? row[dca].GetInt32() : 0,
                EventText = Has(row, eventText) ? row[eventText].GetString() : null,
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
            if (position.Invested > 0)
                position.Percentage = position.Profit / position.Invested * 100m;
            result.Add(position);
        }
        return result;
    }
}
