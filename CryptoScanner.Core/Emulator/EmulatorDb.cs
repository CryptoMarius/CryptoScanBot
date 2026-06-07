using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Utility entry points for the emulator's own CryptoScanBot.db lifecycle.
///
/// The emulator runs in a separate data folder (set via the --folder command-line flag,
/// resolved by IPlatformService.GetDataDirectory). Database paths, settings files and cached
/// candle files are therefore physically separate from the live scanner — this class only
/// adds the run-bookkeeping on top of that isolation.
/// </summary>
public static class EmulatorDb
{
    /// <summary>
    /// Drops the emulator's main CryptoScanBot.db file and rebuilds it from scratch via the
    /// regular migration flow. Idempotent. Per-exchange candle DBs are intentionally left
    /// alone (they hold the historical candles the run replays).
    /// </summary>
    public static void Reset()
    {
        string dbFile = Path.Combine(GlobalData.AppDataFolder, Constants.AppName + ".db");
        if (File.Exists(dbFile))
            File.Delete(dbFile);

        // Recreate via the standard schema bootstrap.
        CryptoDatabase.SetDatabaseDefaults();
    }

    /// <summary>
    /// Removes all stored zones (DLZ/FVG/SMC) for the given symbols and clears their in-memory
    /// zone state so a run starts from a blank slate.
    ///
    /// Zones have no EmulatorRunId — unlike signals/positions they are NOT separated per run.
    /// <see cref="Zones.ZoneDlz.LoadZonesForSymbol"/> reloads every stored zone from the DB at the
    /// start of each zone calculation, INCLUDING its CloseTime/broken state. So a zone that a
    /// PREVIOUS run closed at time T would be loaded as already-closed at the start of a new run,
    /// even though on the new replay's timeline T hasn't happened yet — look-ahead contamination
    /// that makes runs non-reproducible. Zones are fully rebuilt from the candles as the replay
    /// progresses, so clearing them loses nothing.
    /// </summary>
    public static void ClearZonesForSymbols(Model.CryptoExchange exchange, IEnumerable<string> symbolNames)
    {
        using var database = new CryptoDatabase();
        database.Open();
        using var transaction = database.BeginTransaction();
        try
        {
            foreach (string name in symbolNames)
            {
                if (!exchange.SymbolListName.TryGetValue(name, out CryptoSymbol? symbol))
                    continue;

                database.Connection.Execute("delete from Zone where SymbolId = @id", new { id = symbol.Id }, transaction);

                // Drop the in-memory zone lists + DLZ swing-point admin too, otherwise the first
                // inline FVG/SMC scan (before the first DLZ reload) would still see last run's
                // leftover in-memory zones from the same app session.
                symbol.Data.ResetFvgData();
                symbol.Data.ResetDlzData();
                symbol.Data.ResetSmcData();
                symbol.Data.ResetTrendData();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }


    /// <summary>
    /// Inserts an EmulatorRun row and stores its id in
    /// <see cref="GlobalData.CurrentEmulatorRunId"/> so subsequent signals and positions are
    /// tagged with it. Call once at run start.
    /// </summary>
    public static CryptoEmulatorRun StartRun(string configJson, string? settingsJson = null, string? gitSha = null)
    {
        using var database = new CryptoDatabase();
        database.Open();

        var run = new CryptoEmulatorRun
        {
            StartedAt = GlobalData.Clock.UtcNow,
            ConfigJson = configJson,
            SettingsJson = settingsJson,
            GitSha = gitSha,
        };
        run.Id = (int)database.Connection.Insert(run);

        GlobalData.CurrentEmulatorRunId = run.Id;
        return run;
    }

    /// <summary>
    /// Marks the active run as finished — updates FinishedAt, Result and the
    /// signal/position counters — and clears <see cref="GlobalData.CurrentEmulatorRunId"/>.
    /// </summary>
    public static void FinishRun(string result)
    {
        int? runId = GlobalData.CurrentEmulatorRunId;
        if (runId == null)
            return;

        using var database = new CryptoDatabase();
        database.Open();

        var run = database.Connection.Get<CryptoEmulatorRun>(runId.Value);
        if (run != null)
        {
            run.FinishedAt = GlobalData.Clock.UtcNow;
            run.Result = result;
            run.SignalCount = database.Connection.ExecuteScalar<int>(
                "select count(*) from signal where EmulatorRunId = @id", new { id = runId });
            run.PositionCount = database.Connection.ExecuteScalar<int>(
                "select count(*) from position where EmulatorRunId = @id", new { id = runId });
            database.Connection.Update(run);
        }

        GlobalData.CurrentEmulatorRunId = null;
    }
}
