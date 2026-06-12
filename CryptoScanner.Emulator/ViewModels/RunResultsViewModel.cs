using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;

using Dapper;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    // Summed invested capital of the run's closed positions. Nullable in the DB for legacy runs
    // written before this column existed; Dapper maps a NULL to 0 for a non-nullable decimal.
    public decimal Invested { get; set; }

    /// <summary>
    /// Total return as a percentage of the invested capital (100 * Profit / Invested). Returns 0
    /// when nothing was invested (e.g. a run with no closed positions, or a legacy run without the
    /// Invested column). This is the number the grid's "Profit %" column binds to.
    /// </summary>
    public decimal ProfitPercentage => Invested > 0 ? 100m * Profit / Invested : 0m;

    /// <summary>
    /// Win rate over the closed positions: 100 * Won / (Won + Lost). Derived from the already-stored
    /// PositionsWon / PositionsLost counters (no separate DB column needed). Returns 0 when the run
    /// has no closed positions. This is the number the grid's "Win %" column binds to.
    /// </summary>
    public decimal WinPercentage => (PositionsWon + PositionsLost) > 0
        ? 100m * PositionsWon / (PositionsWon + PositionsLost)
        : 0m;

    // StartedAt/FinishedAt are stored as UTC (DateTime.UtcNow in EmulatorDb), but SQLite/Dapper
    // hands them back with Kind=Unspecified. SpecifyKind(..., Utc) tags them correctly so
    // ToLocalTime() actually shifts to the machine's timezone instead of treating the value as
    // already-local. These string projections are what the grid binds to.
    public string StartedLocal =>
        DateTime.SpecifyKind(StartedAt, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string FinishedLocal => FinishedAt.HasValue
        ? DateTime.SpecifyKind(FinishedAt.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        : "—";

    /// <summary>
    /// Wall-clock length of the run. For a finished run it is FinishedAt − StartedAt; for a run that
    /// is still going (no FinishedAt) it is "now − StartedAt", i.e. how long it has been running at
    /// the moment the grid was loaded/refreshed. Both timestamps are UTC, and DateTime.UtcNow is too,
    /// so the difference is correct regardless of timezone. Shows the day count once a run passes 24h.
    /// </summary>
    public string Duration
    {
        get
        {
            TimeSpan span = (FinishedAt ?? DateTime.UtcNow) - StartedAt;
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;
            return span.Days > 0
                ? span.ToString(@"d\.hh\:mm\:ss")
                : span.ToString(@"hh\:mm\:ss");
        }
    }

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
                "       SignalCount, PositionCount, PositionsOpen, PositionsWon, PositionsLost, Profit, Invested " +
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


    /// <summary>
    /// Deletes one emulator run and everything tagged with it (signals, positions and their
    /// parts/steps) from the database, then reloads the grid so the row disappears. The caller
    /// (the view's context-menu handler) is responsible for any confirmation prompt — it owns the
    /// window needed to root a dialog, which a ViewModel deliberately does not.
    /// </summary>
    public void DeleteRuns(IReadOnlyList<RunRow> rows)
    {
        if (rows.Count == 0)
            return;

        try
        {
            // One transaction for the whole selection — all-or-nothing.
            EmulatorDb.DeleteRuns(rows.Select(r => r.Id));
            Refresh();
            Status = rows.Count == 1
                ? $"Run #{rows[0].Id} deleted."
                : $"{rows.Count} runs deleted.";
        }
        catch (Exception ex)
        {
            Refresh();
            Status = $"Failed to delete run(s): {ex.Message}";
        }
    }


    /// <summary>
    /// Recomputes and stores each selected run's aggregates (counts, won/lost/open, Profit, Invested)
    /// from its current positions, then reloads the grid. Use to backfill runs that predate a stat
    /// column (e.g. Invested → the Profit % column) or after positions changed. Non-destructive.
    /// </summary>
    public void RecalculateRuns(IReadOnlyList<RunRow> rows)
    {
        if (rows.Count == 0)
            return;

        try
        {
            int updated = EmulatorDb.RecalculateRuns(rows.Select(r => r.Id));
            Refresh();
            Status = $"Recalculated {updated} run(s).";
        }
        catch (Exception ex)
        {
            Refresh();
            Status = $"Failed to recalculate run(s): {ex.Message}";
        }
    }


    /// <summary>
    /// Returns the scanner-settings JSON that was stored with the run, pretty-printed for display in
    /// the JSON viewer. Returns null when the run has no stored snapshot (e.g. a legacy run); the
    /// caller shows a message then. Falls back to the raw text if it cannot be re-parsed/indented.
    /// </summary>
    public string? GetSettingsJsonForDisplay(int runId)
    {
        string? json = EmulatorDb.GetSettingsJson(runId);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // Not valid JSON for some reason — show it as-is rather than nothing.
            return json;
        }
    }


    /// <summary>
    /// Writes each selected run's stored scanner-settings snapshot back out to a JSON file in the data
    /// folder, named like the scanner's own settings file but with the run id appended
    /// (e.g. "CryptoScanBot-settings-#70.json"). Lets a run's exact configuration be inspected or
    /// copied back over the scanner's settings.json to reproduce it. Runs without a stored snapshot
    /// (legacy) are skipped and noted in the log.
    /// </summary>
    public void ExportSettings(IReadOnlyList<RunRow> rows)
    {
        if (rows.Count == 0)
            return;

        try
        {
            int written = 0;
            string? lastPath = null;
            foreach (RunRow row in rows)
            {
                string? settingsJson = EmulatorDb.GetSettingsJson(row.Id);
                if (string.IsNullOrWhiteSpace(settingsJson))
                {
                    GlobalData.AddTextToLogTab($"Run #{row.Id} has no stored settings to export — skipped.");
                    continue;
                }

                string filename = $"{Constants.AppName}-settings-#{row.Id}.json";
                lastPath = Path.Combine(GlobalData.AppDataFolder, filename);
                File.WriteAllText(lastPath, settingsJson);
                written++;
                GlobalData.AddTextToLogTab($"Exported settings of run #{row.Id} to {lastPath}");
            }

            Status = written switch
            {
                0 => "No settings exported (selected run(s) have no stored snapshot).",
                1 => $"Settings written to {lastPath}",
                _ => $"{written} settings files written to {GlobalData.AppDataFolder}",
            };
        }
        catch (Exception ex)
        {
            Status = $"Failed to export settings: {ex.Message}";
        }
    }
}
