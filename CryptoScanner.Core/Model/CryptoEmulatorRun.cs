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
    public int PositionsCancelled { get; set; }
    public decimal Profit { get; set; }

    // Summed invested amount of the closed positions (same scope as Profit). Lets the Results grid
    // show the total return as a percentage of the invested capital (100 * Profit / Invested).
    public decimal Invested { get; set; }

    // ---------------------------------------------------------------------------------------
    // Run summary: everything below is DERIVED from the run's positions and stored here so it
    // survives them. Session0 (the merged sessions 1..9) kept only the counters above and threw
    // the positions away, which makes peak capital, the DCA split, the long/short split and the
    // average winner/loser unrecoverable for 6.366 runs - the numbers a run is actually judged on.
    // Computing them once at run end costs one pass and keeps an archived database answerable.
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Peak capital: the most money that sat in open positions at the same moment. NOT the summed
    /// <see cref="Invested"/>, which is the same money going round and runs into six figures for a
    /// busy strategy. This is the number that answers "could an account have run this".
    /// </summary>
    public decimal PeakInvested { get; set; }

    /// <summary>Most positions open at the same moment; the practical limit next to the slot count.</summary>
    public int PeakPositions { get; set; }

    // Closed positions and their realised profit per side. A short's stop sits nearer and its
    // target further (both are an arithmetic percentage of the anchor), so the two sides are never
    // compared without that handicap in mind - which needs them counted separately.
    public int PositionsLong { get; set; }
    public int PositionsShort { get; set; }
    public decimal ProfitLong { get; set; }
    public decimal ProfitShort { get; set; }

    // Mean result of a winning and of a losing closed position. Together with PositionsOpen they
    // give the best and worst case for the still-open positions, which is how a run with many of
    // them is read.
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }

    // Position duration over the closed positions, in seconds. Nullable: a run without closed
    // positions has nothing to average. The Results grid used to derive these with a subquery over
    // the whole Position table on every refresh; reading them from the run row is both faster and
    // still correct once the positions are archived away.
    public double? AvgDurationSec { get; set; }
    public double? MinDurationSec { get; set; }
    public double? MaxDurationSec { get; set; }

    /// <summary>
    /// The closed positions grouped by how many DCA parts actually FILLED (Position.PartCount), as
    /// JSON: <c>[{"Parts":0,"Count":423,"Won":423,"Profit":474.61,"Invested":6320.40},...]</c>.
    /// Stored as JSON because the number of rungs varies per run.
    /// <para>
    /// It is here because the run total hides two groups with opposite characters: the entries that
    /// never needed the ladder win nearly always on a small stake, and the group that walks the whole
    /// ladder carries the loss at several times the capital. A run cannot be judged without it.
    /// </para>
    /// </summary>
    public string? DcaBreakdownJson { get; set; }
}


/// <summary>
/// One rung of a run's DCA breakdown: the closed positions that filled exactly <see cref="Parts"/>
/// DCA parts, with what they cost and what they returned. Serialized into
/// <see cref="CryptoEmulatorRun.DcaBreakdownJson"/>.
/// </summary>
public class CryptoDcaBucket
{
    /// <summary>Number of DCA parts that actually filled (Position.PartCount).</summary>
    public int Parts { get; set; }

    /// <summary>Closed positions on this rung.</summary>
    public int Count { get; set; }

    /// <summary>How many of them ended in profit.</summary>
    public int Won { get; set; }

    /// <summary>Their summed realised result.</summary>
    public decimal Profit { get; set; }

    /// <summary>Their summed invested amount, which is what makes the last rung the expensive one.</summary>
    public decimal Invested { get; set; }
}
