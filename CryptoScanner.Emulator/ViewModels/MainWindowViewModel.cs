using Avalonia.Controls;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Config.Views;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Zones;
using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.Views;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// Glue between the placeholder MainWindow and the emulator engine. Holds run state, exposes
/// commands for the three buttons (Configure scanner, Open run.json, Start/Stop), and surfaces
/// progress for the status bar. Deliberately minimal — once the engine is proven we can swap
/// the JSON file for a proper symbols/dates form.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// Window title, same shape as the live scanner's (AppName + version + exchange). Bound to the
    /// custom title bar's title text and the Window.Title. Built once at construction; the active
    /// exchange is already known by the time the MainWindow is created (bootstrap runs first).
    /// </summary>
    [ObservableProperty]
    private string _title =
        $"{Constants.AppName} {GlobalData.AppVersion} {GlobalData.ActiveExchange?.Name} — Emulator".Trim();

    [ObservableProperty]
    private string _appVersion = GlobalData.AppVersion;

    [ObservableProperty]
    private string _appPath = GlobalData.AppPath;

    [ObservableProperty]
    private string _dataFolder = GlobalData.AppDataFolder;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private double _progressMaximum = 1;

    [ObservableProperty]
    private string _currentSymbol = "";

    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// Single LogTab VM lives for the whole MainWindow lifetime so it can keep accumulating
    /// log lines across runs. The Log tab's DataContext is bound to this property.
    /// </summary>
    public LogTabViewModel LogTab { get; } = new();

    /// <summary>
    /// Backs the Results tab next to the Log tab. Lives for the whole MainWindow lifetime and is
    /// re-queried (<see cref="RunResultsViewModel.Refresh"/>) whenever a run finishes, so the grid
    /// reflects the latest EmulatorRun rows without the user reopening anything.
    /// </summary>
    public RunResultsViewModel RunResults { get; } = new();


    private CancellationTokenSource? _cts;


    /// <summary>
    /// Opens the scanner ConfigurationWindow as a modal dialog rooted at this window. The
    /// dialog reads from and writes back to GlobalData.Settings; the settings.json the
    /// emulator just loaded is the one being edited. The dialog itself only updates the
    /// in-memory Settings object — without persisting here every restart would reload the
    /// old file and the user would lose their edits.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureScannerAsync(Window? owner)
    {
        ConfigurationWindow window = new();

        // ConfigurationViewModel signals OK/Cancel through Window.Close(true|false); capture
        // the bool result so a Cancel doesn't persist whatever the user was poking at when
        // they realised they wanted to abandon. ShowDialog<T> returns T?; null is treated as
        // a cancel (e.g. window closed via the X button).
        bool confirmed = false;
        if (owner != null)
            confirmed = await window.ShowDialog<bool>(owner);
        else
            window.Show();

        if (!confirmed)
        {
            GlobalData.AddTextToLogTab("Settings dialog cancelled — nothing saved.");
            return;
        }

        // Persist what the dialog wrote into GlobalData.* back to settings.json + the other
        // sidecar files (telegram, altrady, weblinks). Same call live App.axaml.cs makes on
        // shutdown — we do it eagerly so the change survives a crash too.
        GlobalData.SaveConfiguration();
        GlobalData.AddTextToLogTab("Settings saved.");

        // The user may have changed the dark/light theme in the dialog; apply it right away so the
        // emulator reflects the new choice without needing a restart.
        App.ApplyThemeFromSettings();
    }


    /// <summary>
    /// Fetches the symbol list for the active exchange (REST call, no websocket subscriptions).
    /// Mirrors what ThreadLoadData does at scanner startup; needed before Configure can show
    /// quotes and before TickRunner has any symbols to drive.
    /// </summary>
    [RelayCommand]
    private async Task FetchSymbolsAsync()
    {
        if (GlobalData.ActiveExchange == null)
        {
            Status = "No active exchange — restart and pick one in the Setup dialog.";
            return;
        }

        IsRunning = true;
        Status = $"Fetching symbols for {GlobalData.ActiveExchange.Name}…";
        GlobalData.AddTextToLogTab($"Fetch symbols: {GlobalData.ActiveExchange.Name} — start");

        try
        {
            // CRITICAL: re-sync the in-memory SymbolListName from the DB before invoking the
            // REST fetch. The Symbol table has NO UNIQUE constraint on (ExchangeId, Name); the
            // entire dedup logic in IsSymbolAccepted (SymbolBase.cs) runs against this cache.
            // If the cache is empty or out of sync, every Fetch click reinserts the full list,
            // producing duplicates in the DB. Bootstrap already does this once at startup,
            // but explicitly repeating it here is cheap (AddSymbol skips known keys) and
            // covers state corruption scenarios (cache lost, parallel writes from elsewhere).
            int beforeCount = GlobalData.ActiveExchange.SymbolListName.Count;
            GlobalData.LoadSymbols();
            int afterLoadCount = GlobalData.ActiveExchange.SymbolListName.Count;
            GlobalData.AddTextToLogTab($"Fetch symbols: in-memory cache synced with DB ({beforeCount} → {afterLoadCount})");

            // Same call the live scanner makes (ThreadLoadData.cs). Hits the exchange REST
            // API, writes/updates rows in the Symbol table, sets LastTimeFetched.
            await GlobalData.ActiveExchange.GetApiInstance().Symbol.GetSymbolsAsync();

            // Refresh the in-memory symbol caches from the DB so Configure and run-time code
            // see what we just persisted.
            GlobalData.LoadSymbols();

            // Wire the freshly-known symbols into their quote-side index so Configure shows
            // the new per-quote counts immediately (otherwise it stays empty until next start).
            ThreadLoadData.IndexQuoteDataSymbols(GlobalData.ActiveExchange);

            // Same notification the live scanner sends so any future grid/Combo bindings
            // refresh themselves.
            GlobalData.SendMvvmMessage(new SymbolsHaveChangedMessage());

            int count = GlobalData.ActiveExchange.SymbolListName.Count;
            Status = $"Fetched {count} symbols for {GlobalData.ActiveExchange.Name}.";
            GlobalData.AddTextToLogTab($"Fetch symbols: {GlobalData.ActiveExchange.Name} — done ({count} symbols)");
        }
        catch (Exception ex)
        {
            Status = $"Fetch symbols failed: {ex.Message}";
            GlobalData.AddTextToLogTab($"Fetch symbols: FAILED — {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }


    /// <summary>
    /// For every symbol listed in emulator-run.json, fetch the candles needed for the backtest
    /// window per active interval. Reuses the DLZ "inzoomen" routine
    /// <see cref="Zones.ZoneCandleEngine.FetchFrom"/> exactly the way
    /// <see cref="Zones.ZoneDlz.LoadHistoricCandles"/> does: we only compute the wanted range
    /// (a <c>minDate</c> + <c>candleFetchCount</c>) and hand the rest to that routine. It already
    /// does everything that's needed — materialise what's on disk/candles.db into the in-memory
    /// CandleList, verify with its <c>IsDataLocal</c> walk how much of <c>minDate..maxDate</c> we
    /// already have, and then call <see cref="CandleBase.FetchFrom"/> ONLY for the candles still
    /// missing (from the first gap onward). No hand-rolled coverage logic here — reusing that
    /// routine IS the point.
    ///
    /// Only the intervals <see cref="IndicatorWarmup.ResolveActiveIntervals"/> reports are
    /// fetched — pulling 1d/1w candles for a strategy that never touches them is wasted work.
    /// </summary>
    [RelayCommand]
    private async Task FetchCandlesAsync()
    {
        if (GlobalData.ActiveExchange == null)
        {
            Status = "No active exchange — restart and pick one in the Setup dialog.";
            return;
        }

        EmulatorRunConfig config;
        try
        {
            config = RunConfigFile.Load();
        }
        catch (Exception ex)
        {
            Status = $"Failed to read run config: {ex.Message}";
            return;
        }

        if (config.Symbols.Count == 0)
        {
            Status = "Run config has no symbols — edit emulator-run.json first.";
            return;
        }

        IsRunning = true;
        int total = config.Symbols.Count;
        int symbolIdx = 0;

        List<CryptoInterval> activeIntervals = IndicatorWarmup.ResolveActiveIntervals();
        string intervalNames = string.Join(", ", activeIntervals.Select(i => i.Name));
        GlobalData.AddTextToLogTab(
            $"Fetch candles: {total} symbol(s), window {config.FromDate:yyyy-MM-dd HH:mm}..{config.ToDate:yyyy-MM-dd HH:mm} UTC, active intervals: {intervalNames}");

        try
        {
            // Run the whole fetch on a background thread so the UI stays responsive, same as the
            // replay run. ZoneCandleEngine.FetchFrom is async (network I/O) but the work between
            // the awaits — IsDataLocal walks, candle aggregation, candles.db reads/writes — is
            // synchronous CPU/disk work that would otherwise stutter the UI on a big fetch.
            // Status is a UI-bound property, so every assignment inside is marshalled back to the
            // UI thread; AddTextToLogTab already marshals itself via the Log tab.
            await Task.Run(async () =>
            {
                foreach (string symbolName in config.Symbols)
                {
                    symbolIdx++;
                    if (!GlobalData.ActiveExchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        SetStatus($"Symbol '{symbolName}' not in exchange list — did you Fetch symbols first?");
                        GlobalData.AddTextToLogTab($"Fetch candles: SKIP {symbolName} — not in exchange list");
                        continue;
                    }

                    SetStatus($"Fetching candles for {symbol.Name} ({symbolIdx}/{total})…");

                    // One dict per symbol, exactly like the DLZ zone-calculation path: it tells
                    // ZoneCandleEngine.FetchFrom which intervals it has already materialised from
                    // disk/candles.db into the in-memory CandleList, so the IsDataLocal verify
                    // step sees the full picture instead of only the bounded startup load.
                    SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

                    foreach (CryptoInterval interval in GlobalData.IntervalList)
                    {
                        // Per-interval warmup window — 1m=24h, higher=270×duration. Each interval
                        // pulled in its OWN resolution: a 1w warmup is 270 weekly candles (~5y),
                        // not 5 years of 1m bars. minDate..maxDate is the window we WANT;
                        // ZoneCandleEngine.FetchFrom figures out how much of it we already have and
                        // fetches only the rest.
                        uint warmupMinutes = IndicatorWarmup.ComputeWarmupMinutes(interval);
                        DateTime intervalFromUtc = config.FromDate.AddMinutes(-warmupMinutes);

                        CandleTime minDate = IntervalTools.StartOfIntervalCandle(
                            CandleTime.AlignFromDateTime(intervalFromUtc, interval.Duration), interval.Duration);
                        CandleTime maxDate = IntervalTools.StartOfIntervalCandle(
                            CandleTime.AlignFromDateTime(config.ToDate, interval.Duration), interval.Duration);

                        if (interval.IntervalPeriod > CryptoIntervalPeriod.interval1m)
                            maxDate = IntervalTools.StartOfIntervalCandle(
                            CandleTime.AlignFromDateTime(config.FromDate, interval.Duration), interval.Duration);

                        if (maxDate <= minDate)
                            continue;

                        // candleFetchCount is the bar-count of the wanted range; ZoneCandleEngine's
                        // CalculateDates rebuilds maxDate as minDate + count*duration (and caps it
                        // at "now"), so passing the count keeps us inside the DLZ contract.
                        int candleFetchCount = (int)((maxDate.Minutes - minDate.Minutes) / interval.Duration);

                        GlobalData.AddTextToLogTab(
                            $"Fetch candles: {symbol.Name} {interval.Name} — want {minDate.ToDateTime():yyyy-MM-dd HH:mm}..{maxDate.ToDateTime():yyyy-MM-dd HH:mm} ({candleFetchCount} bars)");

                        try
                        {
                            await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval, minDate, candleFetchCount);
                        }
                        catch (Exception sx)
                        {
                            GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} {interval.Name} — FAILED ({sx.Message})");
                        }
                    }

                    GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} — done");
                }

                // Persist the in-memory CandleLists to {Exchange}.db. Without this, everything
                // the REST fetch just pulled lives only in memory and is gone on the next start
                // — so each launch would have to refetch from the exchange. SaveCandlesAsync is
                // the same call the live scanner makes during ScannerSession shutdown; bulk
                // upserts via SQLite transactions, so calling it once at the end of the fetch is
                // far cheaper than after every symbol.
                SetStatus("Saving candles to database…");
                GlobalData.AddTextToLogTab("Fetch candles: persisting to database");
                await CandleDatabase.SaveCandlesAsync();
            });

            Status = $"Fetch candles: completed for {total} symbol(s).";
            GlobalData.AddTextToLogTab($"Fetch candles: completed ({total} symbol(s))");
        }
        catch (Exception ex)
        {
            Status = $"Fetch candles failed: {ex.Message}";
            GlobalData.AddTextToLogTab($"Fetch candles: FAILED — {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }


    /// <summary>
    /// Sets the UI-bound <see cref="Status"/> from any thread. Fetch/run work happens on a
    /// background thread (Task.Run) but Status drives a binding, so the assignment is marshalled
    /// to the UI thread. Posting from the UI thread itself is fine — it just queues.
    /// </summary>
    private void SetStatus(string text) => Dispatcher.UIThread.Post(() => Status = text);


    /// <summary>
    /// Opens the run-parameters dialog (label, replay period, symbol selection) instead of making
    /// the user hand-edit emulator-run.json. The dialog saves to that same file on OK; we only
    /// surface the result in the log.
    /// </summary>
    [RelayCommand]
    private async Task EditRunConfigAsync(Window? owner)
    {
        var window = new RunConfigWindow();

        bool saved = false;
        if (owner != null)
            saved = await window.ShowDialog<bool>(owner);
        else
            window.Show();

        if (saved)
            GlobalData.AddTextToLogTab("Run parameters saved to emulator-run.json.");
    }


    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
            return;

        EmulatorRunConfig config;
        try
        {
            config = RunConfigFile.Load();
        }
        catch (Exception ex)
        {
            Status = $"Failed to read run config: {ex.Message}";
            return;
        }

        if (config.Symbols.Count == 0)
        {
            Status = "Run config has no symbols — edit emulator-run.json first.";
            return;
        }

        // ─── ScannerSession.ApplyConfigurationAsync subset ──────────────────────────
        // Bind the exchange the user/settings selected.
        if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out var exchange))
        {
            Status = "Run needs an exchange";
            return;
        }

        GlobalData.ActiveExchange = exchange;
        GlobalData.Settings.General.ExchangeName = config.ExchangeName;

        GlobalData.ActiveExchange = exchange;
        GlobalData.Settings.General.ActivateExchangeName = config.ExchangeName;

        IsRunning = true;
        ProgressValue = 0;
        Status = $"Starting run \"{config.Label}\"";

        ApplyRunOverrides(config);

        // Put the DB into WAL mode so the per-tick Flush transactions don't each fsync a
        // freshly created/deleted rollback journal. Persistent in the DB file, so it covers
        // every connection the run opens. Core is untouched.
        EmulatorDb.EnableFastWriteMode();

        _cts = new CancellationTokenSource();
        CryptoEmulatorRun? run = null;

        try
        {
            // Tag every signal and position with this run. ConfigJson captures the user's
            // intent (which symbols/period); SettingsJson is a full snapshot of the scanner
            // settings.json (GlobalData.Settings) at run start — serialized with the SAME options
            // the live SaveConfiguration uses — so the exact configuration behind a run can be
            // inspected and the "best" one restored later.
            string configJson = System.Text.Json.JsonSerializer.Serialize(config);
            string settingsJson = System.Text.Json.JsonSerializer.Serialize(
                GlobalData.Settings, CryptoScanner.Core.Json.JsonTools.JsonSerializerIndented);
            run = EmulatorDb.StartRun(configJson, config.FromDate, config.ToDate, config.Label, settingsJson);
            GlobalData.AddTextToLogTab($"Run #{run.Id} \"{config.Label}\" started: {config.Symbols.Count} symbol(s) {config.FromDate:yyyy-MM-dd} → {config.ToDate:yyyy-MM-dd}");

            var runner = new TickRunner
            {
                Progress = new Progress<TickRunProgress>(OnTickProgress),
                // Temporarily serial: testing whether the parallel symbol order (under slot/capital
                // contention) is what makes the emulator diverge from the serial live scanner.
                RunParallel = false,
            };

            // Run the replay on a background thread. Previously RunAsync was awaited directly on
            // the UI thread; even with periodic Task.Yield the engine work saturated the
            // dispatcher, so the Stop button's click (and thus _cts.Cancel()) was starved and the
            // run "couldn't be stopped". Offloading frees the UI thread to process Stop instantly;
            // the loop then sees the cancelled token at its next iteration. Progress<T> still
            // marshals OnTickProgress back to the UI thread (it captured the UI context here).
            await Task.Run(() => runner.RunAsync(config, _cts.Token), _cts.Token);

            // The TickRunner's replay loop breaks out cleanly on cancellation (it checks the token
            // at the top of each iteration) rather than throwing, so a Stop during replay returns
            // here normally — NOT via the OperationCanceledException catch below. Inspect the token
            // so a stopped run is recorded as "cancelled" instead of "completed". (Cancelling during
            // the warmup phase still throws and is handled by the catch.)
            if (_cts.IsCancellationRequested)
            {
                EmulatorDb.FinishRun("cancelled");
                Status = $"Run \"{config.Label}\" cancelled.";
                GlobalData.AddTextToLogTab($"Run #{run.Id} cancelled");
            }
            else
            {
                EmulatorDb.FinishRun("completed");
                Status = $"Run \"{config.Label}\" completed.";
                GlobalData.AddTextToLogTab($"Run #{run.Id} completed");
            }
        }
        catch (OperationCanceledException)
        {
            EmulatorDb.FinishRun("cancelled");
            Status = $"Run \"{config.Label}\" cancelled.";
            GlobalData.AddTextToLogTab($"Run cancelled");
        }
        catch (Exception ex)
        {
            EmulatorDb.FinishRun($"failed: {ex.GetType().Name}");
            Status = $"Run failed: {ex.Message}";
            GlobalData.AddTextToLogTab($"Run FAILED — {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            // Persist any candles the run pulled in beyond what the up-front fetch stored.
            // The DLZ zone calculation (ZoneDlz.CalculateZonesAsync → ZoneCandleEngine.FetchFrom)
            // fetches its own CandleCount window per zone interval, which can reach further back
            // than our warmup window — those candles land in the in-memory CandleList but the
            // per-tick SaveCandleDataToDiskAsync is intentionally disabled during replay (see
            // ZoneThreadCalculate). A single save here captures them; the replay's own candles
            // are already in the DB so re-upserting them is a cheap idempotent no-op (composite
            // PK on Candle). Runs in finally so a cancelled or failed run still keeps whatever
            // was fetched, avoiding a needless refetch next time.
            try
            {
                GlobalData.AddTextToLogTab("Run: persisting fetched candles to database");
                await CandleDatabase.SaveCandlesAsync();
            }
            catch (Exception sx)
            {
                GlobalData.AddTextToLogTab($"Run: persisting candles FAILED — {sx.Message}");
            }

            // The run just added/updated its EmulatorRun row (and its signals/positions); pull
            // the fresh numbers into the Results tab so it reflects this run immediately.
            RunResults.Refresh();

            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }


    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
        Status = "Cancelling…";
    }


    /// <summary>
    /// Run-start safety overrides. The emulator must never accidentally drive the real
    /// exchange or starve waiting for live services, so this method forces the bits the
    /// engine depends on:
    ///   • ThreadCheckPosition exists (NewCandleArrivedAsync calls AddToQueue on it).
    ///   • Trading goes through PaperTrade — never RealTrading, no matter what settings.json
    ///     contained. The emulator's own paper-trade logic fills orders on candle high/low.
    ///   • For every symbol in the run config the quote is allowed to fetch candles and uses
    ///     the run's EntryAmount as the per-trade size, overriding settings.json. Without
    ///     FetchCandles the live NewCandleArrivedAsync skips the symbol entirely.
    /// </summary>
    private static void ApplyRunOverrides(EmulatorRunConfig config)
    {
        // 1. Make sure a position-check thread exists. Live App.axaml.cs sets this via
        //    ScannerSession.Start; we don't run the session so we wire a bare instance.
        //    No Execute()/threads — the emulator only uses AddToQueue, which works in the
        //    IsEmulatorMode branch without a running worker loop.
        GlobalData.ThreadCheckPosition ??= new ThreadCheckFinishedPosition();

        // 1b. Wire a ThreadZoneCalculate instance so SignalPrepare.Execute's DLZ branch can
        //     enqueue (symbol, interval) work via AddToQueue. We do NOT start its ExecuteAsync
        //     worker loop — the TickRunner drains the queue synchronously after each tick
        //     (DrainQueueAsync) so zone calculation stays on the deterministic replay thread.
        //     Without this instance the AddToQueue null-conditional silently drops every DLZ
        //     recalculation and no DLZ zones would ever form during a run.
        GlobalData.ThreadZoneCalculate ??= new ZoneThreadCalculate();

        // 1c. Wire a ThreadSaveObjects instance. Signal/position/zone persistence all go through
        //     GlobalData.ThreadSaveObjects!.AddToQueue(...) with a null-forgiving '!' — without
        //     an instance the very first created signal or zone diff throws a NullReferenceException.
        //     As with the zone worker we do NOT start its background Execute loop; the TickRunner
        //     calls Flush() synchronously at each tick boundary so the DB is current before
        //     ZoneDlz.LoadZonesForSymbol reloads zones on the next calculation.
        GlobalData.ThreadSaveObjects ??= new ThreadSaveObjects();


        var exchange = GlobalData.ActiveExchange!;

        // Clear positions, assets etc
        exchange.Data.Clear();

        exchange.GetApiInstance().ExchangeDefaults();

        // Clear symbols and refresh
        exchange.Clear();
        GlobalData.LoadSymbols();
        
        // Force paper-trading. The user's settings.json is otherwise authoritative;
        // overriding here protects against accidental RealTrading after a Configure edit.
        GlobalData.Settings.Signal.Active = true;
        GlobalData.Settings.Trading.Active = true;
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        // Start from a clean IN-MEMORY zone slate. Stored zones are now tagged per run (EmulatorRunId)
        // and loaded per run, so a fresh run already starts with no zones of its own — no DB wipe and no
        // look-ahead from a previous run. This only clears the in-memory lists so the first inline
        // FVG/SMC scan does not see leftover zones from an earlier run in the same app session.
        EmulatorDb.ClearZonesForSymbols(exchange, config.Symbols);
        GlobalData.AddTextToLogTab($"Run: cleared in-memory zones for {config.Symbols.Count} symbol(s)");

        // Activte quoteData
        foreach (string symbolName in config.Symbols)
        {
            if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                continue;
            if (symbol.QuoteData == null)
                continue;

            symbol.QuoteData.FetchCandles = true;
        }

        // Just to be sure, do the basic stuff
        GlobalData.IndexStrategySettings();
        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        SignalPrepare.Prepare();
        SignalExecute.Prepare();

        GlobalData.LoadAssets(); // not sure if we need this (papertrading perhaps?)    
    }


    private void OnTickProgress(TickRunProgress p)
    {
        // The Progress<T> callback already marshals to the UI thread when constructed on the
        // UI thread; the explicit Post is defensive in case this VM ever runs in a worker.
        Dispatcher.UIThread.Post(() =>
        {
            CurrentSymbol = p.SymbolName;
            ProgressMaximum = Math.Max(1, p.TotalBars);
            ProgressValue = p.ProcessedBars;
            Status = $"{p.SymbolName}: {p.ProcessedBars}/{p.TotalBars}";
        });
    }
}
