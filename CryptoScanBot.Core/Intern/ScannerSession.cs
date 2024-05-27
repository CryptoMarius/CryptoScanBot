using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

namespace CryptoScanBot.Core.Intern;


// Een betere opzet (maar het moet nog beter)


public static class ScannerSession
{
    private static bool IsStarted { get; set; } = false;
    private static bool IsStartedBefore { get; set; } = false;
    private static bool IsStopInProgress { get; set; } = false;

    // Er zit verschil in de threading aanpak tussen deze timers (wat is dat nu weer?)

    // Timertje voor het doorgeven van de signalen en de log teksten in de memo
    public static readonly System.Timers.Timer TimerAddSignal = new() { Enabled = false };
    // Timertje voor de barometer grafiek
    public static readonly System.Timers.Timer TimerShowInformation = new() { Enabled = false };
    // Timertje voor afspelen van heartbeat signaal (zodat bluetooth speaker wakker blijft)
    public static readonly System.Timers.Timer TimerSoundHeartBeat = new() { Enabled = false };
    // Iedere zoveel uren de memo clearen (anders wordt het te traag)
    public static readonly System.Timers.Timer TimerClearMemo = new() { Enabled = false };


    // Timer voor het verversen van de exchange symbols (en bijbehorende volume enzovoort)
    private static readonly System.Timers.Timer TimerGetExchangeInfoAndCandles = new() { Enabled = false };
    // Iedere x uren de candles bewaren 9anders zoveel achterstand)
    private static readonly System.Timers.Timer TimerSaveCandleData = new() { Enabled = false };
    // Draaien de streams nog steeds, check + restart indien het een duwtje nodig heeft
    private static readonly System.Timers.Timer TimerCheckDataStream = new() { Enabled = false };
    // Vervolg van check, herstel actie in de vorm van exchangeinfo + achterstand candles inhalen
    private static readonly System.Timers.Timer TimerRestartStreams = new() { Enabled = false };

#if TRADEBOT
    // Voor het geval de user ticker het laat afwaten controleren we de posities ook 1x per uur
    private static readonly System.Timers.Timer TimerCheckPositions = new() { Enabled = false };
#endif

    // Exchange events
    private static AddTextEvent ConnectionWasLostEvent { get; set; }
    private static AddTextEvent ConnectionWasRestoredEvent { get; set; }


    static ScannerSession()
    {
#if TRADEBOT
        TimerCheckPositions.Elapsed += TimerCheckPositions_Tick;
#endif
        TimerCheckDataStream.Elapsed += TimerCheckDataStream_Tick;
        TimerRestartStreams.Elapsed += TimerRestartStreams_Tick;

        TimerSaveCandleData.Elapsed += TimerSaveCandleData_Tick;

        ConnectionWasLostEvent += new AddTextEvent(ConnectionWasLostEvent_Tick);
        ConnectionWasRestoredEvent += new AddTextEvent(ConnectionWasRestoredEvent_Tick);

        TimerGetExchangeInfoAndCandles.Elapsed += TimerGetExchangeInfoAndCandles_Tick;
        GlobalData.SetCandleTimerEnableEvent += new SetCandleTimerEnable(SetCandleTimerEnableHandler);
    }


    public static void Start(int delay)
    {
        GlobalData.AddTextToLogTab("Debug: ScannerSession.Start", true);
        ScannerLog.Logger.Trace($"ScannerSession.Starting");
        if (!IsStarted)
        {
            try
            {
                GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;

                ExchangeHelper.CancellationTokenSource = new();
                ExchangeHelper.CancellationToken = ExchangeHelper.CancellationTokenSource.Token;

                GlobalData.ThreadMonitorCandle = new ThreadMonitorCandle();
#if TRADEBOT
                GlobalData.ThreadMonitorOrder = new ThreadMonitorOrder();
                GlobalData.ThreadDoubleCheckPosition = new ThreadCheckFinishedPosition();
                if (GlobalData.TradingApi.Key != "")
                    _ = ExchangeHelper.UserTicker.StartAsync();
#endif
#if BALANCING
        GlobalData.ThreadBalanceSymbols = new ThreadBalanceSymbols();
#endif
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


    public static async Task StopAsync()
    {
        ScannerLog.Logger.Trace($"ScannerSession.Stopping");
        if (IsStarted && !IsStopInProgress)
        {
            IsStopInProgress = true;
            GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;
            try
            {
#if TRADEBOT
                TimerCheckPositions.Enabled = false;
#endif
                TimerCheckDataStream.Enabled = false;
                TimerRestartStreams.Enabled = false;
                TimerSoundHeartBeat.Enabled = false;
                TimerGetExchangeInfoAndCandles.Enabled = false;
                TimerShowInformation.Enabled = false;
                TimerSaveCandleData.Enabled = false;

                ScannerLog.Logger.Trace($"Debug: Request for ticker cancel");
                ExchangeHelper.CancellationTokenSource.Cancel();

                Task task;
                List<Task> taskList = [];

                task = Task.Run(ThreadTelegramBot.Stop);
                taskList.Add(task);

                //GlobalData.ThreadMonitorCandle?.Stop();
                task = Task.Run(() => { GlobalData.ThreadMonitorCandle?.Stop(); });
                taskList.Add(task);

#if TRADEBOT
                //GlobalData.ThreadMonitorOrder?.Stop();
                task = Task.Run(() => { GlobalData.ThreadMonitorOrder?.Stop(); });
                taskList.Add(task);

                //GlobalData.ThreadDoubleCheckPosition?.Stop();
                task = Task.Run(() => { GlobalData.ThreadDoubleCheckPosition?.Stop(); });
                taskList.Add(task);

                if (ExchangeHelper.UserTicker != null && !GlobalData.ApplicationIsClosing)
                {
                    task = Task.Run(async () => { await ExchangeHelper.UserTicker?.StopAsync(); });
                    taskList.Add(task);
                }
#endif
#if BALANCING
                //GlobalData.ThreadBalanceSymbols?.Stop();
                //task = Task.Run(async() => { await GlobalData.ThreadBalanceSymbols?.Stop(); });
                //taskList.Add(task);
#endif

                if (ExchangeHelper.KLineTicker != null && !GlobalData.ApplicationIsClosing)
                {
                    //await ExchangeHelper.KLineTicker?.StopAsync();
                    task = Task.Run(() => { ExchangeHelper.KLineTicker?.StopAsync(); });
                    taskList.Add(task);
                }

                if (ExchangeHelper.PriceTicker != null && !GlobalData.ApplicationIsClosing)
                {
                    //await ExchangeHelper.PriceTicker?.Stop();
                    task = Task.Run(() => { ExchangeHelper.PriceTicker?.StopAsync(); });
                    taskList.Add(task);
                }

                task = Task.Run(DataStore.SaveCandlesAsync);
                taskList.Add(task);

                await Task.WhenAll(taskList).ConfigureAwait(false);
            }
            finally
            {
                IsStopInProgress = false;
                IsStarted = false;
            }
        }
        ScannerLog.Logger.Trace($"ScannerSession.Stopped");
    }


    private static async void TimerSaveCandleData_Tick(object sender, EventArgs e)
    {
        // Save the candles each x hours..
        await DataStore.SaveCandlesAsync();
    }



    public static void SetTimerDefaults()
    {
        TimerAddSignal.InitTimerInterval(2.5); // 2.5 seconds
        TimerShowInformation.InitTimerInterval(5); // 5 seconds

        TimerSoundHeartBeat.InitTimerInterval(60 * GlobalData.Settings.General.SoundHeartBeatMinutes); // x minutes

        // Check data stream's (om toch zeker te zijn van nieuwe candles)
        TimerRestartStreams.InitTimerInterval(0); // OFF
        TimerCheckDataStream.InitTimerInterval(5 * 60); // 5 minutes

        // Restart data stream's every day
        TimerRestartStreams.InitTimerInterval(24 * 60 * 60); // 24 hours

        // Bewaar de candle data iedere x uur
        TimerSaveCandleData.InitTimerInterval(1 * 60 * 60); // 1 hour

        // Maak de log leeg iedere 24 uur
        TimerClearMemo.InitTimerInterval(24 * 60 * 60); // 24 hours

#if TRADEBOT
        // Controleer de posities (fix probleem user ticker)
        TimerCheckPositions.InitTimerInterval(1 * 60 * 60); // 1 hours
#endif

        // Interval voor het ophalen van de exchange info (delisted coins) + bijwerken candles 
        TimerGetExchangeInfoAndCandles.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);
    }


    public static void ScheduleRefresh()
    {
        TimerRestartStreams.InitTimerInterval(1 * 5);
    }

    private static async void TimerRestartStreams_Tick(object sender, EventArgs e)
    {
        GlobalData.AddTextToLogTab("Debug: ScannerSession.Restart", true);
        GlobalData.AddTextToTelegram("Debug: ScannerSession.Restart");

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


#if TRADEBOT
    private static async void TimerCheckPositions_Tick(object sender, EventArgs e)
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
#endif

    private static void TimerCheckDataStream_Tick(object sender, EventArgs e)
    {
        if (ExchangeHelper.KLineTicker.NeedsRestart())
        {
            GlobalData.AddTextToLogTab("Debug: Een van de 1m kline tickers is gestopt!", true);

            // Schedule a rest of the streams
            if (!TimerRestartStreams.Enabled)
                TimerRestartStreams.InitTimerInterval(1 * 60);
        }
    }

    static public void ConnectionWasLost(string text)
    {
        ConnectionWasLostEvent?.Invoke(text);
    }

    static private void ConnectionWasLostEvent_Tick(string text, bool extraLineFeed = false)
    {
        // Plan alvast een verversing omdat er een connection timeout was.
        // Dit kan een aantal berekeningen onderbroken hebben
        // (er komen een aantal reconnects, daarom circa 120 seconden)
        if (!TimerGetExchangeInfoAndCandles.Enabled) // anders krijg je 100 van die dingen achter elkaar
            TimerGetExchangeInfoAndCandles.InitTimerInterval(2 * 60);
    }


    static public void ConnectionWasRestored(string text)
    {
        ConnectionWasRestoredEvent?.Invoke(text);
    }

    static private void ConnectionWasRestoredEvent_Tick(string text, bool extraLineFeed = false)
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

    static private void SetCandleTimerEnableHandler(bool value)
    {
        if (value)
            TimerGetExchangeInfoAndCandles.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);
        else
            TimerGetExchangeInfoAndCandles.InitTimerInterval(0); // disable
    }


    static private void TimerGetExchangeInfoAndCandles_Tick(object sender, EventArgs e)
    {
        // Ophalen van candle candles bijwerken
        TimerGetExchangeInfoAndCandles.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);

        // herstarten van ticker indien errors
        Task.Run(async () =>
        {
            await ExchangeHelper.GetSymbolsAsync();
            await ExchangeHelper.KLineTicker.CheckTickers(); // herstarten van ticker indien errors
            await ExchangeHelper.PriceTicker.CheckTickers(); // herstarten van ticker indien errors
#if TRADEBOT
            await ExchangeHelper.UserTicker.CheckTickers(); // herstarten van ticker indien errors
#endif
            await ExchangeHelper.GetCandlesAsync();
        });
        //_ = ExchangeHelper.KLineTicker.CheckKlineTickers(); // herstarten van ticker indien errors
        //_ = ExchangeHelper.FetchCandlesAsync(); // niet wachten tot deze klaar is
    }

    public static void InitTimerInterval(this System.Timers.Timer timer, double seconds)
    {
        int msec = (int)(seconds * 1000);

        timer.Enabled = false;
        // Pas op, een interval van 0 mag niet
        if (seconds > 0)
            timer.Interval = msec;
        timer.Enabled = msec > 0;
    }

}
