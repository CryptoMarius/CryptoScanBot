using Avalonia.Threading;

using Avalonia.Collections;

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

    // The write time of the emulator binary that made the run (database version 99). Two runs with
    // different stamps ran different code, which is what open point 52 asked to see in the report:
    // the same configuration gave +432,07 and +734,17 on 29-08-2026 with a code change in between.
    public DateTime? BuildStamp { get; set; }
    public string BuildStampText => BuildStamp.HasValue
        ? DateTime.SpecifyKind(BuildStamp.Value, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        : "";
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

    // Position duration stats (in seconds), now read straight from the run row. They used to be
    // computed with a subquery that aggregated the WHOLE Position table on every refresh; the run
    // stores them at run end instead, which is both cheaper and still right once a run's positions
    // have been archived away. Nullable: a run with zero closed positions has nothing to aggregate.
    public double? AvgDurationSec { get; set; }
    public double? MinDurationSec { get; set; }
    public double? MaxDurationSec { get; set; }

    // Run summary, also stored on the run row so it outlives the positions it was derived from.
    // PeakInvested is the money that answers "could an account have run this" - unlike Invested,
    // which is the same stake going round and says nothing about the capital needed.
    public decimal PeakInvested { get; set; }
    public int PeakPositions { get; set; }
    // Deepest fall of the capital from an earlier high, in percent (open point 47).
    public decimal MaxDrawdownPercentage { get; set; }

    // The same fall in USDT, open positions valued at the price of the day (database version 103).
    // Null for older runs, whose daily values are gone.
    public decimal? OpenDrawdown { get; set; }

    // The closed profit without the ten best trades (database version 104, RunRiskMetrics): how many
    // trades the result rests on. Null when the run had no closed trades or no position digest.
    public decimal? ProfitWithoutBestTen { get; set; }

    // How much of the account was at work in trades (database version 110, RunCapitalUse). Null for a
    // run without an equity curve or a position digest. CapitalUseEstimated marks the digest estimate
    // of an older run, which runs high because it counts every DCA from the entry on.
    public decimal? AvgInvested { get; set; }
    public decimal? ExposurePercentage { get; set; }
    public bool? CapitalUseEstimated { get; set; }

    public int PositionsLong { get; set; }
    public int PositionsShort { get; set; }
    public decimal ProfitLong { get; set; }
    public decimal ProfitShort { get; set; }
    // The still-open positions per side at the price of the last day (EmulatorDb.OpenPositionValueSql).
    // Null for runs written before the columns existed.
    public decimal? UnrealizedPnlLong { get; set; }
    public decimal? UnrealizedPnlShort { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }

    // Two fields of the run configuration, pulled out of ConfigJson by the query (json_extract) so
    // the grid does not parse the blob per row. Null for runs written before the fields existed.
    public decimal? StartCapital { get; set; }
    public bool? UseAssetManagement { get; set; }

    /// <summary>
    /// How many quote coins the run's symbols trade in (USDT and USDC = 2), counted by the query from
    /// the symbol list in ConfigJson. Zero when none of those names is found in the Symbol table.
    /// </summary>
    public int QuoteCoinCount { get; set; }

    /// <summary>
    /// The money the run really started with. <see cref="StartCapital"/> is handed out PER quote coin
    /// (PaperAssets.SeedStartBalances), so a run over USDT and USDC symbols had twice that amount -
    /// and dividing the profit by one of them doubled the return. Falls back to the plain start
    /// capital when the quote coins cannot be counted, which is how it was read before.
    /// <para>
    /// A run from 26-10-2026 on has no start capital in its configuration: it started with the
    /// Default column of Paper assets, which may differ per coin. Its start is the first point of its
    /// capital line, which is that money counted together.
    /// </para>
    /// </summary>
    public decimal? TotalStartCapital => StartCapital.HasValue ? StartCapital * Math.Max(1, QuoteCoinCount) : EquityStart;

    /// <summary>
    /// Whether the return on the start capital means anything for this run. It only does when the
    /// paper balances constrained the run: with asset management off nothing is ever refused for lack
    /// of money, so the start capital never bounded what was traded and a percentage of it says
    /// nothing. Legacy runs without the two fields fall out here as well.
    /// </summary>
    public bool HasCapitalReturn => UseAssetManagement == true && TotalStartCapital > 0;

    /// <summary>
    /// The first and last value of the run's equity curve (EmulatorDb.EquityCurveJson): cash plus the
    /// open positions at the price of that day. Read by the query with json_extract; null for runs
    /// without a curve.
    /// </summary>
    public decimal? EquityStart { get; set; }
    public decimal? EquityEnd { get; set; }

    /// <summary>
    /// What the positions still open at the end are worth against the price of the last day: the
    /// change of the equity curve minus the realised <see cref="Profit"/>. Altrady calls it the
    /// unrealized PnL. Without a stop loss this is where the losses hide - every closed position can
    /// have won while the run as a whole is under water.
    /// </summary>
    public decimal? UnrealizedPnl => EquityStart.HasValue && EquityEnd.HasValue ? EquityEnd - EquityStart - Profit : null;

    /// <summary>Realised plus unrealised: what the run is really worth at the end (Altrady: total PnL).</summary>
    public decimal? TotalPnl => EquityStart.HasValue && EquityEnd.HasValue ? EquityEnd - EquityStart : null;

    /// <summary>
    /// Total PnL per side: the realised profit of the closed positions of that side plus what its
    /// still-open positions are worth at the price of the last day. The run's <see cref="TotalPnl"/>
    /// mixes both sides, which hides a side that is deep under water behind one that wins.
    /// </summary>
    public decimal? TotalPnlLong => UnrealizedPnlLong.HasValue ? ProfitLong + UnrealizedPnlLong : null;
    public decimal? TotalPnlShort => UnrealizedPnlShort.HasValue ? ProfitShort + UnrealizedPnlShort : null;

    /// <summary>What the run ends on: the start capital plus the realised profit.</summary>
    public decimal EndCapital => (TotalStartCapital ?? 0m) + Profit;

    /// <summary>
    /// The profit against the money the run started with: 100 * Profit / StartCapital, or as a
    /// difference between end and start capital. Not divided by the number of days - the runs that
    /// are compared cover the same period, so the plain figure is what is wanted. Zero when
    /// <see cref="HasCapitalReturn"/> is false.
    /// </summary>
    public decimal CapitalReturnPercentage => HasCapitalReturn ? 100m * Profit / TotalStartCapital!.Value : 0m;

    /// <summary>Closed trades a run needs before its recovery factor counts (MIN_RANKED_TRADES in emulator_report.py).</summary>
    public const int MinRankedTrades = 30;

    /// <summary>
    /// The one figure the runs are ranked on (decided 03-10-2026, emulator_report.py rank_run and
    /// beste-runs.sql): the result including the open positions (<see cref="TotalPnl"/>) divided by the
    /// deepest fall including the open positions (<see cref="OpenDrawdown"/>) - how many times the run
    /// earned back its worst dip. Both are amounts in USDT, so unlike Return % and Drawdown % it does
    /// not move with the start capital. Null when no fall in USDT is known (runs from before database
    /// version 103, or a run that never fell): ranking those on profit instead is what it replaced.
    /// Like the report, a run without an equity curve falls back on its closed profit as the result.
    /// </summary>
    public decimal? RecoveryFactor => OpenDrawdown > 0 ? (TotalPnl ?? Profit) / OpenDrawdown.Value : null;

    /// <summary>
    /// Whether the recovery factor counts in the ranking. Two thresholds keep chance out: at least
    /// <see cref="MinRankedTrades"/> closed trades, and still a profit without the ten best trades.
    /// A run that does not count stays in the grid; its recovery factor is shown greyed out.
    /// </summary>
    public bool RecoveryFactorCounts => RecoveryFactor.HasValue
        && PositionsWon + PositionsLost >= MinRankedTrades
        && ProfitWithoutBestTen > 0;

    /// <summary>Why the recovery factor does not count, for the hint on its cell; null when it counts.</summary>
    public string? RecoveryFactorReason
    {
        get
        {
            List<string> reasons = [];
            if (PositionsWon + PositionsLost < MinRankedTrades)
                reasons.Add($"fewer than {MinRankedTrades} closed trades");
            if (ProfitWithoutBestTen == null)
                reasons.Add("the positions are no longer known");
            else if (ProfitWithoutBestTen <= 0)
                reasons.Add("the result rests on the best 10 trades");
            if (RecoveryFactor == null)
                reasons.Add("no fall in USDT known");
            return reasons.Count == 0 ? null : "Does not count: " + string.Join(", ", reasons) + ".";
        }
    }

    /// <summary>Greys out the recovery factor of a run that does not count.</summary>
    public double RecoveryFactorOpacity => RecoveryFactorCounts ? 1.0 : 0.4;

    /// <summary>
    /// Return over the capital that was actually tied up: 100 * Profit / PeakInvested. This is the
    /// percentage that means something — <see cref="ProfitPercentage"/> divides by the summed stake,
    /// which grows with the number of trades and therefore flatters a strategy that trades little.
    /// Zero for legacy runs written before the column existed.
    /// </summary>
    public decimal PeakProfitPercentage => PeakInvested > 0 ? 100m * Profit / PeakInvested : 0m;

    /// <summary>
    /// Risk Adjusted Return (AmiBroker): the result against the capital that was on average in
    /// trades, 100 * Total PnL / <see cref="AvgInvested"/> - what the run would have made had the money
    /// been at work all the time. It inflates for a run that is rarely in the market, so it is read
    /// next to Peak cap.: scaling up asks that much more at the busiest moment. Like the recovery
    /// factor, a run without an equity curve falls back on its closed profit. Null without a capital use.
    /// </summary>
    public decimal? RiskAdjustedReturn => AvgInvested > 0 ? 100m * (TotalPnl ?? Profit) / AvgInvested.Value : null;

    /// <summary>Greys out Exposure % and RAR % when they are the digest estimate of an older run.</summary>
    public double CapitalUseOpacity => CapitalUseEstimated == true ? 0.4 : 1.0;

    /// <summary>The hint on an estimated Exposure % or RAR % cell; null for exact figures.</summary>
    public string? CapitalUseReason => CapitalUseEstimated == true
        ? "Estimate from the position digest (run from before database version 110): every DCA counts from the entry on, so the capital runs high - on storsi about 27% - and RAR % low."
        : null;

    /// <summary>
    /// What the run ends on if every still-open position closes as the average winner. The open
    /// positions are not a random sample — the winners already hit take profit and closed — so the
    /// pair with <see cref="WorstCase"/> brackets what the run is really worth.
    /// </summary>
    public decimal BestCase => Profit + PositionsOpen * AverageWin;

    /// <summary>What the run ends on if every still-open position closes as the average loser.</summary>
    public decimal WorstCase => Profit + PositionsOpen * AverageLoss;

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
    public string PeakInvestedText => PeakInvested.ToString("N2");
    public string PeakProfitPercentageText => PeakProfitPercentage.ToString("N2") + "%";
    public string MaxDrawdownText => MaxDrawdownPercentage.ToString("N2") + "%";
    public string UnrealizedPnlText => UnrealizedPnl.HasValue ? UnrealizedPnl.Value.ToString("N2") : "—";
    public string TotalPnlText => TotalPnl.HasValue ? TotalPnl.Value.ToString("N2") : "—";
    public string TotalPnlLongText => TotalPnlLong.HasValue ? TotalPnlLong.Value.ToString("N2") : "—";
    public string TotalPnlShortText => TotalPnlShort.HasValue ? TotalPnlShort.Value.ToString("N2") : "—";
    public string BestCaseText => BestCase.ToString("N2");
    public string WorstCaseText => WorstCase.ToString("N2");
    public string ProfitLongText => ProfitLong.ToString("N2");
    public string ProfitShortText => ProfitShort.ToString("N2");
    public string StartCapitalText => TotalStartCapital.HasValue ? TotalStartCapital.Value.ToString("N2") : "—";
    public string EndCapitalText => HasCapitalReturn ? EndCapital.ToString("N2") : "—";
    public string CapitalReturnPercentageText => HasCapitalReturn ? CapitalReturnPercentage.ToString("N2") + "%" : "—";
    public string RecoveryFactorText => RecoveryFactor.HasValue ? RecoveryFactor.Value.ToString("N2") : "—";
    public string ProfitWithoutBestTenText => ProfitWithoutBestTen.HasValue ? ProfitWithoutBestTen.Value.ToString("N2") : "—";
    public string ExposurePercentageText => CapitalUseText(ExposurePercentage);
    public string RiskAdjustedReturnText => CapitalUseText(RiskAdjustedReturn);

    // An estimate carries "ca." so it still reads as one once it is pasted elsewhere.
    private string CapitalUseText(decimal? value)
    {
        if (!value.HasValue)
            return "—";
        return (CapitalUseEstimated == true ? "ca. " : "") + value.Value.ToString("N2") + "%";
    }

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
/// The runs behind the grid, with one extra: a swap that raises no change event, for a row the
/// filtered view does not show (see <see cref="RunResultsViewModel"/>, the live refresh).
/// </summary>
public class RunRowCollection : ObservableCollection<RunRow>
{
    public void ReplaceWithoutNotify(int index, RunRow row) => Items[index] = row;
}


/// <summary>
/// Backs the Results tab (<c>RunResultsView</c>). Pulls EmulatorRun rows from the emulator's CryptoScanBot.db
/// directly with Dapper — no in-memory cache, no MVVM messages, no live-scanner machinery.
/// The user double-clicks a row to drill into its positions (handled in code-behind).
/// </summary>
public partial class RunResultsViewModel : ObservableObject
{
    [ObservableProperty]
    private RunRowCollection _runs = new();

    [ObservableProperty]
    private string _status = "";

    /// <summary>
    /// Text to filter the grid on: a run stays visible when its label contains every word of it,
    /// in any order and case. "mac open 1d" keeps the Open-marker runs of mac on 1d. Empty shows all.
    /// A word with an operator is a condition on a number column instead: "bbm return%>5 drawdown%<20" keeps
    /// the bbm runs with more than 5% return and less than 20% drawdown (see <see cref="FilterFields"/>).
    /// </summary>
    [ObservableProperty]
    private string _labelFilter = "";

    /// <summary>
    /// What the grid shows: <see cref="Runs"/> through the label filter. The grid sorts this view;
    /// Runs itself keeps every row, so the live refresh and the delete actions work on all of them.
    /// </summary>
    public DataGridCollectionView RunsView { get; }

    /// <summary>Raised when the user selects a run in the grid; the Capital tab follows it.</summary>
    public event Action<int>? RunSelected;

    /// <summary>Called by the grid when its selected row changes.</summary>
    public void OnRunSelected(int runId) => RunSelected?.Invoke(runId);

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
        RunsView = new DataGridCollectionView(Runs) { Filter = MatchesFilter };
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
    /// <para>
    /// Since 26-10-2026 it no longer recomputes anything: it reloads the row the replay loop keeps up
    /// to date (see the comment in the body).
    /// </para>
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
                // Read only since 26-10-2026. The replay loop writes the interim numbers itself
                // (EmulatorDb.SaveRunningStats) and FinishRun the final ones; this timer writing the
                // row as well, from another thread, cleared the FinishedAt and Result of run 726.
                //EmulatorDb.RecalculateRuns([id]); // persist live aggregates from current positions
                return LoadRun(id);
            });
            if (fresh == null)
                return;

            // Back on the UI thread (await resumed on the dispatcher context).
            RunRow? existing = Runs.FirstOrDefault(r => r.Id == fresh.Id);
            if (existing != null && MatchesFilter(existing))
                Runs[Runs.IndexOf(existing)] = fresh;  // in-place: only this row re-renders
            else if (existing != null)
            {
                // The row is filtered out of the view. A Replace (or Remove) of an item the view does
                // not hold makes Avalonia's DataGridCollectionView (11.3) still send a Remove at index
                // 0 to the grid: ProcessRemoveEvent tests "index < (PageIndex + 1) * PageSize", which
                // is -1 < 0 without paging. Every live tick then took the top visible row away, until
                // the filtered grid was empty. The view never sees this swap; it only needs to know
                // when the fresh row would now pass the filter.
                Runs.ReplaceWithoutNotify(Runs.IndexOf(existing), fresh);
                if (MatchesFilter(fresh))
                    RunsView.Refresh();
            }
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


    /// <summary>
    /// The grid's projection of a run. Everything comes from the EmulatorRun row itself — the
    /// duration columns used to be a LEFT JOIN that aggregated the whole Position table on every
    /// refresh, and EmulatorDb now stores them (with the rest of the run summary) at run end. That
    /// keeps the grid correct for a database whose positions have been archived away.
    /// </summary>
    private const string RunSelect =
        "SELECT r.Id, r.StartedAt, r.FinishedAt, r.BuildStamp, r.Label, r.FromDate, r.ToDate, r.Result, " +
        "       r.SignalCount, r.PositionCount, r.PositionsOpen, r.PositionsWon, r.PositionsLost, r.PositionsTimeout, r.Profit, r.Invested, " +
        "       r.PeakInvested, r.PeakPositions, r.MaxDrawdownPercentage, r.PositionsLong, r.PositionsShort, r.ProfitLong, r.ProfitShort, " +
        "       r.UnrealizedPnlLong, r.UnrealizedPnlShort, r.OpenDrawdown, r.ProfitWithoutBestTen, " +
        "       r.AvgInvested, r.ExposurePercentage, r.CapitalUseEstimated, " +
        "       r.AverageWin, r.AverageLoss, r.AvgDurationSec, r.MinDurationSec, r.MaxDurationSec, " +
        // First and last value of the equity curve, for the unrealised and total PnL columns. CAST
        // because json_extract returns an integer for a round value (100000) and a real otherwise,
        // and Dapper stops at the first row whose type differs from the one it started with.
        "       CAST(json_extract(r.EquityCurveJson, '$.rows[0][1]') AS REAL) AS EquityStart, " +
        "       CAST(json_extract(r.EquityCurveJson, '$.rows[#-1][1]') AS REAL) AS EquityEnd, " +
        // Start capital and the asset-management switch live in the run configuration blob. SQLite's
        // json_extract reads them in the query, which keeps the grid fast (parsing ConfigJson per row
        // in C# is what once made this tab take ten seconds to open) and needs no new column.
        "       json_extract(r.ConfigJson, '$.StartCapital') AS StartCapital, " +
        // The switch moved to the settings on 26-10-2026; it was always in the settings snapshot too.
        "       COALESCE(json_extract(r.ConfigJson, '$.UseAssetManagement'), " +
        "                json_extract(r.SettingsJson, '$.Trading.UseAssetManagement')) AS UseAssetManagement, " +
        // The start capital is handed out per quote coin, so the return needs to know how many the
        // run traded in. Through the Symbol.Name index this costs about 50 ms over 1.300 runs.
        "       (SELECT COUNT(DISTINCT s.Quote) FROM json_each(r.ConfigJson, '$.Symbols') j " +
        "        JOIN Symbol s ON s.Name = j.value) AS QuoteCoinCount " +
        "FROM EmulatorRun r ";


    /// <summary>Loads a single run row (same projection as <see cref="Refresh"/>); null when not found.</summary>
    private static RunRow? LoadRun(int runId)
    {
        using var database = new CryptoDatabase();
        database.Open();
        return database.Connection.QueryFirstOrDefault<RunRow>(
            RunSelect + "WHERE r.Id = @runId", new { runId });
    }


    /// <summary>True when the run's label contains every word of <see cref="LabelFilter"/>.</summary>
    internal static bool LabelMatches(string? label, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;
        string text = label ?? "";
        return filter.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }


    /// <summary>
    /// The filter name of a grid column: its header without spaces and dots, in any case - "Recovery
    /// factor" is recoveryfactor, "Peak cap." is peakcap, "Win %" is win%. One rule and no invented
    /// short names (25-10-2026: rf, dd, w10 and the like could not be guessed from the grid).
    /// </summary>
    internal static string FilterName(string header) => header.Replace(" ", "").Replace(".", "").ToLowerInvariant();

    /// <summary>
    /// The numeric columns a filter word can test, each by <see cref="FilterName"/> of its header.
    /// The one name without a column is counts (1 = the recovery factor counts in the ranking), which
    /// the hint of the Recovery factor column mentions. The durations are tested in days.
    /// A null value (a run without a capital return, for instance) never passes a condition.
    /// </summary>
    internal static readonly Dictionary<string, Func<RunRow, decimal?>> FilterFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = r => r.Id,
            ["signals"] = r => r.SignalCount,
            ["pos"] = r => r.PositionCount,
            ["open"] = r => r.PositionsOpen,
            ["won"] = r => r.PositionsWon,
            ["lost"] = r => r.PositionsLost,
            ["timeout"] = r => r.PositionsTimeout,
            ["win%"] = r => r.WinPercentage,
            ["profit"] = r => r.Profit,
            ["unrealizedpnl"] = r => r.UnrealizedPnl,
            ["totalpnl"] = r => r.TotalPnl,
            ["totalpnllong"] = r => r.TotalPnlLong,
            ["totalpnlshort"] = r => r.TotalPnlShort,
            ["profit%"] = r => r.ProfitPercentage,
            ["invested"] = r => r.Invested,
            ["peakcap"] = r => r.PeakInvested,
            ["peakpos"] = r => r.PeakPositions,
            ["peak%"] = r => r.PeakProfitPercentage,
            ["drawdown%"] = r => r.MaxDrawdownPercentage,
            ["startcap"] = r => r.TotalStartCapital,
            ["endcap"] = r => r.HasCapitalReturn ? r.EndCapital : null,
            ["return%"] = r => r.HasCapitalReturn ? r.CapitalReturnPercentage : null,
            ["recoveryfactor"] = r => r.RecoveryFactor,
            // 1 for a run whose recovery factor counts in the ranking, 0 otherwise: "counts=1 recoveryfactor>2".
            ["counts"] = r => r.RecoveryFactorCounts ? 1 : 0,
            ["withoutbest10"] = r => r.ProfitWithoutBestTen,
            ["exposure%"] = r => r.ExposurePercentage,
            ["rar%"] = r => r.RiskAdjustedReturn,
            ["profitlong"] = r => r.ProfitLong,
            ["profitshort"] = r => r.ProfitShort,
            ["bestcase"] = r => r.BestCase,
            ["worstcase"] = r => r.WorstCase,
            ["avgdur"] = r => Days(r.AvgDurationSec),
            ["mindur"] = r => Days(r.MinDurationSec),
            ["maxdur"] = r => Days(r.MaxDurationSec),
        };

    private static decimal? Days(double? seconds) => seconds == null ? null : (decimal)(seconds.Value / 86400.0);


    // Longest first, so ">=" is not read as ">" followed by "=5".
    private static readonly string[] FilterOperators = [">=", "<=", "<>", "!=", ">", "<", "="];


    /// <summary>One "field operator number" word of the filter, e.g. dd&lt;20.</summary>
    internal sealed record FilterCondition(Func<RunRow, decimal?> Field, string Operator, decimal Value)
    {
        public bool Test(RunRow row)
        {
            decimal? actual = Field(row);
            if (actual == null)
                return false;
            return Operator switch
            {
                ">" => actual > Value,
                "<" => actual < Value,
                ">=" => actual >= Value,
                "<=" => actual <= Value,
                "=" => actual == Value,
                _ => actual != Value,
            };
        }
    }


    /// <summary>
    /// Splits the filter text into label words and numeric conditions. A word with an operator in it
    /// is a condition (return&gt;5, dd&lt;20, profit%&gt;=2, win&gt;50, profit&lt;-100); both a comma and
    /// a dot work as decimal separator. A condition on an unknown field lands in
    /// <paramref name="unknown"/>; one whose number is not typed yet ("dd&lt;") is skipped, so the grid
    /// does not empty halfway through typing. Everything else is a word that the label must contain.
    /// </summary>
    internal static (List<string> Words, List<FilterCondition> Conditions) ParseFilter(string? filter, out List<string> unknown)
    {
        var words = new List<string>();
        var conditions = new List<FilterCondition>();
        unknown = [];
        if (string.IsNullOrWhiteSpace(filter))
            return (words, conditions);

        // "return > 1.0" is the same condition as "return>1.0": without this the spaces split it into
        // the label words "return", ">" and "1.0", which no label contains, and the grid went empty.
        // The space after the operator only goes when a number follows, so "dd< foo>3" stays two words.
        filter = System.Text.RegularExpressions.Regex.Replace(filter, @"\s*(>=|<=|<>|!=|>|<|=)(?:\s+(?=[-+]?[0-9.,]))?", "$1");

        foreach (string word in filter.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string? op = null;
            int at = -1;
            foreach (string candidate in FilterOperators)
            {
                at = word.IndexOf(candidate, StringComparison.Ordinal);
                if (at > 0)
                {
                    op = candidate;
                    break;
                }
            }

            if (op == null)
            {
                words.Add(word);
                continue;
            }

            string name = word[..at];
            string number = word[(at + op.Length)..].Replace(',', '.');
            // Through FilterName as well, so a name typed with the dot of its header ("peakcap.") still works.
            if (!FilterFields.TryGetValue(FilterName(name), out Func<RunRow, decimal?>? field))
            {
                unknown.Add(name);
                continue;
            }
            if (decimal.TryParse(number, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal value))
                conditions.Add(new FilterCondition(field, op, value));
        }
        return (words, conditions);
    }


    /// <summary>True when the run passes every word and every numeric condition of <paramref name="filter"/>.</summary>
    internal static bool RowMatches(RunRow row, string? filter)
    {
        var (words, conditions) = ParseFilter(filter, out _);
        return Passes(row, words, conditions);
    }


    private static bool Passes(RunRow row, List<string> words, List<FilterCondition> conditions)
    {
        string label = row.Label ?? "";
        return words.All(word => label.Contains(word, StringComparison.OrdinalIgnoreCase))
            && conditions.All(condition => condition.Test(row));
    }


    // The parsed filter, so the view's per-row test does not split the text for every run.
    private List<string> _filterWords = [];
    private List<FilterCondition> _filterConditions = [];
    private List<string> _filterUnknown = [];


    private bool MatchesFilter(object item) => item is RunRow row && Passes(row, _filterWords, _filterConditions);


    partial void OnLabelFilterChanged(string value)
    {
        (_filterWords, _filterConditions) = ParseFilter(value, out _filterUnknown);
        RunsView.Refresh();
        UpdateStatus();
    }


    private void UpdateStatus()
    {
        if (Runs.Count == 0)
            Status = "No runs yet — start one from the main window.";
        else if (string.IsNullOrWhiteSpace(LabelFilter))
            Status = $"{Runs.Count} run(s).";
        else
            Status = $"{RunsView.Count} of {Runs.Count} run(s) match \"{LabelFilter}\"."
                + (_filterUnknown.Count > 0
                    ? $" Unknown field(s): {string.Join(", ", _filterUnknown)} - known: {string.Join(", ", FilterFields.Keys)}."
                    : "");
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

            var rows = database.Connection.Query<RunRow>(RunSelect + "ORDER BY r.StartedAt DESC");

            var sortedRows = ApplyConfigSort(rows);
            foreach (var row in sortedRows)
            {
                Runs.Add(row);
            }

            sw.Stop();
            GlobalData.AddTextToLogTab($"RunResults.Refresh: loaded {Runs.Count} run(s) in {sw.Elapsed.TotalMilliseconds:N0} ms");

            UpdateStatus();
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
            "Id" => r => r.Id,
            "Label" => r => r.Label,
            "Period" => r => r.FromDate,
            "StartedLocal" => r => r.StartedAt,
            "FinishedLocal" => r => r.FinishedAt,
            "Duration" => r => (r.FinishedAt ?? DateTime.UtcNow) - r.StartedAt,
            "Result" => r => r.Result,
            "BuildStampText" => r => r.BuildStamp,
            "SignalCount" => r => r.SignalCount,
            "PositionCount" => r => r.PositionCount,
            "PositionsOpen" => r => r.PositionsOpen,
            "PositionsWon" => r => r.PositionsWon,
            "PositionsLost" => r => r.PositionsLost,
            "PositionsTimeout" => r => r.PositionsTimeout,
            "WinPercentage" => r => r.WinPercentage,
            "Profit" => r => r.Profit,
            "ProfitPercentage" => r => r.ProfitPercentage,
            "UnrealizedPnl" => r => r.UnrealizedPnl,
            "TotalPnl" => r => r.TotalPnl,
            "TotalPnlLong" => r => r.TotalPnlLong,
            "TotalPnlShort" => r => r.TotalPnlShort,
            "Invested" => r => r.Invested,
            "PeakInvested" => r => r.PeakInvested,
            "PeakPositions" => r => r.PeakPositions,
            "PeakProfitPercentage" => r => r.PeakProfitPercentage,
            "MaxDrawdownPercentage" => r => r.MaxDrawdownPercentage,
            "StartCapital" => r => r.TotalStartCapital,
            "TotalStartCapital" => r => r.TotalStartCapital,
            "EndCapital" => r => r.HasCapitalReturn ? r.EndCapital : null,
            "CapitalReturnPercentage" => r => r.HasCapitalReturn ? r.CapitalReturnPercentage : null,
            "RecoveryFactor" => r => r.RecoveryFactor,
            "ProfitWithoutBestTen" => r => r.ProfitWithoutBestTen,
            "ExposurePercentage" => r => r.ExposurePercentage,
            "RiskAdjustedReturn" => r => r.RiskAdjustedReturn,
            "ProfitLong" => r => r.ProfitLong,
            "ProfitShort" => r => r.ProfitShort,
            "BestCase" => r => r.BestCase,
            "WorstCase" => r => r.WorstCase,
            "AvgDurationText" => r => r.AvgDurationSec,
            "MinDurationText" => r => r.MinDurationSec,
            "MaxDurationText" => r => r.MaxDurationSec,
            _ => r => r.StartedAt,
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


    /// <summary>
    /// Writes the given runs to an Excel sheet in the Excel folder of the data folder and opens it
    /// (Services.ExcelRunsDump). The result goes to Status.
    /// </summary>
    public void ExportToExcel(IReadOnlyList<RunRow> rows)
    {
        if (rows.Count == 0)
        {
            Status = "Nothing selected to export.";
            return;
        }

        try
        {
            string path = new Services.ExcelRunsDump().ExportToExcel(rows);
            Status = $"{rows.Count} run(s) exported to {path}";
        }
        catch (IOException ex)
        {
            // The usual cause: the previous export is still open in Excel, which locks the file
            Status = $"Failed to export to Excel (is the file still open in Excel?): {ex.Message}";
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "");
            Status = $"Failed to export to Excel: {ex.Message}";
        }
    }
}
