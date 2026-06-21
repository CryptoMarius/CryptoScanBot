using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

/// <summary>
/// Bookkeeping record for one emulator (backtest) run. Persisted in the EmulatorRun table of
/// the emulator's CryptoScanBot.db (the live DB has the same schema but the table stays empty).
/// Signal.EmulatorRunId and Position.EmulatorRunId reference this row so a run's signals and
/// positions can always be traced back to the exact configuration that produced them.
/// </summary>
[Table("EmulatorRun")]
public class CryptoEmulatorRun
{
    [Key]
    public int Id { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    // Free-form label copied from the run config at run start, so the Results grid can show it as a
    // column without deserializing ConfigJson per row (that per-row parse was the grid's slow part).
    public string Label { get; set; } = "";

    // The replay window of the run (copied from the run config). Stored on the run itself so the
    // Results grid can show the period — its LENGTH matters when comparing runs.
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // Snapshot of the run configuration (symbols, period, active strategies, settings).
    // Stored as JSON so the schema does not need to evolve every time a knob is added.
    public string ConfigJson { get; set; } = "";

    // Full snapshot of the scanner's settings.json (GlobalData.Settings) at run start, so the
    // exact configuration that produced a run can be inspected and the "best" one restored later.
    // Nullable for rows written before this column existed.
    public string? SettingsJson { get; set; }

    // Build identification (git short SHA, optional).
    public string? GitSha { get; set; }

    // "completed", "cancelled", "failed: <reason>" — set when the run ends.
    public string? Result { get; set; }

    public int SignalCount { get; set; }
    public int PositionCount { get; set; }

    // Outcome breakdown, computed at run end. Open = still running (no CloseTime); Won/Lost/Timeout
    // partition the closed positions: Timeout = the entry order never filled (status Timeout), so it
    // never became a real trade and is excluded from Won/Lost; the rest split on their realised profit.
    // Profit is the summed realised result of the closed positions — the number the whole backtest is
    // ultimately about.
    public int PositionsOpen { get; set; }
    public int PositionsWon { get; set; }
    public int PositionsLost { get; set; }
    public int PositionsTimeout { get; set; }
    public decimal Profit { get; set; }

    // Summed invested amount of the closed positions (same scope as Profit). Lets the Results grid
    // show the total return as a percentage of the invested capital (100 * Profit / Invested).
    public decimal Invested { get; set; }
}
