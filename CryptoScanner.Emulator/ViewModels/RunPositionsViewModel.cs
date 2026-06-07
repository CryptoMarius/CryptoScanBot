using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;

using Dapper;

using System.Collections.ObjectModel;

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
    public int Strategy { get; set; }
    public int Status { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }
    public string? EventText { get; set; }

    public string Duration => CloseTime.HasValue
        ? (CloseTime.Value - CreateTime).ToString(@"hh\:mm\:ss")
        : "—";

    public string SideText => Side switch { 1 => "Long", 2 => "Short", _ => Side.ToString() };
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


    public RunPositionsViewModel(RunRow run)
    {
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
                "       p.Side, p.Strategy, p.Status, p.Profit, p.Percentage, p.EventText " +
                "FROM Position p " +
                "LEFT JOIN Symbol s ON s.Id = p.SymbolId " +
                "LEFT JOIN Interval i ON i.Id = p.IntervalId " +
                "WHERE p.EmulatorRunId = @runId " +
                "ORDER BY p.CreateTime",
                new { runId });

            foreach (var row in rows)
                Positions.Add(row);

            Status = Positions.Count == 0
                ? "This run produced no positions."
                : $"{Positions.Count} position(s).";
        }
        catch (Exception ex)
        {
            Status = $"Failed to load positions: {ex.Message}";
        }
    }
}
