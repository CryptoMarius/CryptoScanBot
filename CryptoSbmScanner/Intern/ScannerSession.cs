using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;


// Een betere opzet (maar het moet nog beter)


public static class ScannerSession
{
    // Om te voorkomen dat we de signalen 2x inlezen
    private static bool IsStarted { get; set; } = false;

    // Er zit verschil in de threading aanpak tussen deze timers (wat is dat nu weer?)

    // Timertje voor het doorgeven van de signalen en de log teksten in de memo
    public static readonly System.Timers.Timer TimerAddSignal = new() { Enabled = false };
    // Timertje voor de barometer grafiek
    public static readonly System.Timers.Timer TimerShowInformation = new() { Enabled = false };
    // Timertje voor afspelen van heartbeat signaal (zodat bluetooth speaker wakker blijft)
    public static readonly System.Timers.Timer TimerSoundHeartBeat = new () { Enabled = false };
    // Iedere zoveel uren de memo clearen (anders wordt het geheel traag)
    public static readonly System.Timers.Timer TimerClearMemo = new() { Enabled = false };


    // Timer voor het verversen van de exchange symbols (en bijbehorende volume enzovoort)
    private static readonly System.Timers.Timer TimerGetExchangeInfo = new() { Enabled = false };
    // Iedere x uren de candles bewaren 9anders zoveel achterstand)
    private static readonly System.Timers.Timer TimerSaveCandleData = new() { Enabled = false };
    // Draaien de streams nog steeds, check + restart indien het een duwtje nodig heeft
    private static readonly System.Timers.Timer TimerCheckDataStream = new() { Enabled = false };
    // Vervolg van check, herstel actie in de vorm van exchangeinfo + achterstand candles inhalen
    private static readonly System.Timers.Timer TimerRestartStreams = new() { Enabled = false };

    

    // Exchange events
    private static AddTextEvent ConnectionWasLostEvent { get; set; }
    private static AddTextEvent ConnectionWasRestoredEvent { get; set; }


    static ScannerSession()
    {
        TimerCheckDataStream.Elapsed += TimerCheckDataStream_Tick;
        TimerRestartStreams.Elapsed += TimerRestartStreams_Tick;

        TimerSaveCandleData.Elapsed += TimerSaveCandleData_Tick;

        ConnectionWasLostEvent += new AddTextEvent(ConnectionWasLostEvent_Tick);
        ConnectionWasRestoredEvent += new AddTextEvent(ConnectionWasRestoredEvent_Tick);

        TimerGetExchangeInfo.Elapsed += TimerGetCandles_Tick;
        GlobalData.SetCandleTimerEnableEvent += new SetCandleTimerEnable(SetCandleTimerEnableHandler);
    }


    public static void Start(bool sleepAwhile)
    {
        GlobalData.AddTextToLogTab("Debug: ResumeScannerSession");
        GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;

        GlobalData.ThreadMonitorCandle = new ThreadMonitorCandle();
#if TRADEBOT
        GlobalData.ThreadMonitorOrder = new ThreadMonitorOrder();
        if (GlobalData.Settings.ApiKey != "")
            _ = ExchangeHelper.UserData.Start();
#endif
#if BALANCING
        GlobalData.ThreadBalanceSymbols = new ThreadBalanceSymbols();
#endif
#if SQLDATABASE
        GlobalData.TaskSaveCandles = new ThreadSaveCandles();
#endif

        // Iets met netwerk verbindingen wat nog niet "up" is?
        if (sleepAwhile)
            Thread.Sleep(5000);

        bool IsStartedCopy = IsStarted;
        Task.Run(async () => { await ThreadLoadData.ExecuteAsync(IsStartedCopy); });
        IsStarted = true;
    }


    public static async Task Stop()
    {
        //Task.Run(async () => { await ScannerSession.Stop(); }).Wait();
        GlobalData.AddTextToLogTab("Debug: Stop ScannerSession");
        //GlobalData.ApplicationStatus = CryptoApplicationStatus.AppStatusExiting;
        GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;

        // pfft, kan er net zo goed een array van maken
        TimerCheckDataStream.Enabled = false;
        TimerRestartStreams.Enabled = false;
        TimerSoundHeartBeat.Enabled = false;
        TimerGetExchangeInfo.Enabled = false;
        TimerShowInformation.Enabled = false;
        ThreadTelegramBot.Stop();


#if TRADEBOT
        GlobalData.ThreadMonitorOrder?.Stop();
        if (ExchangeHelper.UserData != null)
            await ExchangeHelper.UserData?.Stop();
#endif
#if BALANCING
        //GlobalData.ThreadBalanceSymbols?.Stop();
#endif

        // streams Threads (of tasks)
        GlobalData.ThreadMonitorCandle?.Stop();
        if (ExchangeHelper.PriceTicker != null)
            await ExchangeHelper.PriceTicker?.Stop();
        if (ExchangeHelper.KLineTicker != null)
            await ExchangeHelper.KLineTicker?.Stop();
#if SQLDATABASE
        GlobalData.TaskSaveCandles.Stop();

        // En vervolgens alsnog de niet werkte candles bewaren (een nadeel van de lazy write)
        while (true)
        {
            List<CryptoCandle> list = GlobalData.TaskSaveCandles.GetSomeFromQueue();
            if (list.Any())
                GlobalData.TaskSaveCandles.SaveQueuedCandles(list);
            else
                break;
        }
#else
        DataStore.SaveCandles();
#endif


        // Vanwege coordinaten formulier
        GlobalData.SaveSettings();
    }


    private static void TimerSaveCandleData_Tick(object sender, EventArgs e)
    {
#if !SQLDATABASE
        // Elke x uur wordt de candle data bewaard
        DataStore.SaveCandles();
#endif
    }



    public static void SetTimerDefaults()
    {
        TimerAddSignal.InitTimerInterval(1.25); // 1.25 seconde
        TimerShowInformation.InitTimerInterval(5); // 5 seconds

        TimerSoundHeartBeat.InitTimerInterval(60 * GlobalData.Settings.General.SoundHeartBeatMinutes); // x minutes

        // Check data stream's (om toch zeker te zijn van nieuwe candles)
        TimerRestartStreams.InitTimerInterval(0); // UIT!
        TimerCheckDataStream.InitTimerInterval(5 * 60); // 5 minutes

        // Restart data stream's every day
        TimerRestartStreams.InitTimerInterval(24 * 60 * 60); // 24 hours

        // Bewaar de candle data iedere
        TimerSaveCandleData.InitTimerInterval(4 * 60 * 60); // 4 hours

        // Maak de log leeg iedere 24 uur
        TimerClearMemo.InitTimerInterval(24 * 60 * 60); // 24 hours

        // Interval voor het ophalen van de exchange info (delisted coins) + bijwerken candles 
        TimerGetExchangeInfo.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);
    }


    public static void ScheduleRefresh()
    {
        TimerRestartStreams.InitTimerInterval(1 * 5);
    }

    private static void TimerRestartStreams_Tick(object sender, EventArgs e)
    {
        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Restart data streams", true);

        TimerRestartStreams.Enabled = false;
        TimerCheckDataStream.Enabled = false;
        //GlobalData.ApplicationStatus = CryptoApplicationStatus.AppStatusExiting;
        //GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;
        //try
        //{
        //CloseScannerSession().Wait();
        Task.Run(async () => { await Stop(); }).Wait();
        Start(true);
        //}
        //finally
        //{
            //lastCandlesKLineCount = 0;
            //TimerCheckDataStream.InitTimerInterval(5 * 60); // reset interval (back to 5m)
            //TimerRestartStreams.InitTimerInterval(4 * 60 * 60); // reset interval (back to 4h)
        //}
    }

    static int lastCandlesKLineCount = 0;

    private static void TimerCheckDataStream_Tick(object sender, EventArgs e)
    {
        int tickerCount = ExchangeHelper.KLineTicker.Count();
        if (lastCandlesKLineCount != 0 && tickerCount == lastCandlesKLineCount)
        {
            GlobalData.AddTextToLogTab("Debug: De 1m data stream is gestopt!");

            // Schedule a rest of the streams
            TimerRestartStreams.InitTimerInterval(1 * 60);
        }
        lastCandlesKLineCount = tickerCount;
    }

    static public void ConnectionWasLost(string text)
    {
        ConnectionWasLostEvent?.Invoke(text);
    }

    static private void ConnectionWasLostEvent_Tick(string text, bool extraLineFeed = false)
    {
        // Onderdruk alle foutmeldingen totdat het hersteld is
        //if (components != null && IsHandleCreated)
        {
            GlobalData.AddTextToLogTab("Debug: ConnectionWasLostEvent!");
            // anders krijgen we alleen maar fouten dat er geen candles zijn
            GlobalData.ApplicationStatus = CryptoApplicationStatus.Initializing;
        }
    }


    static public void ConnectionWasRestored(string text)
    {
        ConnectionWasRestoredEvent?.Invoke(text);
    }

    static private void ConnectionWasRestoredEvent_Tick(string text, bool extraLineFeed = false)
    {
        // Plan een verversing omdat er een connection timeout was.
        // Dit kan een aantal berekeningen onderbroken hebben
        // (er komen een aantal reconnects, daarom circa 20 seconden)
        //if (components != null && IsHandleCreated)
        {
            GlobalData.AddTextToLogTab("Debug: ConnectionWasRestoredEvent!");
            GlobalData.ApplicationStatus = CryptoApplicationStatus.Running;
            //Invoke((MethodInvoker)(() => InitTimerInterval(ref TimerGetExchangeInfo, 20)));
            TimerGetExchangeInfo.InitTimerInterval(20);
        }
    }

    static private void SetCandleTimerEnableHandler(bool value)
    {
        //if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
        {
            //if (components != null && IsHandleCreated)
            {
                if (value)
                    //Invoke((MethodInvoker)(() => InitTimerInterval(ref TimerGetExchangeInfo, GlobalData.Settings.General.GetCandleInterval * 60)));
                    TimerGetExchangeInfo.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);
                else
                    //Invoke((MethodInvoker)(() => InitTimerInterval(ref TimerGetExchangeInfo, 0))); // disable
                    TimerGetExchangeInfo.InitTimerInterval(0); // disable
            }
        }
    }


    static private void TimerGetCandles_Tick(object sender, EventArgs e)
    {
        // Ophalen van exchange info en candles bijwerken
        //if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
        {
            // De reguliere verversing herstellen (igv een connection timeout)
            //if (components != null && IsHandleCreated)
            {
                // Plan een volgende verversing omdat er bv een connection timeout was.
                // Dit kan een aantal berekeningen onderbroken hebben
                //Invoke((MethodInvoker)(() => InitTimerInterval(ref TimerGetExchangeInfo, GlobalData.Settings.General.GetCandleInterval * 60)));
            }
            TimerGetExchangeInfo.InitTimerInterval(GlobalData.Settings.General.GetCandleInterval * 60);
            _ = ExchangeHelper.FetchCandlesAsync(); // niet wachten tot deze klaar is
        }
    }


}
