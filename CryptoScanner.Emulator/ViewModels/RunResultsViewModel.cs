using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Context;

using Dapper;

using System.Collections.ObjectModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// Lightweight projection of one row in the EmulatorRun table — only the columns the runs
/// grid shows. Avoids pulling all the Symbol/Exchange Computed-properties that the full
/// CryptoEmulatorRun model would not have anyway.
/// </summary>
public class RunRow
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Label { get; set; } = "";
    public string? Result { get; set; }
    public int SignalCount { get; set; }
    public int PositionCount { get; set; }

    public string Duration => FinishedAt.HasValue
        ? (FinishedAt.Value - StartedAt).ToString(@"hh\:mm\:ss")
        : "—";
}


/// <summary>
/// Backs <c>RunResultsWindow</c>. Pulls EmulatorRun rows from the emulator's CryptoScanBot.db
/// directly with Dapper — no in-memory cache, no MVVM messages, no live-scanner machinery.
/// The user double-clicks a row to drill into its positions (handled in code-behind).
/// </summary>
public partial class RunResultsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<RunRow> _runs = [];

    [ObservableProperty]
    private string _status = "";


    public RunResultsViewModel()
    {
        Refresh();
    }


    public void Refresh()
    {
        Runs.Clear();

        try
        {
            using var database = new CryptoDatabase();
            database.Open();

            var rows = database.Connection.Query<RunRow>(
                "SELECT Id, StartedAt, FinishedAt, Label, Result, SignalCount, PositionCount " +
                "FROM EmulatorRun ORDER BY StartedAt DESC");

            foreach (var row in rows)
                Runs.Add(row);

            Status = Runs.Count == 0
                ? "No runs yet — start one from the main window."
                : $"{Runs.Count} run(s).";
        }
        catch (Exception ex)
        {
            Status = $"Failed to load runs: {ex.Message}";
        }
    }
}
