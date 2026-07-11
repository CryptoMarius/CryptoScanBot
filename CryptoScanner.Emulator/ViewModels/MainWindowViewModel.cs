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
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Zones;
using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.Views;
using CryptoScanner.ViewModels;

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
    private string _title = $"{Constants.AppName} {GlobalData.AppVersion} {GlobalData.ActiveExchange?.Name} — Emulator".Trim();

    [ObservableProperty]
    private string _dataFolder = GlobalData.AppDataFolder;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private int _progressValue;

    public string ProgressLabel => ProgressValue > 0 ? $"Progress: {ProgressValue}%" : "Progress:";

    partial void OnProgressValueChanged(int value) => OnPropertyChanged(nameof(ProgressLabel));

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
    /// Re-opens the SetupWindow so the user can pick a different data folder or exchange.
    /// Delegates the heavy lifting (re-bootstrap, window swap) to <see cref="App.SwitchDatabaseAsync"/>.
    /// </summary>
    [RelayCommand]
    private async Task ChangeDatabaseAsync(Window? owner)
    {
        if (owner == null)
            return;
        await App.SwitchDatabaseAsync(owner);
    }


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
    /// For every symbol listed in CryptoScanBot-Emulator.json, fetch the 1m candles needed for
    /// the backtest window from the exchange, then derive all higher intervals locally via
    /// <see cref="CandleTools.BulkCalculateCandles"/>. Only one REST stream per symbol instead of
    /// one per interval, which is the main driver of fetch time on long windows.
    ///
    /// The 1m warmup window is the largest of what the indicators need
    /// (<see cref="IndicatorWarmup.ComputeWarmupMinutes"/>) and what the chart needs
    /// (WindowMarginCandles + WindowCalcWarmupCandles expressed in minutes). Higher intervals are
    /// derived in ascending-duration order — IntervalList is already sorted that way — so a 4h
    /// candle built from 1h finds the 1h list already populated when its turn comes.
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
            Status = "Run config has no symbols — edit CryptoScanBot-Emulator.json first.";
            return;
        }

        if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
        {
            Status = "1m interval not registered — cannot fetch candles.";
            return;
        }

        IsRunning = true;
        int total = config.Symbols.Count;
        int symbolIdx = 0;

        List<CryptoInterval> intervals = GlobalData.IntervalList;
        GlobalData.AddTextToLogTab(
            $"Fetch candles: {total} symbol(s), window {config.FromDate:yyyy-MM-dd HH:mm}..{config.ToDate:yyyy-MM-dd HH:mm} UTC, 1m from exchange + higher derived locally");

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

                    // ── Step 1: fetch 1m from the exchange ───────────────────────────────────
                    // The warmup must cover whichever is bigger: what the indicators need
                    // (ComputeWarmupMinutes) or what the chart needs (WindowMarginCandles +
                    // WindowCalcWarmupCandles). The 1m window is large enough to cover every
                    // higher interval's warmup as well once BulkCalculateCandles derives them.
                    // Zone depth (DLZ CandleCount) is NOT included here — PrepareSymbol loads
                    // those candles per interval directly from the DB at their own resolution.
                    uint chartMarginMinutes1m = (uint)((ChartWindowViewModel.WindowMarginCandles
                        + ChartWindowViewModel.WindowCalcWarmupCandles) * interval1m.Duration);
                    uint warmupMinutes1m = Math.Max(IndicatorWarmup.ComputeWarmupMinutes(interval1m), chartMarginMinutes1m);
                    DateTime from1m = config.FromDate.AddMinutes(-warmupMinutes1m);

                    CandleTime minDate1m = IntervalTools.StartOfIntervalCandle(
                        CandleTime.AlignFromDateTime(from1m, interval1m.Duration), interval1m.Duration);
                    CandleTime maxDate1m = IntervalTools.StartOfIntervalCandle(
                        CandleTime.AlignFromDateTime(config.ToDate, interval1m.Duration), interval1m.Duration);

                    if (maxDate1m > minDate1m)
                    {
                        int candleFetchCount1m = (int)((maxDate1m.Minutes - minDate1m.Minutes) / interval1m.Duration);
                        GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} 1m — " +
                            $"want {minDate1m.ToDateTime():yyyy-MM-dd HH:mm}..{maxDate1m.ToDateTime():yyyy-MM-dd HH:mm} ({candleFetchCount1m} bars)");
                        try
                        {
                            await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval1m, minDate1m, candleFetchCount1m);
                        }
                        catch (Exception sx)
                        {
                            GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} 1m — FAILED ({sx.Message})");
                        }
                    }

                    // ── Step 2: derive all higher intervals from their source interval ────────
                    // IntervalList is sorted ascending by IntervalPeriod, so when a higher interval
                    // is derived from an intermediate one (e.g. 4h from 1h) its source is always
                    // already populated by the time we reach it. Intervals without ConstructFrom
                    // (i.e. 1m itself) are skipped — 1m was fetched in step 1.
                    CandleTime nowTime = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 0);
                    foreach (CryptoInterval interval in intervals)
                    {
                        if (interval.ConstructFrom == null)
                            continue;

                        GlobalData.AddTextToLogTab($"Fetch candles: {symbol.Name} {interval.Name} — calculating from {interval.ConstructFrom.Name}");
                        CandleTools.BulkCalculateCandles(symbol, interval.ConstructFrom, interval, nowTime);
                        loadedCandlesInMemory[interval.IntervalPeriod] = true;
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
    /// the user hand-edit CryptoScanBot-Emulator.json. The dialog saves to that same file on OK; we only
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
            GlobalData.AddTextToLogTab("Run parameters saved to CryptoScanBot-Emulator.json.");
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
            Status = "Run config has no symbols — edit CryptoScanBot-Emulator.json first.";
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
        try
        {
            await RunOnceAsync(config);
        }
        finally
        {
            IsRunning = false;
        }
    }


    /// <summary>
    /// Lets the user pick a subset of the registered algorithms (defaults to all selected), then
    /// runs each of them 1-by-1 with the same symbols/period from CryptoScanBot-Emulator.json, each time as a
    /// long+short analyzer/trader run (so a single algorithm's full signal set is exercised on its
    /// own, without competing with the others for slots/capital). Every algorithm gets its own
    /// EmulatorRun row; the row's label is "{algorithm name} {rest of the configured label}" so the
    /// algorithm name is always the run label's first word, per the user's request. Stops the whole
    /// batch (instead of moving to the next algorithm) if the user hits Stop mid-run.
    /// </summary>
    [RelayCommand]
    private async Task RunAllAlgorithmsAsync(Window? owner)
    {
        if (IsRunning)
            return;

        EmulatorRunConfig baseConfig;
        try
        {
            baseConfig = RunConfigFile.Load();
        }
        catch (Exception ex)
        {
            Status = $"Failed to read run config: {ex.Message}";
            return;
        }

        var selectionWindow = new AlgorithmSelectionWindow(baseConfig.SelectedAlgorithms);
        bool confirmed = owner != null
            ? await selectionWindow.ShowDialog<bool>(owner)
            : false;

        if (!confirmed || !selectionWindow.ViewModel.TryGetSelection(out List<string> selectedNames))
            return;

        // Persist the chosen subset so the next dialog restores it (only the selection is updated;
        // symbols/period/label on disk stay untouched).
        baseConfig.SelectedAlgorithms = selectedNames;
        try
        {
            RunConfigFile.Save(baseConfig);
        }
        catch (Exception ex)
        {
            Status = $"Failed to save run config: {ex.Message}";
        }

        var selectedAlgorithms = selectedNames
            .Select(name => RegisterAlgorithms.AlgorithmDefinitionList.Values.First(a => a.Name == name))
            .ToList();

        if (baseConfig.Symbols.Count == 0)
        {
            Status = "Run config has no symbols — edit CryptoScanBot-Emulator.json first.";
            return;
        }

        if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out var exchange))
        {
            Status = "Run needs an exchange";
            return;
        }

        GlobalData.ActiveExchange = exchange;
        GlobalData.Settings.General.ExchangeName = baseConfig.ExchangeName;
        GlobalData.Settings.General.ActivateExchangeName = baseConfig.ExchangeName;

        // The configured label's first word is replaced per-algorithm below; anything after the
        // first word (extra notes the user typed) is kept and appended to every algorithm's label.
        string[] labelParts = baseConfig.Label.Split(' ', 2, StringSplitOptions.None);
        string labelRest = labelParts.Length > 1 ? labelParts[1] : "";

        // Snapshot the strategy lists the user configured (Configure dialog / settings.json) so the
        // per-algorithm overrides below never leak past this batch. Without the restore the in-memory
        // Settings would keep the LAST algorithm's strategy, which a later plain Start — or a
        // Configure→Save — would then use/persist instead of the user's real selection.
        List<string> savedSignalLongStrategy = GlobalData.Settings.Signal.Long.Strategy;
        List<string> savedSignalShortStrategy = GlobalData.Settings.Signal.Short.Strategy;
        List<string> savedTradingLongStrategy = GlobalData.Settings.Trading.Long.Strategy;
        List<string> savedTradingShortStrategy = GlobalData.Settings.Trading.Short.Strategy;

        IsRunning = true;
        try
        {
            foreach (AlgorithmDefinition algorithm in selectedAlgorithms)
            {
                // Isolate the analyzer and trader to this single algorithm, both sides — so the
                // batch run measures each algorithm on its own, the same way a manual single-algo
                // run would be set up by hand.
                GlobalData.Settings.Signal.Long.Strategy = [algorithm.Name];
                GlobalData.Settings.Signal.Short.Strategy = [algorithm.Name];
                GlobalData.Settings.Trading.Long.Strategy = [algorithm.Name];
                GlobalData.Settings.Trading.Short.Strategy = [algorithm.Name];

                EmulatorRunConfig algoConfig = new()
                {
                    ExchangeName = baseConfig.ExchangeName,
                    Symbols = baseConfig.Symbols,
                    FromDate = baseConfig.FromDate,
                    ToDate = baseConfig.ToDate,
                    Label = labelRest.Length > 0 ? $"{algorithm.Name} {labelRest}" : algorithm.Name,
                };

                bool completed = await RunOnceAsync(algoConfig);
                if (!completed)
                    break; // Stop was pressed (or the run failed) — abandon the rest of the batch.
            }
        }
        finally
        {
            // Restore the user's configured strategy lists — the per-algorithm overrides were transient.
            GlobalData.Settings.Signal.Long.Strategy = savedSignalLongStrategy;
            GlobalData.Settings.Signal.Short.Strategy = savedSignalShortStrategy;
            GlobalData.Settings.Trading.Long.Strategy = savedTradingLongStrategy;
            GlobalData.Settings.Trading.Short.Strategy = savedTradingShortStrategy;
            IsRunning = false;
        }
    }




    /// <summary>
    /// Runs every entry in the queue file (<c>CryptoScanBot-Emulator-Queue.json</c>) as a separate
    /// emulator run, per selected algorithm. Each entry supplies its own SL%, TP list and DCA ladder
    /// — no matrix explosion. Symbols, period and exchange come from the regular
    /// <c>CryptoScanBot-Emulator.json</c>. The algorithm selection dialog is shown up front so
    /// the user picks once and the full (algorithm × queue) batch runs unattended.
    /// </summary>
    [RelayCommand]
    private async Task RunQueueAsync(Window? owner)
    {
        if (IsRunning)
            return;

        EmulatorRunConfig baseConfig;
        try
        {
            baseConfig = RunConfigFile.Load();
        }
        catch (Exception ex)
        {
            Status = $"Failed to read run config: {ex.Message}";
            return;
        }

        if (baseConfig.Symbols.Count == 0)
        {
            Status = "Run config has no symbols — edit CryptoScanBot-Emulator.json first.";
            return;
        }

        List<EmulatorQueueEntry> queue;
        try
        {
            queue = EmulatorQueueFile.Load();
        }
        catch (Exception ex)
        {
            Status = $"Failed to read queue file: {ex.Message}";
            return;
        }

        if (queue.Count == 0)
        {
            Status = $"Queue is empty — add entries to {EmulatorQueueFile.FileName}.";
            GlobalData.AddTextToLogTab($"Queue: file loaded from {EmulatorQueueFile.FilePath} but contains 0 entries");
            return;
        }

        GlobalData.AddTextToLogTab($"Queue: loaded {queue.Count} entries from {EmulatorQueueFile.FilePath}");

        // Collect the distinct algorithm names present in the queue so we can skip the
        // selection dialog when every entry already specifies its algorithm.
        var queueAlgorithmNames = queue
            .Where(e => !string.IsNullOrEmpty(e.Algorithm))
            .Select(e => e.Algorithm!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> selectedNames;
        if (queueAlgorithmNames.Count > 0 && queue.All(e => !string.IsNullOrEmpty(e.Algorithm)))
        {
            // Every entry has an Algorithm — no need for the selection dialog, just use the
            // algorithms present in the queue.
            selectedNames = queueAlgorithmNames;
            GlobalData.AddTextToLogTab($"Queue: all entries have Algorithm set, skipping selection dialog ({string.Join(", ", selectedNames)})");
        }
        else
        {
            var selectionWindow = new AlgorithmSelectionWindow(baseConfig.SelectedAlgorithms);
            bool confirmed = owner != null
                ? await selectionWindow.ShowDialog<bool>(owner)
                : false;

            if (!confirmed || !selectionWindow.ViewModel.TryGetSelection(out selectedNames))
                return;
        }

        baseConfig.SelectedAlgorithms = selectedNames;
        try
        {
            RunConfigFile.Save(baseConfig);
        }
        catch (Exception ex)
        {
            Status = $"Failed to save run config: {ex.Message}";
        }

        var selectedAlgorithms = selectedNames
            .Select(name => RegisterAlgorithms.AlgorithmDefinitionList.Values.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(a => a != null)
            .ToList()!;

        if (selectedAlgorithms.Count == 0)
        {
            Status = $"No matching registered algorithms found for: {string.Join(", ", selectedNames)}";
            GlobalData.AddTextToLogTab($"Queue: none of the selected algorithms ({string.Join(", ", selectedNames)}) are registered");
            return;
        }

        if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out var exchange))
        {
            Status = "Run needs an exchange";
            return;
        }

        GlobalData.ActiveExchange = exchange;
        GlobalData.Settings.General.ExchangeName = baseConfig.ExchangeName;
        GlobalData.Settings.General.ActivateExchangeName = baseConfig.ExchangeName;

        List<string> savedSignalLongStrategy = GlobalData.Settings.Signal.Long.Strategy;
        List<string> savedSignalShortStrategy = GlobalData.Settings.Signal.Short.Strategy;
        List<string> savedTradingLongStrategy = GlobalData.Settings.Trading.Long.Strategy;
        List<string> savedTradingShortStrategy = GlobalData.Settings.Trading.Short.Strategy;
        decimal savedStopLossPercentage = GlobalData.Settings.Trading.StopLossPercentage;
        decimal savedStopLossLimitPercentage = GlobalData.Settings.Trading.StopLossLimitPercentage;
        List<CryptoTpEntry> savedTpList = GlobalData.Settings.Trading.TpList;
        List<CryptoDcaEntry> savedDcaList = GlobalData.Settings.Trading.DcaList;

        int totalRuns = selectedAlgorithms.Sum(a =>
            queue.Count(e => string.IsNullOrEmpty(e.Algorithm) || e.Algorithm.Equals(a!.Name, StringComparison.OrdinalIgnoreCase)));
        int runIndex = 0;

        GlobalData.AddTextToLogTab($"Queue: starting {totalRuns} runs across {selectedAlgorithms.Count} algorithm(s)");

        IsRunning = true;
        try
        {
            foreach (AlgorithmDefinition algorithm in selectedAlgorithms!)
            {
                GlobalData.Settings.Signal.Long.Strategy = [algorithm.Name];
                GlobalData.Settings.Signal.Short.Strategy = [algorithm.Name];
                GlobalData.Settings.Trading.Long.Strategy = [algorithm.Name];
                GlobalData.Settings.Trading.Short.Strategy = [algorithm.Name];

                for (int i = 0; i < queue.Count; i++)
                {
                    EmulatorQueueEntry entry = queue[i];

                    if (!string.IsNullOrEmpty(entry.Algorithm)
                        && !entry.Algorithm.Equals(algorithm.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    runIndex++;

                    GlobalData.Settings.Trading.StopLossPercentage = entry.StopLossPercentage;
                    GlobalData.Settings.Trading.StopLossLimitPercentage = entry.StopLossPercentage + 1m;

                    if (entry.TpList.Count > 0)
                        GlobalData.Settings.Trading.TpList = entry.TpList
                            .Select(e => new CryptoTpEntry { Factor = e.Factor, Percentage = e.Percentage })
                            .ToList();

                    GlobalData.Settings.Trading.DcaList = entry.DcaList
                        .Select(e => new CryptoDcaEntry { Factor = e.Factor, Percentage = e.Percentage })
                        .ToList();

                    List<SignalGridExpander.Override> overrides = SignalGridExpander.Apply(entry);
                    try
                    {
                        string entryLabel = !string.IsNullOrWhiteSpace(entry.Label) ? entry.Label : $"queue-{i + 1}";
                        string algoName = !string.IsNullOrEmpty(entry.Algorithm) ? entry.Algorithm : algorithm.Name;
                        string runLabel = $"{algoName} {entryLabel}";

                        EmulatorRunConfig runConfig = new()
                        {
                            ExchangeName = baseConfig.ExchangeName,
                            Symbols = baseConfig.Symbols,
                            FromDate = baseConfig.FromDate,
                            ToDate = baseConfig.ToDate,
                            Label = runLabel,
                        };

                        Status = $"Queue {runIndex}/{totalRuns}: {algoName} — {entryLabel}";
                        bool completed = await RunOnceAsync(runConfig);
                        if (!completed)
                            return;
                    }
                    finally
                    {
                        SignalGridExpander.Revert(overrides);
                    }
                }
            }
        }
        finally
        {
            GlobalData.Settings.Signal.Long.Strategy = savedSignalLongStrategy;
            GlobalData.Settings.Signal.Short.Strategy = savedSignalShortStrategy;
            GlobalData.Settings.Trading.Long.Strategy = savedTradingLongStrategy;
            GlobalData.Settings.Trading.Short.Strategy = savedTradingShortStrategy;
            GlobalData.Settings.Trading.StopLossPercentage = savedStopLossPercentage;
            GlobalData.Settings.Trading.StopLossLimitPercentage = savedStopLossLimitPercentage;
            GlobalData.Settings.Trading.TpList = savedTpList;
            GlobalData.Settings.Trading.DcaList = savedDcaList;
            IsRunning = false;
        }
    }


    /// <summary>
    /// Drives a single replay end-to-end: applies the run overrides, opens an EmulatorRun row,
    /// runs the TickRunner and records the outcome. Shared by <see cref="StartAsync"/> (one run)
    /// and <see cref="RunAllAlgorithmsAsync"/> (one run per algorithm) — neither touches
    /// <see cref="IsRunning"/> here, that's the caller's responsibility since the batch commands
    /// keep it true across multiple calls.
    /// </summary>
    /// <returns>True if the run completed normally; false if it was cancelled or failed.</returns>
    private async Task<bool> RunOnceAsync(EmulatorRunConfig config)
    {
        ProgressValue = 0;
        Status = $"Starting run \"{config.Label}\"";

        ApplyRunOverrides(config);

        // Put the DB into WAL mode so the per-tick Flush transactions don't each fsync a
        // freshly created/deleted rollback journal. Persistent in the DB file, so it covers
        // every connection the run opens. Core is untouched.
        EmulatorDb.EnableFastWriteMode();

        _cts = new CancellationTokenSource();
        CryptoEmulatorRun? run = null;
        bool completed = false;

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
                RunParallel = true,
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
                completed = true;
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

            _cts?.Dispose();
            _cts = null;
        }

        return completed;
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

        // Clear the live-dashboard dedupe queue so a previous run's (symbol, interval) entries
        // do not suppress that combination's first live update in this run.
        GlobalData.LiveDataQueue.Clear();
        GlobalData.LiveDataQueueAdded.Clear();

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
        Dispatcher.UIThread.Post(() => ProgressValue = p.Percent);
    }
}
