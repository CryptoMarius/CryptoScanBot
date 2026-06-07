using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Context;
using CryptoScanner.Emulator.Engine;

using Dapper;

using System.Collections.ObjectModel;
using System.Text.Json;

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

    // Nullable: runs created before the period columns existed have NULL here.
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // The EmulatorRun table has no Label column — the label lives inside ConfigJson (the
    // serialized EmulatorRunConfig). Dapper fills ConfigJson from the query; Refresh parses
    // the human Label out of it afterwards. Selecting a non-existent "Label" column was the
    // bug that made the whole grid come up empty (the query threw "no such column: Label").
    public string? ConfigJson { get; set; }
    public string Label { get; set; } = "";

    public string? Result { get; set; }
    public int SignalCount { get; set; }
    public int PositionCount { get; set; }
    public int PositionsOpen { get; set; }
    public int PositionsWon { get; set; }
    public int PositionsLost { get; set; }
    public decimal Profit { get; set; }

    public string Duration => FinishedAt.HasValue
        ? (FinishedAt.Value - StartedAt).ToString(@"hh\:mm\:ss")
        : "—";

    /// <summary>The replay window as "from → to", plus the length in days — the period length is
    /// what makes two runs comparable. Blank for legacy runs without a stored period.</summary>
    public string Period
    {
        get
        {
            if (FromDate == null || ToDate == null)
            {
                return "—";
            }
            int days = (int)Math.Round((ToDate.Value - FromDate.Value).TotalDays);
            return $"{FromDate.Value:yyyy-MM-dd} → {ToDate.Value:yyyy-MM-dd} ({days}d)";
        }
    }
}


/// <summary>
/// Backs the Results tab (<c>RunResultsView</c>). Pulls EmulatorRun rows from the emulator's CryptoScanBot.db
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


    [RelayCommand]
    public void Refresh()
    {
        Runs.Clear();

        try
        {
            using var database = new CryptoDatabase();
            database.Open();

            var rows = database.Connection.Query<RunRow>(
                "SELECT Id, StartedAt, FinishedAt, FromDate, ToDate, ConfigJson, Result, " +
                "       SignalCount, PositionCount, PositionsOpen, PositionsWon, PositionsLost, Profit " +
                "FROM EmulatorRun ORDER BY StartedAt DESC");

            foreach (var row in rows)
            {
                // Pull the human label out of the stored run config. Best-effort: a malformed or
                // legacy ConfigJson just leaves the Label blank rather than dropping the row.
                if (!string.IsNullOrWhiteSpace(row.ConfigJson))
                {
                    try
                    {
                        var cfg = JsonSerializer.Deserialize<EmulatorRunConfig>(row.ConfigJson);
                        if (cfg != null)
                            row.Label = cfg.Label;
                    }
                    catch
                    {
                        // leave Label empty
                    }
                }
                Runs.Add(row);
            }

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
