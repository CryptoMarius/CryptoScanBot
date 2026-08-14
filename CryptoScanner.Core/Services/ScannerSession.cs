using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Helpers;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Telegram;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Zones;

using System.Text.RegularExpressions;
using System.Timers;

namespace CryptoScanner.Core.Services;


public class ScannerSession : IScannerSession
{
    private bool IsStarted { get; set; } = false;
    private bool IsStartedBefore { get; set; } = false;
    private bool IsStopInProgress { get; set; } = false;

    // Timer voor het verversen van de exchange symbols (en bijbehorende volume enzovoort)
    private readonly System.Timers.Timer TimerGetExchangeInfoAndCandles = new() { Enabled = false };
    // Iedere x uren de candles bewaren (anders veel achterstand bij crash)
    private readonly System.Timers.Timer TimerSaveCandleData = new() { Enabled = false };
    // Draaien de streams nog steeds, check + restart indien het een duwtje nodig heeft
    private readonly System.Timers.Timer TimerCheckDataStream = new() { Enabled = false };
    // Vervolg van check, herstel actie in de vorm van exchangeinfo + achterstand candles inhalen
    private readonly System.Timers.Timer TimerRestartStreams = new() { Enabled = false };

    // Heart beat to make blue tooth speakers awake
    private readonly System.Timers.Timer TimerSoundHeartBeat = new() { Enabled = false };

    // Voor het geval de user ticker het laat afwaten controleren we de posities ook 1x per uur
    private readonly System.Timers.Timer TimerCheckPositions = new() { Enabled = false };

    // The barometer is market breadth over the full symbol pool of a quote. It used to be
    // recalculated from the Avalonia dashboard viewmodel, which meant any other host (Photino,
    // Web) never got a barometer at all — and SignalExecute rejects every signal when
    // PriceBarometer has no value ("Barometer x not calculated"). Recalculating it here makes
    // it host independent. BarometerTools.ExecuteAsync() is guarded by its own Monitor.TryEnter,
    // so an overlapping call from the dashboard simply returns instead of doing double work.
    private readonly System.Timers.Timer TimerBarometer = new() { Enabled = false };

    // Periodiek de strategy performance herberekenen (adaptieve feedback)
    //private readonly System.Timers.Timer TimerCheckStrategyPerformance = new() { Enabled = false };

    // Exchange events
    private AddTextEvent ConnectionWasLostEvent { get; set; }
    private AddTextEvent ConnectionWasRestoredEvent { get; set; }


    public ScannerSession()
    {
        TimerCheckPositions.Elapsed += TimerCheckPositions_Tick;
        TimerBarometer.Elapsed += TimerBarometer_Tick;
        TimerCheckDataStream.Elapsed += TimerCheckDataStream_Tick;
        TimerRestartStreams.Elapsed += TimerRestartStreams_Tick;
        TimerSoundHeartBeat.Elapsed += TimerHeartBeath_Tick;

        TimerSaveCandleData.Elapsed += TimerSaveCandleData_Tick;
        //TimerCheckStrategyPerformance.Elapsed += TimerCheckStrategyPerformance_Tick;

        ConnectionWasLostEvent += new AddTextEvent(ConnectionWasLostEvent_Tick);
        ConnectionWasRestoredEvent += new AddTextEvent(ConnectionWasRestoredEvent_Tick);

        TimerGetExchangeInfoAndCandles.Elapsed += TimerGetExchangeInfoAndCandles_Tick;
        GlobalData.SetCandleTimerEnableEvent += new SetCandleTimerEnable(SetCandleTimerEnableHandler);
    }

    public void AfterStartup()
    {
        System.Diagnostics.Debug.WriteLine($"ScannerSession.AfterStarup");

        // Database initialization + load some basic objects
        Directory.CreateDirectory(GlobalData.AppDataFolder);
        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();

        // Load settings and combine with the application parameters (-e ExchangeName)
        GlobalData.LoadConfiguration();
        PickupExchangeFromParameter();
    }

    private static void PickupExchangeFromParameter()
    {
        // Pick the exchange from the application parameters (-e exchangename) if present
        // Initialize the exchange name, otherwise we take the exchange from the settings
        string? exchangeName = ApplicationParams.Options!.ExchangeName;
        if (exchangeName != null)
        {
            // People forget to use the right casing
            exchangeName = exchangeName.Trim().ToLower();
            string? found = GlobalData.ExchangeListName.Values.Where(x => x.Name.Equals(exchangeName, StringComparison.CurrentCultureIgnoreCase)).SingleOrDefault()?.Name;
            if (found != null)
                exchangeName = found;
            GlobalData.Settings.General.ExchangeName = exchangeName;
        }
    }

    public async Task ApplyConfigurationAsync(bool loadSymbols)
    {
        System.Diagnostics.Debug.WriteLine($"ScannerSession.ApplySettings");

        // Initialize the active exchange
        var currentExchange = GlobalData.ActiveExchange;
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? activeExchange))
            GlobalData.ActiveExchange = activeExchange;
        else
            throw new Exception($"Exchange {GlobalData.Settings.General.ExchangeName} does not exist");

        // Initialize the exchange defaults (once), or when it has changed
        if (currentExchange != GlobalData.ActiveExchange)
        {
            GlobalData.ActiveExchange!.GetApiInstance().ExchangeDefaults();
        }


        // Add a default quote if needed
        string? defaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
        // strange default //if (string.IsNullOrEmpty(defaultQuote)) //    defaultQuote = "USDT";
        if (!string.IsNullOrEmpty(defaultQuote) && !GlobalData.Settings.QuoteCoins.ContainsKey(defaultQuote))
        {
            CryptoQuoteData defaultQuoteData = GlobalData.AddQuoteData(defaultQuote);
            defaultQuoteData.FetchCandles = true;
            // The boundary comes from the exchange itself: what counts as a tradable 24 hour volume on
            // Binance leaves nothing at all on a small exchange like HyperLiquid. Only applies to a quote
            // the settings have never seen, so an existing (user adjusted) value is never overwritten.
            defaultQuoteData.MinimalVolume = ExchangeBase.ExchangeOptions.MinimalVolume;
        }
        //if (GlobalData.ActiveExchange!.SymbolListName.Count == 0)
        //    GlobalData.LoadSymbols(); // need this for the information dashboard (needs refactoring, todo)

        // ????? Do we need this, it looks like a lot of work in Avalonia...
        //if ((GlobalData.Settings.General.FontSizeNew != Font.Size) || (GlobalData.Settings.General.FontNameNew.Equals(Font.Name)))
        //{
        //    Font = new System.Drawing.Font(GlobalData.Settings.General.FontNameNew, GlobalData.Settings.General.FontSizeNew,
        //        System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        //    dashBoardControl1.Font = Font;
        //}

        PluginManager.RestoreSettings(GlobalData.Settings.Signal.AnalyzerSettings);
        GlobalData.IndexStrategySettings();
        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        SignalPrepare.Prepare();
        SignalExecute.Prepare();

        // Report enabled strategies that can never signal (plugin missing from this build, stale
        // settings entry). Silent in the healthy case.
        Signal.Indicators.StrategyDiagnostics.Report();

        SetTimerDefaults();

        // Change theme if needed
        GlobalData.SetTheme?.Invoke(GlobalData.Settings.General.Theme ?? "Default");

        SetApplicationTitle();

        if (loadSymbols)
        {
            // Positions will be loaded later
            GlobalData.LoadAssets(); // not sure if we need this (papertrading perhaps?)
            GlobalData.LoadSymbols(); // need to load these before the tickers are created
            GlobalData.SendMvvmMessage(new SymbolsHaveChangedMessage());
        }

        // Restart Telegram if token changed
        if (GlobalData.Telegram.Token != ThreadTelegramBot.Token || GlobalData.Telegram.ChatId != ThreadTelegramBot.ChatId)
            await ThreadTelegramBot.Start(GlobalData.Telegram.Token, GlobalData.Telegram.ChatId);
        //ThreadTelegramBot.ChatId = GlobalData.Telegram.ChatId;
    }


    private static void SetApplicationTitle()
    {
        string title = $"{Const.Constants.AppName} {GlobalData.AppVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();
        GlobalData.SetTitle?.Invoke(title);
    }



    public void Start(int delay)
    {
        //GlobalData.AddTextToLogTab("Debug: ScannerSession.Start");
        ScannerLog.Logger.Trace($"ScannerSession.Starting");
        if (!IsStarted)
        {
            try
            {
                GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;

                ExchangeBase.CancellationTokenSource = new();
                ExchangeBase.CancellationToken = ExchangeBase.CancellationTokenSource.Token;

                GlobalData.ThreadSaveObjects = new ThreadSaveObjects();
                GlobalData.ThreadMonitorCandle = new ThreadMonitorCandle();
                GlobalData.ThreadMonitorOrder = new ThreadMonitorOrder();
                GlobalData.ThreadCheckPosition = new ThreadCheckFinishedPosition();
                GlobalData.ThreadZoneCalculate = new ZoneThreadCalculate();

                //if (GlobalData.TradingApi.Key != "")
                //    _ = ExchangeBase.UserTicker!.StartAsync();
                // Vanuit hybernate wachten ivm netwerk verbindingen..
                if (delay > 0)
                    Thread.Sleep(delay);

                // De task start "traag" en dan heeft ie de nieuwe true te pakken
                bool checkPositions = IsStartedBefore;
                Task.Run(async () => { await ThreadLoadData.ExecuteAsync(checkPositions); });
            }
            finally
            {
                IsStarted = true;
                IsStartedBefore = true;
            }
        }
        ScannerLog.Logger.Trace($"ScannerSession.Started");
    }


    public async Task StopAsync()
    {
        ScannerLog.Logger.Trace($"ScannerSession.Stopping");
        if (IsStarted && !IsStopInProgress)
        {
            IsStopInProgress = true;
            GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;
            try
            {
                TimerCheckPositions.Enabled = false;
                TimerBarometer.Enabled = false;
                TimerCheckDataStream.Enabled = false;
                TimerRestartStreams.Enabled = false;
                TimerSoundHeartBeat.Enabled = false;
                TimerGetExchangeInfoAndCandles.Enabled = false;
                TimerSaveCandleData.Enabled = false;
                //TimerCheckStrategyPerformance.Enabled = false;

                ScannerLog.Logger.Trace($"Debug: Request for ticker cancel");
                ExchangeBase.CancellationTokenSource.Cancel();

                Task task;
                List<Task> taskList = [];

                task = Task.Run(ThreadTelegramBot.Stop);
                taskList.Add(task);

                task = Task.Run(() => { GlobalData.ThreadSaveObjects?.Stop(); });
                taskList.Add(task);

                task = Task.Run(() => { GlobalData.ThreadMonitorCandle?.Stop(); });
                taskList.Add(task);

                //GlobalData.ThreadMonitorOrder?.Stop();
                task = Task.Run(() => { GlobalData.ThreadMonitorOrder?.Stop(); });
                taskList.Add(task);

                //GlobalData.ThreadDoubleCheckPosition?.Stop();
                task = Task.Run(() => { GlobalData.ThreadCheckPosition?.Stop(); });
                taskList.Add(task);

                task = Task.Run(() => { GlobalData.ThreadZoneCalculate?.Stop(); });
                taskList.Add(task);

                //if (ExchangeBase.UserTicker != null && !GlobalData.ApplicationIsClosing)
                //{
                //    task = Task.Run(async () => { await ExchangeBase.UserTicker.StopAsync(); });
                //    taskList.Add(task);
                //}

                if (ExchangeBase.KLineTicker != null && !GlobalData.ApplicationIsClosing)
                {
                    //await ExchangeHelper.KLineTicker?.StopAsync();
                    task = Task.Run(async () => { await ExchangeBase.KLineTicker.StopAsync(); });
                    taskList.Add(task);
                }

                //if (ExchangeBase.PriceTicker != null && !GlobalData.ApplicationIsClosing)
                //{
                //    //await ExchangeHelper.PriceTicker?.Stop();
                //    task = Task.Run(() => { ExchangeBase.PriceTicker?.StopAsync(); });
                //    taskList.Add(task);
                //}

                //task = Task.Run(DataStore.SaveCandlesAsync);
                task = Task.Run(CandleDatabase.SaveCandlesAsync);
                taskList.Add(task);

                await Task.WhenAll(taskList).ConfigureAwait(false);

                // On application close the timers are no longer needed; disposing them releases
                // the threadpool callbacks that would otherwise keep the process alive
                if (GlobalData.ApplicationIsClosing)
                {
                    TimerCheckPositions.Dispose();
                    TimerBarometer.Dispose();
                    TimerCheckDataStream.Dispose();
                    TimerRestartStreams.Dispose();
                    TimerSoundHeartBeat.Dispose();
                    TimerGetExchangeInfoAndCandles.Dispose();
                    TimerSaveCandleData.Dispose();
                }
            }
            finally
            {
                IsStopInProgress = false;
                IsStarted = false;
            }
        }
        ScannerLog.Logger.Trace($"ScannerSession.Stopped");
    }


    private async void TimerSaveCandleData_Tick(object? sender, EventArgs? e)
    {
        // Save the candles each x hours..
        //await DataStore.SaveCandlesAsync();
        await CandleDatabase.SaveCandlesAsync();

        // Hourly cleanup of the experimental SQLite candle store. Independent of the file
        // save above — even if the .compressed save fails this still runs so the DB does
        // not grow without bound. CandleDatabase has its own try/catch per symbol.
        await CandleDatabase.CleanCandlesAsync();

        // After the DB cleanup, sweep the exchange/quote folders for leftover .compressed
        // files of symbols that are dormant, below volume threshold, or no longer listed.
        await DataStore.CleanOrphanCandleFilesAsync();
    }

    public void SetTimerDefaults()
    {
        TimerCheckDataStream.InitTimerInterval(5 * 60); // 5 minutes

        // Restart data stream's every day
        TimerRestartStreams.InitTimerInterval(24 * 60 * 60); // 24 hours

        // Bewaar de candle data iedere x uur
        TimerSaveCandleData.InitTimerInterval(1 * 60 * 60); // 1 hour

        // Controleer de posities (fix probleem user ticker)
        TimerCheckPositions.InitTimerInterval(1 * 60 * 60); // 1 hours

        // The barometer produces one value per minute, so recalculating every 30 seconds keeps
        // it at most one candle behind without doing meaningful extra work (the internal
        // bookkeeping only iterates the candles that were not calculated yet).
        TimerBarometer.InitTimerInterval(30);

        // Interval voor het ophalen van de exchange info (delisted coins) + bijwerken candles
        TimerGetExchangeInfoAndCandles.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);

        TimerSoundHeartBeat.InitTimerInterval(GlobalData.Settings.General.SoundHeartBeatMinutes * 60);

        // Herbereken strategy performance elke 15 minuten
        //TimerCheckStrategyPerformance.InitTimerInterval(15 * 60);
    }


    public void ScheduleRefresh()
    {
        TimerRestartStreams.InitTimerInterval(1 * 5);
    }


    private async void TimerHeartBeath_Tick(object? sender, EventArgs? e)
    {
        GlobalData.PlaySomeMusic(GlobalData.Settings.General.SoundHeartBeat);
    }


    private void TimerBarometer_Tick(object? sender, EventArgs? e)
    {
        // The emulator replays a handful of symbols; a barometer over that subset is meaningless
        // (BarometerHelper treats a missing barometer as neutral in emulator mode).
        if (GlobalData.IsEmulatorMode || GlobalData.ApplicationIsClosing)
            return;
        if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running)
            return;

        Task.Run(() =>
        {
            try
            {
                BarometerTools barometerTools = new();
                barometerTools.ExecuteAsync();
                GlobalData.SendMvvmMessage(new BarometerRefreshMessage());
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "TimerBarometer");
            }
        });
    }


    private async void TimerRestartStreams_Tick(object? sender, EventArgs? e)
    {
        GlobalData.AddTextToLogTab("ScannerSession.Restart");
        GlobalData.AddTextToTelegram("ScannerSession.Restart");

        TimerRestartStreams.Enabled = false;
        TimerCheckDataStream.Enabled = false;
        //GlobalData.ApplicationStatus = CryptoApplicationStatus.AppStatusExiting;
        //GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;
        //try
        //{
        //CloseScannerSession().Wait();
        await Task.Run(StopAsync).ConfigureAwait(false);   //.Wait();
        Start(5000);
        //}
        //finally
        //{
        //lastCandlesKLineCount = 0;
        //TimerCheckDataStream.InitTimerInterval(5 * 60); // reset interval (back to 5m)
        //TimerRestartStreams.InitTimerInterval(4 * 60 * 60); // reset interval (back to 4h)
        //}
    }


    private async void TimerCheckPositions_Tick(object? sender, EventArgs? e)
    {
        if (TimerCheckPositions.Enabled)
        {
            await TradeTools.CheckOpenPositions();
        }

        // Daarnaast gaarne een controle op de user ticker en een herstart van de user ticker indien deze oproblemen heeft gehad
        //if (ExchangeHelper.UserData.NeedsRestart())
        //{
        //    //?
        //}
    }

    // Number of consecutive checks that found a stalled ticker. Restarting the whole scanner session is
    // a heavy hammer for a single silent subscription, so the first attempt only restarts the affected
    // subscriptions and the session restart is kept as the fallback.
    private int _dataStreamProblemCount = 0;

    private void TimerCheckDataStream_Tick(object? sender, EventArgs? e)
    {
        if (ExchangeBase.KLineTicker != null)
        {
            if (ExchangeBase.KLineTicker.NeedsRestart())
            {
                _dataStreamProblemCount++;
                GlobalData.AddTextToLogTab($"One of {ExchangeBase.KLineTicker.TickerType} tickers has stopped (check {_dataStreamProblemCount})");

                // First try to restart only the subscriptions that reported a problem
                if (_dataStreamProblemCount < 2)
                {
                    Task.Run(async () => await ExchangeBase.KLineTicker.CheckSubscriptions());
                    return;
                }

                // That did not help, schedule a restart of the streams in 1m max
                if (!TimerRestartStreams.Enabled || TimerRestartStreams.Interval > 60 * 1000)
                    TimerRestartStreams.InitTimerInterval(1 * 60);
            }
            else
                _dataStreamProblemCount = 0;
        }
    }


    public void ConnectionWasLost(string text)
    {
        ConnectionWasLostEvent?.Invoke(text);
    }

    private void ConnectionWasLostEvent_Tick(string text)
    {
        // Plan alvast een verversing omdat er een connection timeout was.
        // Dit kan een aantal berekeningen onderbroken hebben
        // (er komen een aantal reconnects, daarom circa 120 seconden)
        if (!TimerGetExchangeInfoAndCandles.Enabled) // anders krijg je 100 van die dingen achter elkaar
            TimerGetExchangeInfoAndCandles.InitTimerInterval(2 * 60);
    }


    public void ConnectionWasRestored(string text)
    {
        ConnectionWasRestoredEvent?.Invoke(text);
    }

    private void ConnectionWasRestoredEvent_Tick(string text)
    {
        // Pas de geplande verversing omdat er een connection timeout was.
        // Dit kan een aantal berekeningen onderbroken hebben
        // (er komen een aantal reconnects, daarom circa 30 seconden)
        //if (TimerGetExchangeInfoAndCandles.Enabled && TimerGetExchangeInfoAndCandles.Interval == 2 * 60) //?
        //    TimerGetExchangeInfoAndCandles.InitTimerInterval(30);
        //else if (!TimerGetExchangeInfoAndCandles.Enabled) // Anders krijg je diverse achter elkaar
        //    TimerGetExchangeInfoAndCandles.InitTimerInterval(30);
        TimerGetExchangeInfoAndCandles.InitTimerInterval(30);
    }

    private void SetCandleTimerEnableHandler(bool value)
    {
        if (value)
            TimerGetExchangeInfoAndCandles.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);
        else
            TimerGetExchangeInfoAndCandles.InitTimerInterval(0); // disable
    }


    // Guards against a second refresh cycle starting while the previous one is still busy. Replaces
    // the old approach of switching the timer off during the refresh, which also restarted its
    // countdown afterwards and stretched the effective period far beyond the configured interval.
    private int _getExchangeInfoAndCandlesRunning = 0;

    private void TimerGetExchangeInfoAndCandles_Tick(object? sender, EventArgs? e)
    {
        // Ophalen van candle candles bijwerken
        // Reschedule before doing any work so the next run is exactly one interval away from this one,
        // regardless of how long the refresh itself takes.
        int intervalMinutes = GlobalData.Settings.General.GetCandleInterval;
        TimerGetExchangeInfoAndCandles.InitTimerInterval(intervalMinutes * 60);
        GlobalData.AddTextToLogTab($"Next refresh of exchange info and candles at {GlobalData.Clock.UtcNow.AddMinutes(intervalMinutes).ToLocalTime():HH:mm:ss}");

        if (Interlocked.CompareExchange(ref _getExchangeInfoAndCandlesRunning, 1, 0) != 0)
        {
            GlobalData.AddTextToLogTab("Refresh of exchange info and candles is still running, skipping this cycle");
            return;
        }

        // restart tickers if errors
        Task.Run(async () =>
        {
            try
            {
                var api = GlobalData.ActiveExchange!.GetApiInstance();

                await api.Symbol.GetSymbolsAsync();

                // The volume decision for this whole cycle is taken here, right after the volumes were
                // refreshed, so the synchronisation and the candle fetch below agree on who qualifies.
                CandleBase.UpdateVolumeDecisions();

                if (ExchangeBase.KLineTicker != null)
                    await ExchangeBase.KLineTicker.CheckSubscriptions(); // herstarten van ticker indien errors
                //if (ExchangeBase.PriceTicker != null)
                //    await ExchangeBase.PriceTicker.CheckSubscriptions(); // herstarten van ticker indien errors
                //if (ExchangeBase.UserTicker != null)
                //    await ExchangeBase.UserTicker.CheckSubscriptions(); // herstarten van ticker indien errors

                // Subscribe BEFORE fetching, so the live 1m stream and the REST catch-up overlap. The
                // other way around leaves a gap for every minute boundary that passes in between, and
                // that candle only arrives an hour later with the next catch-up.
                if (ExchangeBase.KLineTicker != null)
                    await ExchangeBase.KLineTicker.SynchronizeSymbolsAsync();

                await api.Candle.GetCandlesForAllSymbolsAndIntervalsAsync();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "TimerGetExchangeInfoAndCandles");
                GlobalData.AddTextToLogTab("error refreshing exchange info and candles " + error.ToString());
            }
            finally
            {
                Interlocked.Exchange(ref _getExchangeInfoAndCandlesRunning, 0);
            }
        });
        //_ = ExchangeHelper.KLineTicker.CheckKlineTickers(); // herstarten van ticker indien errors
        //_ = ExchangeHelper.FetchCandlesAsync(); // niet wachten tot deze klaar is
    }



    static void ProcessFile(string fileName, Model.CryptoExchange exchange, CryptoQuoteData quoteData)
    {
        string extension = Path.GetExtension(fileName);
        if (extension.Equals(".compressed") || extension.Equals(".bin"))
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            var match = Regex.Match(baseName, @"^(?<name>.+)-(?<interval>\d+[a-zA-Z]+)$");
            if (match.Success)
            {
                string symbolName = match.Groups["name"].Value;
                string intervalName = match.Groups["interval"].Value;
                if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                {
                    if (exchange.SymbolListName.TryGetValue(symbolName + quoteData.Name, out CryptoSymbol? symbol))
                    {
                        if (symbol.IsBarometerSymbol())
                            return;

                        if (!symbol.QuoteData.FetchCandles || symbol.Status != 1)
                        {
                            File.Delete(fileName);
                            GlobalData.AddTextToLogTab($"{baseName}{quoteData.Name}.{extension} deleted");
                        }
                    }
                }
            }
            else if (exchange.SymbolListName.TryGetValue(baseName + quoteData.Name, out CryptoSymbol? symbol))
            {
                if (!symbol.QuoteData.FetchCandles || symbol.Status != 1)
                {
                    File.Delete(fileName);
                    GlobalData.AddTextToLogTab($"{baseName}{quoteData.Name}.{extension} deleted");
                }
            }
        }
    }

    // clean the exchange folder (this will not clear the old pivots folder!)
    private void TimerClearData_Tick(object? sender, ElapsedEventArgs e)
    {
        var exchange = GlobalData.ActiveExchange;
        if (exchange != null)
        {
            string exchangeStoragePath = Path.Combine(GlobalData.AppDataFolder, exchange.Name.ToLower());

            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                string storagePath = Path.Combine(exchangeStoragePath, quoteData.Name.ToLower());

                if (Directory.Exists(storagePath))
                {
                    string[] files = Directory.GetFiles(storagePath);
                    foreach (string file in files)
                    {
                        ProcessFile(file, exchange, quoteData);
                    }
                }
            }

        }
    }

}
