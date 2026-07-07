using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Settings;
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

    // Label is now its own EmulatorRun column (filled from the run config at run start), so the grid
    // binds it directly. Previously it was parsed out of ConfigJson per row, which is what made the
    // Results tab take ~10s to open. Legacy runs created before the column show a blank label.
    public string Label { get; set; } = "";

    public string? Result { get; set; }
    public int SignalCount { get; set; }
    public int PositionCount { get; set; }
    public int PositionsOpen { get; set; }
    public int PositionsWon { get; set; }
    public int PositionsLost { get; set; }
    public int PositionsTimeout { get; set; }
    public decimal Profit { get; set; }

    // Summed invested capital of the run's closed positions. Nullable in the DB for legacy runs
    // written before this column existed; Dapper maps a NULL to 0 for a non-nullable decimal.
    public decimal Invested { get; set; }

    // Position duration stats (in seconds) computed via subquery on the Position table. Nullable
    // because a run with zero closed positions has no durations to aggregate.
    public double? AvgDurationSec { get; set; }
    public double? MinDurationSec { get; set; }
    public double? MaxDurationSec { get; set; }

    /// <summary>
    /// Total return as a percentage of the invested capital (100 * Profit / Invested). Returns 0
    /// when nothing was invested (e.g. a run with no closed positions, or a legacy run without the
    /// Invested column). This is the number the grid's "Profit %" column binds to.
    /// </summary>
    public decimal ProfitPercentage => Invested > 0 ? 100m * Profit / Invested : 0m;

    /// <summary>
    /// Win rate over the closed positions: 100 * Won / (Won + Lost). Derived from the already-stored
    /// PositionsWon / PositionsLost counters (no separate DB column needed). Positions whose entry
    /// order never filled (PositionsTimeout) are excluded from both, so they don't drag the win rate
    /// down. Returns 0 when the run has no closed (won/lost) positions. This is the number the grid's
    /// "Win %" column binds to.
    /// </summary>
    public decimal WinPercentage => (PositionsWon + PositionsLost) > 0
        ? 100m * PositionsWon / (PositionsWon + PositionsLost)
        : 0m;

    // Pre-formatted string projections for the numeric grid columns. The grid binds these plain
    // strings instead of a StringFormat on the column binding: a DataGridTextColumn.Binding with a
    // StringFormat over a decimal source sets up a converting binding that tries to parse the
    // formatted text back to decimal on every cell realization, throwing a caught FormatException /
    // TypeConverter ArgumentException per cell — which floods the debugger and stutters scrolling.
    // A string→string binding has no conversion at all. Same approach already used for StartedLocal,
    // Duration, Period and for the columns in PositionRow.
    public string ProfitText => Profit.ToString("N2");
    public string ProfitPercentageText => ProfitPercentage.ToString("N2") + "%";
    public string WinPercentageText => WinPercentage.ToString("N2") + "%";
    public string InvestedText => Invested.ToString("N2");

    public string AvgDurationText => FormatDuration(AvgDurationSec);
    public string MinDurationText => FormatDuration(MinDurationSec);
    public string MaxDurationText => FormatDuration(MaxDurationSec);

    private static string FormatDuration(double? seconds)
    {
        if (seconds == null)
            return "—";
        var span = TimeSpan.FromSeconds(seconds.Value);
        return span.Days > 0
            ? span.ToString(@"d\.hh\:mm\:ss")
            : span.ToString(@"hh\:mm\:ss");
    }

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

    // Ticks while the Results tab is alive; each tick refreshes only the active run's row (no-op when no
    // run is running). 15s is plenty — the user wants "a bit more than the progress bar", not a live feed.
    private readonly DispatcherTimer _liveTimer;

    // Guards against a slow tick overlapping the next one (the DB work runs off the UI thread).
    private bool _liveBusy;

    // The run id refreshed on the previous tick. When it was set and the active run is now gone, the run
    // just finished — so we refresh that row one last time to flip it to its finished state.
    private int? _lastActiveRunId;


    public RunResultsViewModel()
    {
        Refresh();

        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _liveTimer.Tick += (_, _) => RefreshActiveRun();
        _liveTimer.Start();
    }


    /// <summary>
    /// Recomputes the currently-running emulator run's aggregates from its (still growing) signals and
    /// positions and updates only that one row in the grid, so the Results tab shows live numbers during
    /// a run instead of only the progress bar — without rebuilding the whole grid (other rows keep their
    /// selection/scroll). No-op when no run is active. The DB work runs off the UI thread; a transient
    /// lock while the engine is writing is ridden out by the 5s busy-timeout, or skipped until next tick.
    /// </summary>
    private async void RefreshActiveRun()
    {
        if (_liveBusy)
            return;

        int? activeId = GlobalData.CurrentEmulatorRunId;
        // Refresh the active run, or — the tick right after it finished — that same run one final time so
        // the row flips to its finished state (FinishedAt / Result / final stats).
        int? targetId = activeId ?? _lastActiveRunId;
        _lastActiveRunId = activeId;
        if (targetId == null)
            return;

        _liveBusy = true;
        try
        {
            int id = targetId.Value;
            RunRow? fresh = await Task.Run(() =>
            {
                EmulatorDb.RecalculateRuns([id]); // persist live aggregates from current positions
                return LoadRun(id);
            });
            if (fresh == null)
                return;

            // Back on the UI thread (await resumed on the dispatcher context).
            RunRow? existing = Runs.FirstOrDefault(r => r.Id == fresh.Id);
            if (existing != null)
                Runs[Runs.IndexOf(existing)] = fresh;  // in-place: only this row re-renders
            else
                Runs.Insert(0, fresh);                 // run started after the grid loaded (newest first)
        }
        catch
        {
            // Transient DB lock / read error while the engine writes — skip, try again next tick.
        }
        finally
        {
            _liveBusy = false;
        }
    }


    /// <summary>Loads a single run row (same projection as <see cref="Refresh"/>); null when not found.</summary>
    private static RunRow? LoadRun(int runId)
    {
        using var database = new CryptoDatabase();
        database.Open();
        return database.Connection.QueryFirstOrDefault<RunRow>(
            "SELECT r.Id, r.StartedAt, r.FinishedAt, r.Label, r.FromDate, r.ToDate, r.Result, " +
            "       r.SignalCount, r.PositionCount, r.PositionsOpen, r.PositionsWon, r.PositionsLost, r.PositionsTimeout, r.Profit, r.Invested, " +
            "       d.AvgDurationSec, d.MinDurationSec, d.MaxDurationSec " +
            "FROM EmulatorRun r " +
            "LEFT JOIN (SELECT EmulatorRunId, " +
            "           AVG((julianday(CloseTime) - julianday(CreateTime)) * 86400) as AvgDurationSec, " +
            "           MIN((julianday(CloseTime) - julianday(CreateTime)) * 86400) as MinDurationSec, " +
            "           MAX((julianday(CloseTime) - julianday(CreateTime)) * 86400) as MaxDurationSec " +
            "           FROM Position WHERE CloseTime IS NOT NULL GROUP BY EmulatorRunId) d ON d.EmulatorRunId = r.Id " +
            "WHERE r.Id = @runId",
            new { runId });
    }


    [RelayCommand]
    public void Refresh()
    {
        Runs.Clear();

        try
        {
            // TEMP diagnostic: isolate the actual data-load cost of the Results tab from any UI-thread
            // blocking. If this logs a few ms while the view's "render settled" shows seconds, the delay
            // is NOT this grid — it is the UI thread being busy with something else (e.g. the chart).
            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var database = new CryptoDatabase();
            database.Open();

            var rows = database.Connection.Query<RunRow>(
                "SELECT r.Id, r.StartedAt, r.FinishedAt, r.Label, r.FromDate, r.ToDate, r.Result, " +
                "       r.SignalCount, r.PositionCount, r.PositionsOpen, r.PositionsWon, r.PositionsLost, r.PositionsTimeout, r.Profit, r.Invested, " +
                "       d.AvgDurationSec, d.MinDurationSec, d.MaxDurationSec " +
                "FROM EmulatorRun r " +
                "LEFT JOIN (SELECT EmulatorRunId, " +
                "           AVG((julianday(CloseTime) - julianday(CreateTime)) * 86400) as AvgDurationSec, " +
                "           MIN((julianday(CloseTime) - julianday(CreateTime)) * 86400) as MinDurationSec, " +
                "           MAX((julianday(CloseTime) - julianday(CreateTime)) * 86400) as MaxDurationSec " +
                "           FROM Position WHERE CloseTime IS NOT NULL GROUP BY EmulatorRunId) d ON d.EmulatorRunId = r.Id " +
                "ORDER BY r.StartedAt DESC");

            var sortedRows = ApplyConfigSort(rows);
            foreach (var row in sortedRows)
            {
                Runs.Add(row);
            }

            sw.Stop();
            GlobalData.AddTextToLogTab($"RunResults.Refresh: loaded {Runs.Count} run(s) in {sw.Elapsed.TotalMilliseconds:N0} ms");

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
    /// Deletes EVERY emulator run and everything tagged with them (signals, positions, parts/steps,
    /// zones) so the runs grid ends up empty — a full reset back to a clean slate. The caller (the
    /// view's button handler) is responsible for the confirmation prompt.
    /// </summary>
    public void DeleteAllRuns()
    {
        int count = Runs.Count;
        if (count == 0)
            return;

        try
        {
            EmulatorDb.DeleteAllRuns();
            Refresh();
            Status = $"All {count} run(s) deleted.";
        }
        catch (Exception ex)
        {
            Refresh();
            Status = $"Failed to delete all runs: {ex.Message}";
        }
    }


    /// <summary>
    /// Stores a new free-text label (remark) for one run, then reloads the grid so the change shows.
    /// The caller (the view) collects the text via an input dialog; an empty string clears the label.
    /// </summary>
    public void UpdateLabel(int runId, string label)
    {
        try
        {
            EmulatorDb.UpdateLabel(runId, label);
            Refresh();
            Status = $"Run #{runId} label updated.";
        }
        catch (Exception ex)
        {
            Refresh();
            Status = $"Failed to update label: {ex.Message}";
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
    /// Sorts the loaded rows according to the SortColumn / SortDescending saved in the emulator
    /// config file. Returns the rows in default order (StartedAt DESC) when no preference is stored.
    /// </summary>
    private static IEnumerable<RunRow> ApplyConfigSort(IEnumerable<RunRow> rows)
    {
        EmulatorRunConfig config;
        try { config = RunConfigFile.Load(); } catch { return rows; }

        if (string.IsNullOrEmpty(config.SortColumn))
            return rows;

        return SortRows(rows, config.SortColumn, config.SortDescending);
    }


    /// <summary>
    /// Sorts <paramref name="rows"/> by the property that matches the given SortMemberPath
    /// (<paramref name="sortMemberPath"/>). The paths match the DataGrid column definitions.
    /// </summary>
    public static IOrderedEnumerable<RunRow> SortRows(IEnumerable<RunRow> rows, string sortMemberPath, bool descending)
    {
        Func<RunRow, object?> key = sortMemberPath switch
        {
            "Id"                => r => r.Id,
            "Label"             => r => r.Label,
            "Period"            => r => r.FromDate,
            "StartedLocal"      => r => r.StartedAt,
            "FinishedLocal"     => r => r.FinishedAt,
            "Duration"          => r => (r.FinishedAt ?? DateTime.UtcNow) - r.StartedAt,
            "Result"            => r => r.Result,
            "SignalCount"       => r => r.SignalCount,
            "PositionCount"     => r => r.PositionCount,
            "PositionsOpen"     => r => r.PositionsOpen,
            "PositionsWon"      => r => r.PositionsWon,
            "PositionsLost"     => r => r.PositionsLost,
            "PositionsTimeout"  => r => r.PositionsTimeout,
            "WinPercentage"     => r => r.WinPercentage,
            "Profit"            => r => r.Profit,
            "ProfitPercentage"  => r => r.ProfitPercentage,
            "Invested"          => r => r.Invested,
            "AvgDurationText"   => r => r.AvgDurationSec,
            "MinDurationText"   => r => r.MinDurationSec,
            "MaxDurationText"   => r => r.MaxDurationSec,
            _                   => r => r.StartedAt,
        };

        return descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
    }


    /// <summary>
    /// Deserializes the scanner-settings snapshot stored with the run into a <see cref="SettingsBasic"/>
    /// (the same type as <c>GlobalData.Settings</c>), so the caller can show it in the Configure UI.
    /// Returns null when the run has no stored snapshot or it cannot be parsed.
    /// </summary>
    public static SettingsBasic? GetRunSettings(int runId)
    {
        string? json = EmulatorDb.GetSettingsJson(runId);
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<SettingsBasic>(json, JsonTools.DeSerializerOptions);
        }
        catch
        {
            return null;
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
