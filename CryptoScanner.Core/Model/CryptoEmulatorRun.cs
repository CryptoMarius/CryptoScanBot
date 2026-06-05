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

    // Snapshot of the run configuration (symbols, period, active strategies, settings).
    // Stored as JSON so the schema does not need to evolve every time a knob is added.
    public string ConfigJson { get; set; } = "";

    // Build identification (git short SHA, optional).
    public string? GitSha { get; set; }

    // "completed", "cancelled", "failed: <reason>" — set when the run ends.
    public string? Result { get; set; }

    public int SignalCount { get; set; }
    public int PositionCount { get; set; }
}
