using CryptoScanBot.Commands;
using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;
using CryptoScanBot.Trader;

using Microsoft.Win32;

using Nito.AsyncEx;

using System.Reflection;
using System.Text;

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
#if TRADEBOT
    private readonly ToolStripMenuItemCommand ApplicationTradingBot;
    private readonly List<CryptoPosition> PositionOpenListView = [];
    private readonly CryptoDataGridPositionsOpen<CryptoPosition> GridPositionOpenView;

    private readonly List<CryptoPosition> PositionClosedListView = [];
    private readonly CryptoDataGridPositionsClosed<CryptoPosition> GridPositionClosedView;
#endif

    public FrmMain()
    {
        InitializeComponent();

        ApplicationPlaySounds = MenuMain.AddCommand(null, "Geluiden afspelen", Command.None, ApplicationPlaySounds_Click);
        ApplicationPlaySounds.Checked = true;
        ApplicationCreateSignals = MenuMain.AddCommand(null, "Signalen maken", Command.None, ApplicationCreateSignals_Click);
        ApplicationCreateSignals.Checked = true;
#if TRADEBOT
        ApplicationTradingBot = MenuMain.AddCommand(null, "Trading bot actief", Command.None, ApplicationTradingBot_Click);
        ApplicationTradingBot.Checked = true;
#endif
        MenuMain.AddCommand(null, "Instellingen", Command.None, ToolStripMenuItemSettings_Click);
        MenuMain.AddCommand(null, "Verversen informatie", Command.None, ToolStripMenuItemRefresh_Click_1);
        MenuMain.AddCommand(null, "Reset log en getallen", Command.None, MainMenuClearAll_Click);
        MenuMain.AddCommand(null, "Exchange information (Excel)", Command.ExcelExchangeInformation);
#if TRADEBOT
#if SQLDATABASE
        MenuMain.AddCommand(null, "Backtest", Command.None, BacktestToolStripMenuItem_Click);
#endif
#endif
        MenuMain.AddCommand(null, "About", Command.About);

#if DEBUG
        MenuMain.AddCommand(null, "Test - Scanner restart", Command.ScannerSessionDebug);
        MenuMain.AddCommand(null, "Test - Save Candles", Command.None, TestClick);
#endif

        //Console.Write("Hello world 1");
        //System.Diagnostics.Debug.WriteLine("Hello world 2");

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
#if TRADEBOT
        GlobalData.AssetsHaveChangedEvent += new AddTextEvent(AssetsHaveChangedEvent);
        GlobalData.PositionsHaveChangedEvent += new AddTextEvent(PositionsHaveChangedEvent);
#endif

        GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;
        GlobalData.ApplicationHasStarted += new AddTextEvent(ApplicationHasStarted);

        // Events inregelen
        ScannerSession.TimerClearMemo.Elapsed += TimerClearMemo_Tick;
        ScannerSession.TimerAddSignal.Elapsed += TimerAddSignalsAndLog_Tick;
        ScannerSession.TimerSoundHeartBeat.Elapsed += TimerSoundHeartBeat_Tick;
        ScannerSession.TimerShowInformation.Elapsed += dashBoardInformation1.TimerShowInformation_Tick;


        // Instelling laden
        GlobalData.LoadSettings();

        GridSymbolView = new(dataGridViewSymbols, SymbolListView, GlobalData.SettingsUser.GridColumnsSymbol);
        GridSignalView = new(dataGridViewSignals, SignalListView, GlobalData.SettingsUser.GridColumnsSignal);
#if TRADEBOT
        GridPositionOpenView = new(dataGridViewPositionOpen, PositionOpenListView, GlobalData.SettingsUser.GridColumnsPositionsOpen);
        GridPositionClosedView = new(dataGridViewPositionClosed, PositionClosedListView, GlobalData.SettingsUser.GridColumnsPositionsClosed);
#endif

        // Dummy browser verbergen, is een browser om het extra confirmatie dialoog in externe browser te vermijden
        LinkTools.TabControl = tabControl;
        LinkTools.TabPageBrowser = tabPageBrowser;
        LinkTools.WebViewDummy = webViewDummy;
        LinkTools.WebViewTradingView = webViewTradingView;
        tabControl.TabPages.Remove(tabPagewebViewDummy);

#if !TRADEBOT
        GlobalData.Settings.Trading.Active = false;
        tabControl.TabPages.Remove(tabPageDashBoard);
        tabControl.TabPages.Remove(tabPagePositionsOpen);
        tabControl.TabPages.Remove(tabPagePositionsClosed);
#endif

        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();

        // Is er via de command line aangegeven dat we default een andere exchange willen?

        ApplicationParams.InitApplicationOptions();

        string exchangeName = ApplicationParams.Options.ExchangeName;
        if (exchangeName != null)
        {
            // De default exchange is Bybit Spot (Binance is geen goede keuze meer)
            if (exchangeName == "")
                exchangeName = "Bybit Spot";
            if (GlobalData.ExchangeListName.TryGetValue(exchangeName, out var exchange))
            {
                GlobalData.Settings.General.Exchange = exchange;
                GlobalData.Settings.General.ExchangeId = exchange.Id;
                GlobalData.Settings.General.ExchangeName = exchange.Name;
            }
            else throw new Exception(string.Format("Exchange {0} bestaat niet", exchangeName));
        }
    }

    private void TestClick(object sender, EventArgs e)
    {
        DataStore.SaveCandles();
    }

    private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        GlobalData.ApplicationIsClosing = true;
        AsyncContext.Run(ScannerSession.StopAsync);
        //await ScannerSession.StopAsync();
    }

    private void FrmMain_Shown(object sender, EventArgs e)
    {
        GlobalData.ApplicationIsShowed = true;
    }


    /// <summary>
    /// Save Window coordinates and screen
    /// </summary>
    private void FrmMain_Resize(object sender, EventArgs e)
    {
        if (GlobalData.ApplicationIsClosing || !GlobalData.ApplicationIsShowed)
            return;

        ApplicationTools.SaveWindowLocation(this);
        GlobalData.SaveUserSettings();
    }


    private void FrmMain_Load(object sender, EventArgs e)
    {
        ApplicationTools.WindowLocationRestore(this);

        ExchangeHelper.ExchangeDefaults();
        GlobalData.LoadAccounts();
        ApplySettings();

        GlobalData.LoadSymbols();
        GlobalData.SymbolsHaveChanged("");
        GlobalData.LoadSignals();
#if TRADEBOT
        TradeTools.LoadAssets();
        TradeTools.LoadOpenPositions();
        TradeTools.LoadClosedPositions();
        //ClosedPositionsHaveChangedEvent();
        PositionsHaveChangedEvent("");
#endif

        ScannerSession.Start(0);
    }


    private void ShowApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appName = assembly.Name.ToString();
        string appVersion = assembly.Version.ToString();
        while (appVersion.EndsWith(".0"))
            appVersion = appVersion[0..^2];
        string text = $"{appName} {appVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();
        Text = text.Trim();
    }


    private void ApplySettings()
    {
        // De exchange overnemen die is ingesteld (vanuit dialoog wordt het wel gedaan, bij laden)
        if (GlobalData.Settings.General.Exchange == null)
        {
            if (GlobalData.ExchangeListId.TryGetValue(GlobalData.Settings.General.ExchangeId, out var exchange))
            {
                GlobalData.Settings.General.Exchange = exchange;
                GlobalData.Settings.General.ExchangeId = exchange.Id;
                GlobalData.Settings.General.ExchangeName = exchange.Name;
            }
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

#if TRADEBOT
        GridPositionOpenView.InitCommandCaptions();
        GridPositionClosedView.InitCommandCaptions();
#endif


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

#if TRADEBOT
        ApplicationTradingBot.Checked = GlobalData.Settings.Trading.Active;
#endif
        ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive;
        ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.Active;

        panelLeft.Visible = !GlobalData.Settings.General.HideSymbolsOnTheLeft;

        ShowApplicationVersion();
        Refresh(); // Redraw
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
                LinkTools.WebViewDummy.Dispose();
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
                ThreadSoundPlayer.AddToQueue(text);
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

    private void AddTextToTelegram(string text, bool extraLineFeed = false)
    {
        if (IsHandleCreated)
        {
            // Het ding crasht wel eens (meestal netwerk of timing problemen)
            ThreadTelegramBot.SendMessage(text);
        }
    }

    private void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        // Via queue want afzonderlijk regels toevoegen kost relatief veel tijd

        text = text.TrimEnd();
        ScannerLog.Logger.Info(text);

        if (text != "")
            text = DateTime.Now.ToLocalTime() + " " + text;
        //if (extraLineFeed)
        //    text += "\r\n\r\n";
        //else
        //    text += "\r\n";

        //if (InvokeRequired)
        //    Invoke((MethodInvoker)(() => TextBoxLog.AppendText(text)));
        //else
        //    TextBoxLog.AppendText(text);

        //testen!
        if (extraLineFeed)
            text += "\r\n";
        logQueue.Enqueue(text);

        //// even rechstreeks
        //text = text.TrimEnd() + "\r\n";
        //if (InvokeRequired)
        //    Invoke((MethodInvoker)(() => TextBoxLog.AppendText(text)));
        //else
        //    TextBoxLog.AppendText(text);
    }

    private void TelegramHasChangedEvent(string text, bool extraLineFeed = false)
    {
#if TRADEBOT
        Invoke((System.Windows.Forms.MethodInvoker)(() => ApplicationTradingBot.Checked = GlobalData.Settings.Trading.Active));
#endif
        Invoke((System.Windows.Forms.MethodInvoker)(() => ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive));
        Invoke((System.Windows.Forms.MethodInvoker)(() => ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.Active));
    }


#if TRADEBOT
    /// <summary>
    /// Dan is er in de achtergrond een verversing actie geweest, display bijwerken!
    /// </summary>
    private void AssetsHaveChangedEvent(string text, bool extraLineFeed = false)
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
#endif

    private void ToolStripMenuItemRefresh_Click_1(object sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            await ExchangeHelper.FetchSymbolsAsync(); // niet wachten tot deze klaar is
            await ExchangeHelper.KLineTicker.CheckKlineTickers(); // herstarten van ticker indien errors
            await ExchangeHelper.FetchCandlesAsync(); // niet wachten tot deze klaar is
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


    private async void ToolStripMenuItemSettings_Click(object sender, EventArgs e)
    {
        SettingsBasic oldSettings = GlobalData.Settings;

        GetReloadRelatedSettings(out string activeQuotes);
        Model.CryptoExchange oldExchange = GlobalData.Settings.General.Exchange;

        // Dan wordt de basecoin en coordinaten etc. bewaard voor een volgende keer
#if TRADEBOT
        GlobalData.Settings.Trading.Active = ApplicationTradingBot.Checked;
#endif
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
            GetReloadRelatedSettings(out string activeQuotes2);

            // Detectie of we hebben gewisseld van Exchange (reload) of QuoteData (reload)
            bool reloadQuoteChange = activeQuotes != activeQuotes2;
            bool reloadExchangeChange = dialog.NewExchange.Id != GlobalData.Settings.General.ExchangeId;
            if (reloadQuoteChange || reloadExchangeChange)
            {
                GlobalData.AddTextToLogTab("");
                if (reloadExchangeChange)
                    GlobalData.AddTextToLogTab("De exchange is aangepast (reload)!");
                else if (reloadQuoteChange)
                    GlobalData.AddTextToLogTab("De lijst met quote's is aangepast (reload)!");
                AsyncContext.Run(ScannerSession.StopAsync);

                GlobalData.Settings.General.Exchange = dialog.NewExchange;
                GlobalData.Settings.General.ExchangeId = dialog.NewExchange.Id;
                GlobalData.Settings.General.ExchangeName = dialog.NewExchange.Name;
                GlobalData.SaveSettings();

                // Standaard timers e.d.
                ApplySettings();

                if (reloadExchangeChange)
                {
                    // Exchange: Symbols clearen
                    oldExchange.Clear();

                    // TradingAccount: Posities en Assets clearen!
                    foreach (CryptoTradeAccount ta in GlobalData.TradeAccountList.Values)
                        ta.Clear();
                }

                // Bij wijzigingen aantal signalen)
                //signal.ExpirationDate = signal.CloseDate.AddSeconds(GlobalData.Settings.General.RemoveSignalAfterxCandles * Interval.Duration);

                // Optioneel een herstart van de Telegram bot
                if (GlobalData.Telegram.Token != ThreadTelegramBot.Token)
                    await ThreadTelegramBot.Start(GlobalData.Telegram.Token, GlobalData.Telegram.ChatId);
                ThreadTelegramBot.ChatId = GlobalData.Telegram.ChatId;

                ExchangeHelper.ExchangeDefaults();
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


    private void MainMenuClearAll_Click(object sender, EventArgs e)
    {
        TextBoxLog.Clear();
        GlobalData.createdSignalCount = 0;

        PositionMonitor.AnalyseCount = 0;
        ExchangeHelper.KLineTicker.Reset();
        ExchangeHelper.PriceTicker.Reset();
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


    private readonly Queue<string> logQueue = new();


    private void TimerAddSignalsAndLog_Tick(object sender, EventArgs e)
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

                    // verwerken..
                    Task.Factory.StartNew(() =>
                    {
                        Invoke(new Action(() =>
                        {
                            string text = stringBuilder.ToString().TrimEnd() + "\r\n";
                            if (!GlobalData.ApplicationIsClosing)
                            {
                                if (InvokeRequired)
                                    Invoke((System.Windows.Forms.MethodInvoker)(() =>
                                    {
                                        TextBoxLog.AppendText(text);
                                    }));
                                else
                                    TextBoxLog.AppendText(text);
                            }
                        }));
                    });
                }
                finally
                {
                    Monitor.Exit(logQueue);
                }
        }
    }


    private void TimerClearMemo_Tick(object sender, EventArgs e)
    {
        // Elke 24 uur wordt de memo gecleared
        Invoke((System.Windows.Forms.MethodInvoker)(() => TextBoxLog.Clear()));

        // De database een beetje opruimen
        CryptoDatabase.CleanUpDatabase();
    }


    private void AnalyzeSignalCreated(CryptoSignal signal)
    {
        GlobalData.createdSignalCount++;
        string text = "Analyze signal " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);

        if (signal.BackTest)
            return;

        // Zet de laatste munt in de "caption" (en taskbar) van de applicatie bar (visuele controle of er meldingen zijn)
        //Invoke(new Action(() => { this.Text = signal.Symbol.Name + " " + createdSignalCount.ToString(); }));

        if (!signal.IsInvalid || (signal.IsInvalid && GlobalData.Settings.General.ShowInvalidSignals))
        {
            GlobalData.SignalList.Add(signal);
            GlobalData.SignalQueue.Enqueue(signal);
        }

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
                case CryptoSignalStrategy.Sbm4:
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
                            GlobalData.Settings.Signal.StoRsi.SoundFileLong, ref LastSignalSoundStobbOversold);
                    if (signal.Side == CryptoTradeSide.Short)
                        PlaySound(signal, GlobalData.Settings.Signal.StoRsi.PlaySound, GlobalData.Settings.Signal.StoRsi.PlaySpeech,
                            GlobalData.Settings.Signal.StoRsi.SoundFileShort, ref LastSignalSoundStobbOverbought);
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


    private void TimerSoundHeartBeat_Tick(object sender, EventArgs e)
      => GlobalData.PlaySomeMusic("sound-heartbeat.wav");

    private void ApplicationCreateSignals_Click(object sender, EventArgs e)
    {
        ApplicationCreateSignals.Checked = !ApplicationCreateSignals.Checked;
        GlobalData.Settings.Signal.Active = ApplicationCreateSignals.Checked;
        GlobalData.SaveSettings();
    }

    private void ApplicationPlaySounds_Click(object sender, EventArgs e)
    {
        ApplicationPlaySounds.Checked = !ApplicationPlaySounds.Checked;
        GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
        GlobalData.SaveSettings();
    }

#if TRADEBOT
    private void ApplicationTradingBot_Click(object sender, EventArgs e)
    {
        ApplicationTradingBot.Checked = !ApplicationTradingBot.Checked;
        GlobalData.Settings.Trading.Active = ApplicationTradingBot.Checked;
        GlobalData.SaveSettings();
    }
#endif


#if SQLDATABASE
    private void BacktestToolStripMenuItem_Click(object sender, EventArgs e)
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

                if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
                {
                    MessageBox.Show("Exchange bestaat niet");
                    return;
                }

                // Bestaat de coin? (uiteraard, net geladen)
                if (!exchange.SymbolListName.TryGetValue(GlobalData.Settings.BackTest.BackTestSymbol, out CryptoSymbol symbol))
                {
                    MessageBox.Show("Symbol bestaat niet");
                    return;
                }

                CryptoInterval interval = null;
                //CryptoInterval interval = GlobalData.IntervalList => (Name == GlobalData.Settings.BackTestInterval); ???
                foreach (CryptoInterval intervalX in GlobalData.IntervalList)
                {
                    if (intervalX.Name == GlobalData.Settings.BackTest.BackTestInterval)
                    {
                        interval = intervalX;
                        break;
                    }
                }
                if (interval == null)
                {
                    MessageBox.Show("Interval bestaat niet");
                    return;
                }

                long unix = CandleTools.GetUnixTime(GlobalData.Settings.BackTest.BackTestTime, interval.Duration);
                if (!symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.TryGetValue(unix, out CryptoCandle candle))
                {
                    MessageBox.Show("Candle bestaat niet");
                    return;
                }

                long einde = candle.OpenTime;
                long start = einde - 2 * 60 * interval.Duration;
                foreach (CryptoTradeSide side in Enum.GetValues(typeof(CryptoTradeSide))) // niet efficient meer?
                {
                    SignalCreate createSignal = new(symbol, interval, side, start + interval.Duration);
                    while (start <= einde)
                    {
                        if (symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.TryGetValue(start, out candle))
                        {
                            if (createSignal.Prepare(start))
                            {
                                // todo, configuratie short/long
                                SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(CryptoTradeSide.Long, GlobalData.Settings.BackTest.BackTestAlgoritm, symbol, interval, candle);
                                if (algorithm != null)
                                {
                                    if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                                    {
                                        //createSignal.PrepareAndSendSignal(algorithm);
                                        algorithm.ExtraText = "Signal!";
                                    }
                                    //candle.ExtraText = algorithm.ExtraText;
                                }
                            }
                        }
                        start += interval.Duration;
                    }

                    BackTestExcel backTestExcel = new(symbol, createSignal.history);
                    backTestExcel.ExportToExcell(GlobalData.Settings.BackTest.BackTestAlgoritm);
                }

            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }

    }
#endif


    private void ApplicationHasStarted(string text, bool extraLineFeed = false)
    {
        GlobalData.SymbolsHaveChanged("");

        // De barometer een zetje geven...
        Invoke((System.Windows.Forms.MethodInvoker)(() => dashBoardInformation1.ShowBarometerStuff(null, null)));

#if TRADEBOT
        // Toon de ingelezen posities
        GlobalData.PositionsHaveChanged("");
#endif
    }


    private void SymbolsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
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

    private void SymbolFilter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SymbolsHaveChangedEvent("");
        }
    }

#if TRADEBOT
    private void PositionsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if (!GlobalData.ApplicationIsClosing)
        {
            List<CryptoPosition> list = [];
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
            {
                foreach (var tradingAccount in GlobalData.TradeAccountList.Values)
                {
                    foreach (var position in tradingAccount.PositionList.Values)
                    {
                        list.Add(position);
                    }
                }
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
#endif

    private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Beetje rare plek om deze te initialiseren, voila...
        if (tabControl.SelectedTab == tabPageBrowser && webViewTradingView.Source == null)
        {
            string symbolname = "BTCUSDT";
            CryptoInterval interval = GlobalData.IntervalListPeriod[0];
            if (GlobalData.Settings.General.Exchange.SymbolListName.TryGetValue(symbolname, out CryptoSymbol symbol))
                Invoke((System.Windows.Forms.MethodInvoker)(() =>
                LinkTools.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false)));
        }

    }

}
