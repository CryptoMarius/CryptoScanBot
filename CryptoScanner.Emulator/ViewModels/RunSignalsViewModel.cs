using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

using Dapper;

using System.Collections.ObjectModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// One signal row in the run-signals grid, with the position it produced (if any) joined in so the
/// operator can correlate "signal fired here → position created there → this was the result". The
/// position columns are null when the signal never became a position (filtered out / invalid).
/// </summary>
public class SignalRow
{
    public DateTime SignalTime { get; set; }      // Signal.OpenDate (candle that triggered it)
    public string? Symbol { get; set; }
    public string? Interval { get; set; }
    public int Side { get; set; }
    public int Strategy { get; set; }
    public decimal SignalPrice { get; set; }
    public decimal? SlPercentage { get; set; }
    public int IsInvalid { get; set; }
    public string? EventText { get; set; }

    // Correlated position (LEFT JOIN on symbol + SignalEventTime). Null when no position followed.
    public int? PositionId { get; set; }
    public DateTime? PositionCreateTime { get; set; }
    public int? PositionStatus { get; set; }
    public decimal? Profit { get; set; }
    public decimal? Percentage { get; set; }

    public string QuoteDisplayFormat { get; set; } = "N8";

    public string SignalLocal => DateTime.SpecifyKind(SignalTime, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string SideText => ((CryptoTradeSide)Side).ToString();
    public string StrategyText => RegisterAlgorithms.GetAlgorithm((CryptoSignalStrategy)Strategy);
    public string SignalPriceText => SignalPrice.ToString(QuoteDisplayFormat);
    public string SlPercentageText => SlPercentage.HasValue ? SlPercentage.Value.ToString("N2") : "—";
    public string ValidText => IsInvalid != 0 ? "invalid" : "ok";

    public string PositionText => PositionId.HasValue ? $"#{PositionId.Value}" : "—";
    public string PositionCreatedLocal => PositionCreateTime.HasValue
        ? DateTime.SpecifyKind(PositionCreateTime.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        : "—";
    public string PositionStatusText => PositionStatus.HasValue ? ((CryptoPositionStatus)PositionStatus.Value).ToString() : "—";
    public string ProfitText => Profit.HasValue ? Profit.Value.ToString(QuoteDisplayFormat) : "—";
    public string PercentageText => Percentage.HasValue ? Percentage.Value.ToString("N2") : "—";
}


/// <summary>
/// Backs <c>RunSignalsWindow</c>. Lists every signal of one run and LEFT JOINs the position it
/// produced (matched on symbol + the triggering candle's close time = Position.SignalEventTime), so
/// the operator can see the moment a signal was made, whether/when it became a position, and the
/// realised result — to correlate against the chart.
/// </summary>
public partial class RunSignalsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _header = "";

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private ObservableCollection<SignalRow> _signals = [];

    /// <summary>The run these signals belong to — passed to the chart so it shows only this run's data.</summary>
    public int RunId { get; }


    public RunSignalsViewModel(RunRow run)
    {
        RunId = run.Id;
        Header = $"Signals for run #{run.Id}  ({run.Label}, started {run.StartedAt:yyyy-MM-dd HH:mm})";
        Load(run.Id);
    }


    private void Load(int runId)
    {
        try
        {
            using var database = new CryptoDatabase();
            database.Open();

            var rows = database.Connection.Query<SignalRow>(
                "SELECT s.OpenDate as SignalTime, sym.Name as Symbol, i.Name as Interval, " +
                "       s.Side, s.Strategy, s.SignalPrice, s.SlPercentage, s.IsInvalid, s.EventText, " +
                "       p.Id as PositionId, p.CreateTime as PositionCreateTime, p.Status as PositionStatus, " +
                "       p.Profit, p.Percentage " +
                "FROM Signal s " +
                "inner JOIN Symbol sym ON sym.Id = s.SymbolId " +
                "inner JOIN Interval i ON i.Id = s.IntervalId " +
                "left JOIN position p ON p.SignalId = s.SignalId " +
                //"LEFT JOIN Position p ON p.SignalEventTime = s.CloseDate and P.IntervalId=s.IntervalId " +
                //"       AND p.SymbolId = s.SymbolId AND p.EmulatorRunId = s.EmulatorRunId " +
                "WHERE s.EmulatorRunId = @runId " +
                "ORDER BY s.OpenDate",
                new { runId });

            // Attach the quote currency's display format per row so prices/profit show the quote's decimals.
            var exchange = GlobalData.ActiveExchange;
            foreach (var row in rows)
            {
                if (exchange != null && row.Symbol != null
                    && exchange.SymbolListName.TryGetValue(row.Symbol, out var symbol) && symbol.QuoteData != null)
                {
                    row.QuoteDisplayFormat = symbol.QuoteData.DisplayFormat;
                }
                Signals.Add(row);
            }

            int withPosition = 0;
            foreach (var r in Signals)
                if (r.PositionId.HasValue)
                    withPosition++;

            Status = Signals.Count == 0
                ? "This run produced no signals."
                : $"{Signals.Count} signal(s), {withPosition} became a position.";
        }
        catch (Exception ex)
        {
            Status = $"Failed to load signals: {ex.Message}";
        }
    }
}
