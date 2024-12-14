using CryptoScanBot.Commands;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Emulator;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Telegram;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Intern;
using CryptoScanBot.ZoneVisualisation;

using Microsoft.Win32;

using Nito.AsyncEx;

using System.Text;
using System.Text.Json;
using CryptoScanBot.Core.Json;

namespace CryptoScanBot;

public partial class FrmMain : Form
{
    private readonly ColorSchemeTest theme = new();
    //private ContextMenuStrip MenuTest = new();

    public class ColorSchemeTest
    {
        public Color Background { get; set; } = Color.Black;
        public Color Foreground { get; set; } = Color.White;
    }

    private readonly List<CryptoSymbol> SymbolListView = [];
    private readonly CryptoDataGridSymbol<CryptoSymbol> GridSymbolView;

    private readonly List<CryptoSignal> SignalListView = [];
    private readonly CryptoDataGridSignal<CryptoSignal> GridSignalView;

    private readonly ToolStripMenuItemCommand ApplicationPlaySounds;
    private readonly ToolStripMenuItemCommand ApplicationCreateSignals;
    private readonly ToolStripMenuItemCommand ApplicationTradingBot;
    private readonly ToolStripMenuItemCommand ApplicationBackTestMode;
    private readonly ToolStripMenuItemCommand ApplicationBackTestExec;

    private readonly List<CryptoPosition> PositionOpenListView = [];
    private readonly CryptoDataGridPositionsOpen<CryptoPosition> GridPositionOpenView;

    private readonly List<CryptoPosition> PositionClosedListView = [];
    private readonly CryptoDataGridPositionsClosed<CryptoPosition> GridPositionClosedView;

    public FrmMain()
    {
        InitializeComponent();
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        logQueue.EnsureCapacity(1500);

        ApplicationPlaySounds = MenuMain.AddCommand(null, "Play sounds", Command.None, ApplicationPlaySounds_Click);
        ApplicationPlaySounds.Checked = true;
        ApplicationCreateSignals = MenuMain.AddCommand(null, "Create signals", Command.None, ApplicationCreateSignals_Click);
        ApplicationCreateSignals.Checked = true;
        ApplicationTradingBot = MenuMain.AddCommand(null, "Trading bot active", Command.None, ApplicationTradingBot_Click);
        ApplicationTradingBot.Checked = true;

        MenuMain.AddCommand(null, "Settings", Command.None, ToolStripMenuItemSettings_Click);
        MenuMain.AddCommand(null, "Refresh information", Command.None, ToolStripMenuItemRefresh_Click_1);
        MenuMain.AddCommand(null, "Clear log en ticker count", Command.None, MainMenuClearAll_Click);
        MenuMain.AddCommand(null, "Tradingview import files", Command.TradingViewImportList);
        MenuMain.AddSeperator();
        MenuMain.AddCommand(null, "Export all exchange information to Excel", Command.ExcelExchangeInformation);
        MenuMain.AddCommand(null, "Export all signal information to Excel", Command.ExcelSignalsInformation);
        MenuMain.AddCommand(null, "Export all position information to Excel", Command.ExcelPositionsInformation);

        MenuMain.AddSeperator();
        ApplicationBackTestMode = MenuMain.AddCommand(null, "Backtest mode", Command.None, ApplicationBackTestMode_Click);
        ApplicationBackTestExec = MenuMain.AddCommand(null, "Backtest exec", Command.None, BacktestToolStripMenuItem_Click);

#if DEBUG
        MenuMain.AddSeperator();
        MenuMain.AddCommand(null, "Test - Scanner restart", Command.ScannerSessionDebug);
        MenuMain.AddCommand(null, "Test - Save Candles", Command.None, TestSaveCandlesClick);
        MenuMain.AddCommand(null, "Test - Create url testfile", Command.None, TestCreateUrlTestFileClick);
        MenuMain.AddCommand(null, "Test - Dump ticker information", Command.None, TestShowTickerInformationClick);
        MenuMain.AddCommand(null, "Test - Calculate all liquidity zones (slow!)", Command.CalculateAllLiquidityZones);
#endif
        MenuMain.AddSeperator();
        MenuMain.AddCommand(null, "About", Command.About);

        // De events pas op het laatst zetten
        SystemEvents.PowerModeChanged += OnPowerChange;
        FormClosing += FrmMain_FormClosing;
        Load += FrmMain_Load;
        Move += FrmMain_Resize;
        Resize += FrmMain_Resize;
        Shown += FrmMain_Shown;

        // Om vanuit achtergrond threads iets te kunnen loggen of te doen
        GlobalData.PlaySound += new PlayMediaEvent(PlaySound);
        GlobalData.PlaySpeech += new PlayMediaEvent(PlaySpeech);
        GlobalData.LogToTelegram += new AddTextEvent(AddTextToTelegram);
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);
        GlobalData.TelegramHasChangedEvent += new AddTextEvent(TelegramHasChangedEvent);

        // Niet echt een text event, meer misbruik van het event type
        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        GlobalData.AssetsHaveChangedEvent += new AddTextEvent(AssetsHaveChangedEvent);
        GlobalData.PositionsHaveChangedEvent += new AddTextEvent(PositionsHaveChangedEvent);
        GlobalData.StatusesHaveChangedEvent += new AddTextEvent(StatusesHaveChangedEvent);

        GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;
        GlobalData.ApplicationHasStarted += new AddTextEvent(ApplicationHasStarted);

        // Events inregelen
        ScannerSession.TimerClearMemo.Elapsed += TimerClearMemo_Tick;
        ScannerSession.TimerAddSignal.Elapsed += TimerAddSignalsAndLog_Tick;
        ScannerSession.TimerSoundHeartBeat.Elapsed += TimerSoundHeartBeat_Tick;
        ScannerSession.TimerShowInformation.Elapsed += dashBoardInformation1.TimerShowInformation_Tick;


        // Instelling laden
        GlobalData.LoadSettings();

        GridSymbolView = new() { Grid = dataGridViewSymbols, List = SymbolListView, ColumnList = GlobalData.SettingsUser.GridColumnsSymbol };
        GridSymbolView.InitGrid();

        GridSignalView = new() { Grid = dataGridViewSignals, List = SignalListView, ColumnList = GlobalData.SettingsUser.GridColumnsSignal };
        GridSignalView.InitGrid();

        GridPositionOpenView = new() { Grid = dataGridViewPositionOpen, List = PositionOpenListView, ColumnList = GlobalData.SettingsUser.GridColumnsPositionsOpen };
        GridPositionOpenView.InitGrid();

        GridPositionClosedView = new() { Grid = dataGridViewPositionClosed, List = PositionClosedListView, ColumnList = GlobalData.SettingsUser.GridColumnsPositionsClosed };
        GridPositionClosedView.InitGrid();

        // Dummy browser verbergen, is een browser om het extra confirmatie dialoog in externe browser te vermijden
        LinkTools.TabControl = tabControl;
        LinkTools.TabPageBrowser = tabPageBrowser;
        LinkTools.WebViewDummy = webViewDummy;
        LinkTools.WebViewTradingView = webViewTradingView;
        tabControl.TabPages.Remove(tabPagewebViewDummy);

        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();

        // Is er via de command line aangegeven dat we default een andere exchange willen?
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();
    }


#if DEBUG
    private async void TestSaveCandlesClick(object? sender, EventArgs? e)
    {
        await DataStore.SaveCandlesAsync();
    }


    private void TestShowTickerInformationClick(object? sender, EventArgs? e)
    {
        ExchangeBase.KLineTicker?.DumpTickerInfo();
        ExchangeBase.PriceTicker?.DumpTickerInfo();
        ExchangeBase.UserTicker?.DumpTickerInfo();
    }
#endif

    private void FrmMain_FormClosing(object? sender, FormClosingEventArgs? e)
    {
        GlobalData.ApplicationIsClosing = true;
        AsyncContext.Run(ScannerSession.StopAsync);
    }

    private void FrmMain_Shown(object? sender, EventArgs? e)
    {
        // This does not work in the Load, so moved to the Show..
        if (GlobalData.SettingsUser.MainForm.SplitterDistance > 0)
            splitContainer1.SplitterDistance = GlobalData.SettingsUser.MainForm.SplitterDistance;
        GlobalData.ApplicationIsShowed = true;
        // This event also reacts on the form resize, so manually..
        splitContainer1.SplitterMoved += SplitContainerSplitterMoved;
    }


    /// <summary>
    /// Save Window coordinates and screen
    /// </summary>
    private void FrmMain_Resize(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing || !GlobalData.ApplicationIsShowed)
            return;

        ApplicationTools.SaveWindowLocation(this, GlobalData.SettingsUser.MainForm);
        GlobalData.SaveUserSettings();
    }


    private void FrmMain_Load(object? sender, EventArgs? e)
    {
        ApplicationTools.WindowLocationRestore(this, GlobalData.SettingsUser.MainForm);

        GlobalData.Settings.General.Exchange!.GetApiInstance().ExchangeDefaults();
        GlobalData.LoadAccounts();
        ApplySettings();

        GlobalData.LoadSymbols();
        GlobalData.SymbolsHaveChanged("");
        GlobalData.LoadSignals();
        TradeTools.LoadAssets();
        TradeTools.LoadOpenPositions();
        TradeTools.LoadClosedPositions();
        PositionsHaveChangedEvent("");

        ScannerSession.Start(0);
    }


    private void ApplySettings()
    {
        // Is done multiple times, but that is okay
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? exchange))
        {
            GlobalData.Settings.General.Exchange = exchange;
            GlobalData.Settings.General.ExchangeId = exchange.Id;
            GlobalData.Settings.General.ExchangeName = exchange.Name;
        }

        // Het juiste trading coount in de globale variabelen zetten
        GlobalData.SetTradingAccounts();

        // Eventueel de nieuwe quotes zetten enz.
        dashBoardInformation1.InitializeBarometer();

        if ((GlobalData.Settings.General.FontSizeNew != Font.Size) || (GlobalData.Settings.General.FontNameNew.Equals(Font.Name)))
        {
            Font = new System.Drawing.Font(GlobalData.Settings.General.FontNameNew, GlobalData.Settings.General.FontSizeNew,
                System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            dashBoardControl1.Font = Font;
        }

        GridSymbolView.InitCommandCaptions();
        GridSignalView.InitCommandCaptions();
        GridPositionOpenView.InitCommandCaptions();
        GridPositionClosedView.InitCommandCaptions();


        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        // De timertjes goed zetten
        ScannerSession.SetTimerDefaults();

        // Theming
        if (GlobalData.Settings.General.BlackTheming)
        {
            theme.Background = Color.LightGray;
            theme.Foreground = Color.Black;
        }
        else
        {
            theme.Background = Color.White;
            theme.Foreground = Color.Black;
        }
        ChangeTheme(theme, this);

        ApplicationTradingBot.Checked = GlobalData.Settings.Trading.Active;
        ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive;
        ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.Active;

        splitContainer1.Panel1Collapsed = GlobalData.Settings.General.HideSymbolsOnTheLeft;

        GlobalData.StatusesHaveChangedEvent?.Invoke("");
        SetApplicationTitle();

        Refresh(); // Redraw
    }


    private void SetApplicationTitle()
    {
        string text = $"{GlobalData.AppName} {GlobalData.AppVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();
        if (GlobalData.BackTest)
            text += " (backtest mode)";
        // Adjust the application title
        Text = text;
    }

    private void OnPowerChange(object s, PowerModeChangedEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerModes.Resume:
                GlobalData.AddTextToLogTab("PowerMode - Resume");
                ScannerSession.Start(5000);
                break;
            case PowerModes.Suspend:
                GlobalData.AddTextToLogTab("PowerMode - Suspend");
                AsyncContext.Run(ScannerSession.StopAsync);
                break;
        }
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            //AsyncContext.Run(ScannerSession.StopAsync);

            if (components != null)
            {
                LinkTools.WebViewDummy?.Dispose();
                components.Dispose();
            }
        }

        base.Dispose(disposing);
    }


    private void PlaySound(string text, bool test = false)
    {
        if (IsHandleCreated)
        {
            if (GlobalData.Settings.Signal.SoundsActive)
                ThreadSoundPlayer.AddToQueue(text, test);
        }
    }

    private void PlaySpeech(string text, bool test = false)
    {
        if (IsHandleCreated)
        {
            if (GlobalData.Settings.Signal.SoundsActive || test)
                ThreadSpeechPlayer.AddToQueue(text);
        }
    }

    private void AddTextToTelegram(string text)
    {
        if (IsHandleCreated)
        {
            // Het ding crasht wel eens (meestal netwerk of timing problemen)
            ThreadTelegramBot.SendMessage(text);
        }
    }

    private void AddTextToLogTab(string text)
    {
        // Via queue want afzonderlijk regels toevoegen kost relatief veel tijd
        ScannerLog.Logger.Info(text);
        text = text.Trim();

        if (text != "")
        {
            if (GlobalData.BackTest)
                text = GlobalData.BackTestDateTime.ToLocalTime() + " " + text;
            else
                text = DateTime.Now.ToLocalTime() + " " + text;
        }
        logQueue.Enqueue(text);
    }

    private void TelegramHasChangedEvent(string text)
    {
        Invoke((System.Windows.Forms.MethodInvoker)(() => ApplicationTradingBot.Checked = GlobalData.Settings.Trading.Active));
        Invoke((System.Windows.Forms.MethodInvoker)(() => ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive));
        Invoke((System.Windows.Forms.MethodInvoker)(() => ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.Active));
    }


    /// <summary>
    /// Dan is er in de achtergrond een verversing actie geweest, display bijwerken!
    /// </summary>
    private void AssetsHaveChangedEvent(string text)
    {
        // TODO: Activeren!
        //if (components != null && IsHandleCreated)
        //{
        //decimal valueBtc, valueUsdt;
        //StringBuilder stringBuilder = new StringBuilder();
        //Helper.ShowAssets(stringBuilder, out valueUsdt, out valueBtc);

        //// De totaal waarde van de assets tonen
        //if (InvokeRequired)
        //    Invoke((MethodInvoker)(() => labelAssetUSDT.Text = "$" + valueUsdt.ToString0("N2")));
        //else
        //    labelAssetUSDT.Text = "$" + valueUsdt.ToString0("N2");

        //if (InvokeRequired)
        //    Invoke((MethodInvoker)(() => labelAssetBTC.Text = "₿" + valueBtc.ToString0()));
        //else
        //    labelAssetBTC.Text = "₿" + valueBtc.ToString0();


        //if (text == "")
        //    return;
        //GlobalData.AddTextToLogTab(stringBuilder.ToString());
        //}
    }

    private void ToolStripMenuItemRefresh_Click_1(object? sender, EventArgs? e)
    {
        Task.Run(async () =>
        {
            var api = GlobalData.Settings.General.Exchange!.GetApiInstance();
            await api.Symbol.GetSymbolsAsync(); // niet wachten tot deze klaar is
            if (ExchangeBase.KLineTicker != null)
                await ExchangeBase.KLineTicker!.CheckTickers(); // herstarten van ticker indien errors
            if (ExchangeBase.PriceTicker != null)
                await ExchangeBase.PriceTicker!.CheckTickers(); // herstarten van ticker indien errors
            if (ExchangeBase.UserTicker != null)
                await ExchangeBase.UserTicker!.CheckTickers(); // herstarten van ticker indien errors
            await api.Candle.GetCandlesForAllSymbolsAndIntervalsAsync(); // niet wachten tot deze klaar is
        });
    }


    private static void GetReloadRelatedSettings(out string activeQuoteData)
    {
        activeQuoteData = "";
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                activeQuoteData += "," + quoteData.Name;
        }
    }


    private async void ToolStripMenuItemSettings_Click(object? sender, EventArgs? e)
    {
        SettingsBasic oldSettings = GlobalData.Settings;

        GetReloadRelatedSettings(out string activeQuotes);
        Core.Model.CryptoExchange? oldExchange = GlobalData.Settings.General.Exchange;

        // Dan wordt de basecoin en coordinaten etc. bewaard voor een volgende keer
        GlobalData.Settings.Trading.Active = ApplicationTradingBot.Checked;
        GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
        GlobalData.Settings.Signal.Active = ApplicationCreateSignals.Checked;
        dashBoardInformation1.PickupBarometerProperties();
        try
        {
            FrmSettings dialog = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            dialog.InitSettings(GlobalData.Settings);
            ChangeTheme(theme, dialog);

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            GlobalData.SaveSettings();
            GlobalData.SaveUserSettings(); // custom colors
            GetReloadRelatedSettings(out string activeQuotes2);

            // Detectie of we hebben gewisseld van Exchange (reload) of QuoteData (reload)
            bool reloadQuoteChange = activeQuotes != activeQuotes2;
            bool reloadExchangeChange = dialog.NewExchange?.Id != GlobalData.Settings.General.ExchangeId;
            if (reloadQuoteChange || reloadExchangeChange)
            {
                GlobalData.AddTextToLogTab("");
                if (reloadExchangeChange)
                    GlobalData.AddTextToLogTab("De exchange is aangepast (reload)!");
                else if (reloadQuoteChange)
                    GlobalData.AddTextToLogTab("De lijst met quote's is aangepast (reload)!");
                AsyncContext.Run(ScannerSession.StopAsync);

                GlobalData.Settings.General.Exchange = dialog.NewExchange;
                GlobalData.Settings.General.ExchangeId = dialog.NewExchange!.Id;
                GlobalData.Settings.General.ExchangeName = dialog.NewExchange.Name;
                GlobalData.SaveSettings();

                // Standaard timers e.d.
                ApplySettings();

                if (reloadExchangeChange)
                {
                    // Exchange: Symbols clearen
                    oldExchange?.Clear();

                    // TradingAccount: Posities en Assets clearen!
                    foreach (CryptoAccount ta in GlobalData.TradeAccountList.Values)
                        ta.Data.Clear();
                }

                // Clear candle data
                if (reloadQuoteChange || reloadExchangeChange)
                {
                    foreach (var s in GlobalData.Settings.General.Exchange!.SymbolListId.Values)
                    {
                        if ((!s.QuoteData.FetchCandles || s.Status == 0) && s.CandleList.Count != 0)
                        {
                            foreach (var x in s.IntervalPeriodList)
                            {
                                if (x.CandleList.Count != 0)
                                {
                                    x.CandleList.Clear();
                                    GlobalData.AddTextToLogTab($"Cleared candles for {s.Name} {x.Interval.Name}");
                                }
                            }
                        }
                    }
                }

                // Bij wijzigingen aantal signalen)
                //signal.CloseTime = signal.CloseTime.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * Interval.Duration);

                // Optioneel een herstart van de Telegram bot
                if (GlobalData.Telegram.Token != ThreadTelegramBot.Token)
                    await ThreadTelegramBot.Start(GlobalData.Telegram.Token, GlobalData.Telegram.ChatId);
                ThreadTelegramBot.ChatId = GlobalData.Telegram.ChatId;

                GlobalData.Settings.General.Exchange!.GetApiInstance().ExchangeDefaults();
                MainMenuClearAll_Click(null, null);
                // Schedule een reload of data
                ScannerSession.ScheduleRefresh();
            }
            else ApplySettings();

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }
    }


    private void MainMenuClearAll_Click(object? sender, EventArgs? e)
    {
        TextBoxLog.Clear();
        GlobalData.CreatedSignalCount = 0;

        PositionMonitor.ResetAnalyseCount();
        ExchangeBase.KLineTicker!.Reset();
        ExchangeBase.PriceTicker!.Reset();
    }


    private static void PlaySound(CryptoSignal signal, bool playSound, bool playSpeech, string soundName, ref long lastSound)
    {
        // Reduce the amount of sounds/speech
        if (signal.EventTime > lastSound && !signal.IsInvalid)
        {
            //GlobalData.AddTextToLogTab(signal.Symbol.Name + " " + signal.StrategyText + " " + lastSound.ToString());
            if (playSound && (soundName != ""))
                GlobalData.PlaySomeMusic(soundName);

            if (playSpeech)
                GlobalData.PlaySomeSpeech("Found a signal for " + signal.Symbol.Base + "/" + signal.Symbol.Quote + " interval " + signal.Interval.Name);

            lastSound = signal.EventTime + 20; // stay silent for the next 20 seconds
        }
        //else GlobalData.AddTextToLogTab(signal.Symbol.Name + " " + signal.StrategyText + " " + lastSound.ToString() + " ignored");
    }

    private long LastSignalSoundSbmOversold = 0;
    private long LastSignalSoundSbmOverbought = 0;
    private long LastSignalSoundStobbOversold = 0;
    private long LastSignalSoundStobbOverbought = 0;
    private long LastSignalSoundCandleJumpUp = 0;
    private long LastSignalSoundCandleJumpDown = 0;
    private long LastSignalSoundStoRsiOversold = 0;
    private long LastSignalSoundStoRsiOverbought = 0;
    private long LastSignalSoundZonesOversold = 0;
    private long LastSignalSoundZonesOverbought = 0;


    private readonly Queue<string> logQueue = new();


    private void TimerAddSignalsAndLog_Tick(object? sender, EventArgs? e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Speed up adding signals
        if (GlobalData.SignalQueue.Count > 0)
        {
            if (Monitor.TryEnter(GlobalData.SignalQueue))
                try
                {
                    List<CryptoSignal> signals = [];

                    while (GlobalData.SignalQueue.Count > 0)
                    {
                        CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                        if (signal != null)
                            signals.Add(signal);
                    }

                    if (signals.Count != 0)
                    {
                        // verwerken..
                        Task.Factory.StartNew(() =>
                        {
                            Invoke(new Action(() =>
                            {
                                if (!GlobalData.ApplicationIsClosing)
                                {
                                    GridSignalView.AddObject(signals);
                                    //ListViewSignalsAddSignalRange(signals);
                                }
                            }));
                        });
                    }
                }
                finally
                {
                    Monitor.Exit(GlobalData.SignalQueue);
                }
        }


        // Speed up adding text
        if (logQueue.Count > 0)
        {
            if (Monitor.TryEnter(logQueue))
                try
                {
                    List<CryptoSignal> signals = [];
                    StringBuilder stringBuilder = new();

                    while (logQueue.Count > 0 && !GlobalData.ApplicationIsClosing)
                    {
                        string text = logQueue.Dequeue();
                        stringBuilder.AppendLine(text);
                    }

                    string allText = stringBuilder.ToString().Trim();
                    if (allText != "")
                    {
                        // verwerken..
                        Task.Factory.StartNew(() =>
                        {
                            Invoke(new Action(() =>
                            {
                                allText += "\r\n";
                                if (!GlobalData.ApplicationIsClosing)
                                {
                                    if (InvokeRequired)
                                        Invoke((System.Windows.Forms.MethodInvoker)(() =>
                                        {
                                            TextBoxLog.AppendText(allText);
                                        }));
                                    else
                                        TextBoxLog.AppendText(allText);
                                }
                            }));
                        });
                    }
                }
                finally
                {
                    Monitor.Exit(logQueue);
                }
        }
    }


    private void TimerClearMemo_Tick(object? sender, EventArgs? e)
    {
        // Elke 24 uur wordt de memo gecleared
        Invoke((System.Windows.Forms.MethodInvoker)(() => TextBoxLog.Clear()));

        // De database een beetje opruimen
        CryptoDatabase.CleanUpDatabase();
    }


    private void AnalyzeSignalCreated(CryptoSignal signal)
    {
        GlobalData.CreatedSignalCount++;
        string text = "Signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);


        // Zet de laatste munt in de "caption" (en taskbar) van de applicatie bar (visuele controle of er meldingen zijn)
        //Invoke(new Action(() => { this.Text = signal.Symbol.Name + " " + createdSignalCount.ToString(); }));

        if (!signal.IsInvalid || (signal.IsInvalid && GlobalData.Settings.General.ShowInvalidSignals))
            GlobalData.SignalQueue.Enqueue(signal);

        if (signal.BackTest)
            return;


        // Speech and/or sound
        if (!signal.IsInvalid)
        {
            switch (signal.Strategy)
            {
                case CryptoSignalStrategy.Jump:
                    if (signal.Side == CryptoTradeSide.Long)
                        PlaySound(signal, GlobalData.Settings.Signal.Jump.PlaySound, GlobalData.Settings.Signal.Jump.PlaySpeech,
                            GlobalData.Settings.Signal.Jump.SoundFileLong, ref LastSignalSoundCandleJumpUp);
                    if (signal.Side == CryptoTradeSide.Short)
                        PlaySound(signal, GlobalData.Settings.Signal.Jump.PlaySound, GlobalData.Settings.Signal.Jump.PlaySpeech,
                            GlobalData.Settings.Signal.Jump.SoundFileShort, ref LastSignalSoundCandleJumpDown);
                    break;

                case CryptoSignalStrategy.Stobb:
                    if (signal.Side == CryptoTradeSide.Long)
                        PlaySound(signal, GlobalData.Settings.Signal.Stobb.PlaySound, GlobalData.Settings.Signal.Stobb.PlaySpeech,
                            GlobalData.Settings.Signal.Stobb.SoundFileLong, ref LastSignalSoundStobbOversold);
                    if (signal.Side == CryptoTradeSide.Short)
                        PlaySound(signal, GlobalData.Settings.Signal.Stobb.PlaySound, GlobalData.Settings.Signal.Stobb.PlaySpeech,
                            GlobalData.Settings.Signal.Stobb.SoundFileShort, ref LastSignalSoundStobbOverbought);
                    break;

                case CryptoSignalStrategy.Sbm1:
                case CryptoSignalStrategy.Sbm2:
                case CryptoSignalStrategy.Sbm3:
                    if (signal.Side == CryptoTradeSide.Long)
                        PlaySound(signal, GlobalData.Settings.Signal.Sbm.PlaySound, GlobalData.Settings.Signal.Sbm.PlaySpeech,
                        GlobalData.Settings.Signal.Sbm.SoundFileLong, ref LastSignalSoundSbmOversold);
                    if (signal.Side == CryptoTradeSide.Short)
                        PlaySound(signal, GlobalData.Settings.Signal.Sbm.PlaySound, GlobalData.Settings.Signal.Sbm.PlaySpeech,
                            GlobalData.Settings.Signal.Sbm.SoundFileShort, ref LastSignalSoundSbmOverbought);
                    break;

                case CryptoSignalStrategy.StoRsi:
                    if (signal.Side == CryptoTradeSide.Long)
                        PlaySound(signal, GlobalData.Settings.Signal.StoRsi.PlaySound, GlobalData.Settings.Signal.StoRsi.PlaySpeech,
                            GlobalData.Settings.Signal.StoRsi.SoundFileLong, ref LastSignalSoundStoRsiOversold);
                    if (signal.Side == CryptoTradeSide.Short)
                        PlaySound(signal, GlobalData.Settings.Signal.StoRsi.PlaySound, GlobalData.Settings.Signal.StoRsi.PlaySpeech,
                            GlobalData.Settings.Signal.StoRsi.SoundFileShort, ref LastSignalSoundStoRsiOverbought);
                    break;

                case CryptoSignalStrategy.DominantLevel:
                    if (signal.Side == CryptoTradeSide.Long)
                        PlaySound(signal, GlobalData.Settings.Signal.Zones.PlaySound, GlobalData.Settings.Signal.Zones.PlaySpeech,
                            GlobalData.Settings.Signal.Zones.SoundFileLong, ref LastSignalSoundZonesOversold);
                    if (signal.Side == CryptoTradeSide.Short)
                        PlaySound(signal, GlobalData.Settings.Signal.Zones.PlaySound, GlobalData.Settings.Signal.Zones.PlaySpeech,
                            GlobalData.Settings.Signal.Zones.SoundFileShort, ref LastSignalSoundZonesOverbought);
                    break;
            }


            if (GlobalData.Telegram.SendSignalsToTelegram)
                ThreadTelegramBot.SendSignal(signal);
        }
    }


    private static void ChangeTheme(ColorSchemeTest scheme, Control container)
    {
        //return;
        foreach (Control component in container.Controls)
        {
            if (component is Form)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            if (component is Panel p)
            {
                if (!p.Name.Equals("PanelColor")) // Gone those specific colors ;-)
                {
                    component.BackColor = scheme.Background;
                    component.ForeColor = scheme.Foreground;
                }
            }
            else if (component is Button)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is TextBox)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is ListBox)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is ComboBox)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is Label)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is TabControl)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is TabPage)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is CheckBox)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            //else if (component is ListViewDoubleBuffered)
            //{
            //    component.BackColor = scheme.Background;
            //    component.ForeColor = scheme.Foreground;
            //}
            else if (component is ListView)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is MenuStrip)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is ContextMenuStrip)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            else if (component is DataGridView grid)
            {
                grid.BackgroundColor = scheme.Background;
                grid.ForeColor = scheme.Foreground;

                grid.GridColor = scheme.Background;
                grid.DefaultCellStyle.BackColor = scheme.Background;
                grid.DefaultCellStyle.ForeColor = scheme.Foreground;
                grid.ColumnHeadersDefaultCellStyle.BackColor = scheme.Background;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = scheme.Foreground;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = scheme.Background;
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = scheme.Foreground;

                grid.ColumnHeadersDefaultCellStyle.ForeColor = scheme.Foreground;
            }
            //else if (component is ToolStripMenuItem)
            //{
            //    component.BackColor = scheme.Background;
            //    component.ForeColor = scheme.Foreground;
            //}
            ChangeTheme(scheme, component);
        }
    }


    private void TimerSoundHeartBeat_Tick(object? sender, EventArgs? e)
      => GlobalData.PlaySomeMusic("sound-heartbeat.wav");

    private void ApplicationCreateSignals_Click(object? sender, EventArgs? e)
    {
        ApplicationCreateSignals.Checked = !ApplicationCreateSignals.Checked;
        GlobalData.Settings.Signal.Active = ApplicationCreateSignals.Checked;
        GlobalData.SaveSettings();
        GlobalData.StatusesHaveChangedEvent?.Invoke("");
    }

    private void ApplicationPlaySounds_Click(object? sender, EventArgs? e)
    {
        ApplicationPlaySounds.Checked = !ApplicationPlaySounds.Checked;
        GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
        GlobalData.SaveSettings();
        GlobalData.StatusesHaveChangedEvent?.Invoke("");
    }


    private void ApplicationTradingBot_Click(object? sender, EventArgs? e)
    {
        ApplicationTradingBot.Checked = !ApplicationTradingBot.Checked;
        GlobalData.Settings.Trading.Active = ApplicationTradingBot.Checked;
        GlobalData.SaveSettings();
        GlobalData.StatusesHaveChangedEvent?.Invoke("");
    }


    private void ApplicationHasStarted(string text)
    {
        // Show the symbols
        GlobalData.SymbolsHaveChanged("");

        // Show barometer and that it is running
        Invoke((System.Windows.Forms.MethodInvoker)(() => dashBoardInformation1.ShowBarometerStuff(null, null)));

        // Show the positions
        GlobalData.PositionsHaveChanged("");
    }


    private void StatusesHaveChangedEvent(string text)
    {
        Invoke((System.Windows.Forms.MethodInvoker)(() => { dashBoardInformation1.ShowStatusesStuff(); }));
    }

    private void SymbolsHaveChangedEvent(string text)
    {
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? exchange))
        {

            List<CryptoSymbol> range = [];

            string filter = "";
            symbolFilter.Invoke((System.Windows.Forms.MethodInvoker)(() => filter = symbolFilter.Text.ToUpper()));
            //Invoke((MethodInvoker)(() => TextBoxLog.AppendText(text)));

            // De muntparen toevoegen aan de userinterface
            foreach (var symbol in exchange.SymbolListName.Values)
            {
                if (symbol.QuoteData.FetchCandles && symbol.Status == 1)
                {
                    if (!symbol.IsSpotTradingAllowed || symbol.IsBarometerSymbol())
                        continue;

                    bool addSymbol = true;
                    if (filter != "" && !symbol.Name.Contains(filter))
                        addSymbol = false;



                    if (addSymbol)
                        range.Add(symbol);

                    //ListViewItem item = null;
                    //ListViewSymbols.Invoke((MethodInvoker)(() => item = ListViewSymbols.FindItemWithText(symbol.Name)));

                    //if (item == null && addSymbol)
                    //{
                    //    item = AddSymbolItem(symbol);
                    //    item.Tag = symbol;
                    //    FillSymbolItem(symbol, item);

                    //    ListViewSymbols.Invoke((MethodInvoker)(() => ListViewSymbols.Items.Add(item)));
                    //}
                    //if (item != null)
                    //{
                    //    if (!addSymbol)
                    //        ListViewSymbols.Invoke((MethodInvoker)(() => ListViewSymbols.Items.Remove(item)));
                    //    else
                    //        ListViewSymbols.Invoke((MethodInvoker)(() => FillSymbolItem(symbol, item)));
                    //}
                }
            }
            SymbolListView.Clear();
            GridSymbolView.AddObject(range);
            GridSymbolView.AdjustObjectCount();
            GridSymbolView.ApplySorting();
        }
    }

    private void SymbolFilter_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SymbolsHaveChangedEvent("");
        }
    }

    private void PositionsHaveChangedEvent(string text)
    {
        if (!GlobalData.ApplicationIsClosing && GlobalData.ActiveAccount != null)
        {
            List<CryptoPosition> list = [];
            foreach (var position in GlobalData.ActiveAccount.Data.PositionList.Values)
            {
                list.Add(position);
            }

            //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#start");

            // Alle positie gerelateerde zaken verversen
            Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    dataGridViewPositionOpen.SuspendDrawing();
                    try
                    {
                        PositionOpenListView.Clear();
                        GridPositionOpenView.AddObject(list);
                        GridPositionOpenView.AdjustObjectCount();
                        GridPositionOpenView.ApplySorting();
                    }
                    finally
                    {
                        dataGridViewPositionOpen.ResumeDrawing();
                    }

                    dataGridViewPositionClosed.SuspendDrawing();
                    try
                    {
                        PositionClosedListView.Clear();
                        GridPositionClosedView.AddObject(GlobalData.PositionsClosed);
                        GridPositionClosedView.AdjustObjectCount();
                        GridPositionClosedView.ApplySorting();

                        dashBoardControl1.RefreshInformation(null, null);
                        //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#einde");
                    }
                    finally
                    {
                        dataGridViewPositionClosed.ResumeDrawing();
                    }
                }));
            });
        }
    }

    private void TabControl_SelectedIndexChanged(object? sender, EventArgs? e)
    {
        // Beetje rare plek om deze te initialiseren, voila...
        if (tabControl.SelectedTab == tabPageBrowser && webViewTradingView.Source == null)
        {
            string symbolname = "BTCUSDT";
            CryptoInterval interval = GlobalData.IntervalListPeriod[0];
            if (GlobalData.Settings.General.Exchange!.SymbolListName.TryGetValue(symbolname, out CryptoSymbol? symbol))
                Invoke((System.Windows.Forms.MethodInvoker)(() =>
                LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false)));
        }

    }

    private void TestCreateUrlTestFileClick(object? sender, EventArgs? e)
    {
        GlobalData.ExternalUrls.Clear();
        GlobalData.ExternalUrls.InitializeUrls();
        string filename = GlobalData.GetBaseDir() + $"{GlobalData.AppName}-weblinks.json";
        string text = JsonSerializer.Serialize(GlobalData.ExternalUrls, JsonTools.JsonSerializerIndented);
        File.WriteAllText(filename, text);


        string symbolname = "XRPUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        if (GlobalData.Settings.General.Exchange!.SymbolListName.TryGetValue(symbolname, out CryptoSymbol? symbol))
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("<html>");
            stringBuilder.AppendLine("<body>");
            foreach (var exchange in GlobalData.ExchangeListName.Values)
            {
                stringBuilder.AppendLine("<br>");
                stringBuilder.AppendLine("<br>");
                stringBuilder.AppendLine($"{exchange.Name}<br>");

                //foreach (var x in GlobalData.ExternalUrls)
                {
                    foreach (CryptoTradingApp tradingApp in Enum.GetValues(typeof(CryptoTradingApp)))
                    {
                        // standaard url voor de exchange
                        (string url1, CryptoExternalUrlType _) = GlobalData.ExternalUrls.GetExternalRef(exchange, tradingApp, false, symbol, interval);
                        stringBuilder.AppendLine($"<a href=\"{url1}\">{tradingApp} {symbol.Name} {interval.Name}</a><br>");

                        // url via de cc scanner voor hypertrader
                        (string url2, CryptoExternalUrlType _) = GlobalData.ExternalUrls.GetExternalRef(exchange, tradingApp, true, symbol, interval);
                        if (url1 != url2)
                            stringBuilder.AppendLine($"<a href=\"{url2}\">Telegram {tradingApp} {symbol.Name} {interval.Name}</a><br>");
                    }
                }
            }

            stringBuilder.AppendLine("</body>");
            stringBuilder.AppendLine("</html>");


            filename = GlobalData.GetBaseDir() + @"\trading app urls.html";
            File.WriteAllText(filename, stringBuilder.ToString());
        }
    }


    public void RefreshDataGrids()
    {
        // Refresh displayed information
        GridSignalView.Clear();
        GridPositionOpenView.Clear();
        GridPositionClosedView.Clear();
        GlobalData.PositionsClosed.Clear(); // weird, move to account?
        // weird queue setup
        GlobalData.LoadSignals();
        GridSignalView.Grid.Invalidate();

        // another weird queue
        GlobalData.ActiveAccount!.Data.PositionList.Clear();
        TradeTools.LoadOpenPositions();
        TradeTools.LoadClosedPositions();
        PositionsHaveChangedEvent("");
    }

    private void ApplicationBackTestMode_Click(object? sender, EventArgs? e)
    {
        ApplicationBackTestMode.Checked = !ApplicationBackTestMode.Checked;
        if (ApplicationBackTestMode.Checked)
        {
            GlobalData.BackTest = true;
            GlobalData.BackTestDateTime = GlobalData.Settings.BackTest.BackTestStartTime;
            GlobalData.Settings.Trading.ActiveBackup = GlobalData.Settings.Trading.Active;
            GlobalData.Settings.Trading.Active = true;
        }
        else
        {
            GlobalData.BackTest = false;
            GlobalData.Settings.Trading.Active = GlobalData.Settings.Trading.ActiveBackup;
        }
        ApplicationTradingBot.Enabled = !GlobalData.BackTest;
        ApplicationPlaySounds.Enabled = !GlobalData.BackTest;
        ApplicationCreateSignals.Enabled = !GlobalData.BackTest;
        ApplicationBackTestExec.Enabled = GlobalData.BackTest;

        GlobalData.SaveSettings();
        SetApplicationTitle();

        GlobalData.SetTradingAccounts();
        RefreshDataGrids();

        // Resume scanner session, fill missing information
        if (!GlobalData.BackTest)
            ToolStripMenuItemRefresh_Click_1(null, null);
    }

    private async void BacktestToolStripMenuItem_Click(object? sender, EventArgs? e)
    {
        /// TODO: Deze code verhuizen naar aparte class of het dialoog zelf?
        /// Probleem: Door recente aanpassingen lopen de meldingen en accounts 
        /// allemaal door elkaar (misschien een extra tabsheet met de resultaten?)
        /// (waarschijnlijk werkt het niets eens meer! was tijdelijk experiment)

        try
        {
            AskSymbolDialog form = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            if (form.ShowDialog() == DialogResult.OK)
            {
                GlobalData.SaveSettings();

                if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? exchange))
                {
                    MessageBox.Show("Exchange bestaat niet");
                    return;
                }

                // Bestaat de coin? (uiteraard, net geladen)
                if (!exchange.SymbolListName.TryGetValue(GlobalData.Settings.BackTest.BackTestSymbol, out CryptoSymbol? symbol))
                {
                    MessageBox.Show("Symbol bestaat niet");
                    return;
                }

                if (!GlobalData.BackTest)
                {
                    ApplicationBackTestMode_Click(sender, e);
                    if (GlobalData.ActiveAccount!.AccountType == CryptoAccountType.PaperTrade)
                        await PaperTrading.CheckPositionsAfterRestart(GlobalData.ActiveAccount!);
                }

                BackTestAsync();
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }

    }

    public void BackTestAsync()
    {
        if (!GlobalData.BackTest)
            return;
        if (!GlobalData.Settings.General.Exchange!.SymbolListName.TryGetValue("BTCUSDT", out CryptoSymbol? btcSymbol))
            return;
        if (!GlobalData.Settings.General.Exchange!.SymbolListName.TryGetValue(GlobalData.Settings.BackTest.BackTestSymbol, out CryptoSymbol? symbol))
            return;

        MainMenuClearAll_Click(null, null);
        GlobalData.ActiveAccount!.Data.Clear();
        Emulator.DeletePreviousData();
        RefreshDataGrids();

        var _ = Task.Run(async () =>
        {
            await Emulator.Execute(btcSymbol, symbol);
            PositionsHaveChangedEvent("");
            RefreshDataGrids();
        });
        return;
    }


    private void SplitContainerSplitterMoved(object? sender, SplitterEventArgs e)
    {
        // save SplitterDistance to user settings
        if (GlobalData.SettingsUser.MainForm.SplitterDistance != splitContainer1.SplitterDistance)
        {
            GlobalData.SettingsUser.MainForm.SplitterDistance = splitContainer1.SplitterDistance;
            GlobalData.SaveUserSettings();
        }
    }

}
