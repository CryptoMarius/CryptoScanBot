using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;

using Microsoft.Win32;

using Nito.AsyncEx;

using System.Reflection;
using System.Text;

namespace CryptoSbmScanner;

public partial class FrmMain : Form
{
    private readonly ColorSchemeTest theme = new();

    public class ColorSchemeTest
    {
        public Color Background { get; set; } = Color.Black;
        public Color Foreground { get; set; } = Color.White;
    }


    public FrmMain()
    {
        InitializeComponent();

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appName = assembly.Name.ToString();
        string appVersion = assembly.Version.ToString();
        while (appVersion.EndsWith(".0"))
            appVersion = appVersion[0..^2];
        Text = appName + " " + appVersion;



        // Om vanuit achtergrond threads iets te kunnen loggen of te doen
        GlobalData.PlaySound += new PlayMediaEvent(PlaySound);
        GlobalData.PlaySpeech += new PlayMediaEvent(PlaySpeech);
        GlobalData.LogToTelegram += new AddTextEvent(AddTextToTelegram);
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);

        // Niet echt een text event, meer misbruik van het event type
        GlobalData.AssetsHaveChangedEvent += new AddTextEvent(AssetsHaveChangedEvent);
        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        GlobalData.PositionsHaveChangedEvent += new AddTextEvent(OpenPositionsHaveChangedEvent);

        GlobalData.SignalEvent = SignalArrived;

        // Events inregelen
        ScannerSession.TimerClearMemo.Elapsed += TimerClearMemo_Tick;
        ScannerSession.TimerAddSignal.Elapsed += TimerAddSignalsAndLog_Tick;
        ScannerSession.TimerSoundHeartBeat.Elapsed += TimerSoundHeartBeat_Tick;
        ScannerSession.TimerShowInformation.Elapsed += dashBoardInformation1.TimerShowInformation_Tick;


        // Instelling laden waaronder de API enzovoort
        GlobalData.LoadSettings();

        // Partial class "constructors"
        ListViewSignalsConstructor();
        ListViewSymbolsConstructor();
        ListViewPositionsOpenConstructor();
        ListViewPositionsClosedConstructor();

        // Dummy browser tabblad verbergen, is enkel een browser om dat extra dialoog in externe browser te vermijden
        // (onderstaande aanpak verdient aandacht, deze zit niet fijn in elkaar met de tabPage && focus zetten)
        LinkTools.tabControl = tabControl;
        LinkTools.tabPageBrowser = tabPageBrowser;
        LinkTools._webViewDummy = webViewDummy;
        LinkTools.webViewTradingView = webViewTradingView;
        //_webViewDummy = webViewDummy; // kan weg?
        tabControl.TabPages.Remove(tabPagewebViewDummy);

#if !TRADEBOT
        ApplicationTradingBot.Visible = false;
        backtestToolStripMenuItem.Visible = false;
        GlobalData.Settings.Trading.Active = false;
        tabControl.TabPages.Remove(tabPageDashBoard);
        tabControl.TabPages.Remove(tabPagePositionsOpen);
        tabControl.TabPages.Remove(tabPagePositionsClosed);
#endif


        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();

        // Is er via de command line aangegeven dat we default een andere exchange willen?
        {
            ApplicationParams.InitApplicationOptions();

            string exchangeName = ApplicationParams.Options.ExchangeName;
            if (exchangeName != null)
            {
                // De default exchange is Binance (maar of dat tegenwoordig een goede keuze is)
                if (exchangeName == "")
                    exchangeName = "Binance";
                if (GlobalData.ExchangeListName.TryGetValue(exchangeName, out var exchange))
                {
                    GlobalData.Settings.General.Exchange = exchange;
                    GlobalData.Settings.General.ExchangeId = exchange.Id;
                    GlobalData.Settings.General.ExchangeName = exchange.Name;
                }
                else throw new Exception(string.Format("Exchange {0} bestaat niet", exchangeName));
            }
        }
        ExchangeHelper.ExchangeDefaults();
        GlobalData.LoadAccounts();

        WindowLocationRestore();
        ApplySettings();


        ScannerSession.Start(false);
        dashBoardControl1.InitializeStuff();
        SystemEvents.PowerModeChanged += OnPowerChange;
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

        if ((GlobalData.Settings.General.FontSize != Font.Size) || (GlobalData.Settings.General.FontName.Equals(Font.Name)))
        {
            Font = new System.Drawing.Font(GlobalData.Settings.General.FontName, GlobalData.Settings.General.FontSize,
                System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            //this.applicationMenuStrip.Font.Size = GlobalData.Settings.General.FontSize;
            dashBoardControl1.Font = Font;
        }

        ListboxSymbolsInitCaptions();
        ListViewSignalsInitCaptions();


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
        ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.SignalsActive;

        panelLeft.Visible = !GlobalData.Settings.General.HideSymbolsOnTheLeft;
        //panelLeftTop.Visible = !GlobalData.Settings.General.HideSymbolsOnTheLeft;
        //listBoxSymbols.Visible = !GlobalData.Settings.General.HideSymbolsOnTheLeft;

        this.Refresh();
        //GlobalData.DumpSessionInformation();
    }


    private void OnPowerChange(object s, PowerModeChangedEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerModes.Resume:
                GlobalData.AddTextToLogTab("PowerMode - Resume");
                ScannerSession.Start(true);
                //AsyncContext.Run(ScannerSession.Start(true));
                break;
            case PowerModes.Suspend:
                GlobalData.AddTextToLogTab("PowerMode - Suspend");
                this.SaveWindowLocation(false);
                //Task.Run(async () => { await ScannerSession.Stop(); }).Wait();
                AsyncContext.Run(ScannerSession.Stop);
                break;
        }
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.SaveWindowLocation(false);
            AsyncContext.Run(ScannerSession.Stop);

            if (components != null)
            {
                GlobalData.ApplicationStatus = CryptoApplicationStatus.Disposing;
                LinkTools._webViewDummy.Dispose(); // dirty
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
        //if ((components != null) && (IsHandleCreated)) gaat nu via een queue, wordt wel opgepakt
        {
            text = text.TrimEnd();
            GlobalData.Logger.Info(text);

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
    }

    /// <summary>
    /// Dan is er in de achtergrond een verversing actie geweest, display bijwerken!
    /// </summary>
    private void AssetsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if (components != null && IsHandleCreated)
        {
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
        }
    }


    private void ToolStripMenuItemRefresh_Click_1(object sender, EventArgs e)
    {
        _ = ExchangeHelper.FetchCandlesAsync(); // niet wachten tot deze klaar is
    }


    private static void GetReloadRelatedSettings(out string activeQuoteData)
    {
        activeQuoteData = "";
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles)
                activeQuoteData += "," + quoteData.Name;
        }
    }


    private async void ToolStripMenuItemSettings_Click(object sender, EventArgs e)
    {
        SettingsBasic oldSettings = GlobalData.Settings;

        GetReloadRelatedSettings(out string activeQuotes);
        Model.CryptoExchange oldExchange = GlobalData.Settings.General.Exchange;

        // Dan wordt de basecoin en coordinaten etc. bewaard voor een volgende keer
        this.SaveWindowLocation(false);
        GlobalData.Settings.Trading.Active = ApplicationTradingBot.Checked;
        GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
        GlobalData.Settings.Signal.SignalsActive = ApplicationCreateSignals.Checked;
        dashBoardInformation1.PickupBarometerProperties();
        try
        {
            FrmSettings dialog = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            ChangeTheme(theme, dialog);
            dialog.InitSettings(GlobalData.Settings);
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
                AsyncContext.Run(ScannerSession.Stop);

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

                // Optioneel een herstart van de Telegram bot
                if (GlobalData.Settings.Telegram.Token != ThreadTelegramBot.Token)
                    await ThreadTelegramBot.Start(GlobalData.Settings.Telegram.Token, GlobalData.Settings.Telegram.ChatId);
                ThreadTelegramBot.ChatId = GlobalData.Settings.Telegram.ChatId;

                ExchangeHelper.ExchangeDefaults();
                MainMenuClearAll_Click(null, null);
                // Schedule een reload of data
                ScannerSession.ScheduleRefresh();
            }
            else ApplySettings();
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }
    }

    //private class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    //{
    //    public int Compare(TKey x, TKey y)
    //    {
    //        int result = x.CompareTo(y);

    //        if (result == 0)
    //            return 1;   // Handle equality as beeing greater
    //        else
    //            return result;
    //    }
    //}

    private void MainMenuClearAll_Click(object sender, EventArgs e)
    {
        //this.Text = "";
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
        // Speed up adding signals
        if (GlobalData.SignalQueue.Count > 0 && !IsDisposed && GlobalData.ApplicationStatus != CryptoApplicationStatus.Disposing)
        {
            Monitor.Enter(GlobalData.SignalQueue);
            try
            {
                List<CryptoSignal> signals = new();

                while (GlobalData.SignalQueue.Count > 0)
                {
                    CryptoSignal signal = GlobalData.SignalQueue.Dequeue();
                    if (signal != null)
                        signals.Add(signal);
                }

                if (signals.Any())
                {
                    // verwerken..
                    Task.Factory.StartNew(() =>
                    {
                        Invoke(new Action(() =>
                        {
                            if (!this.IsDisposed && GlobalData.ApplicationStatus != CryptoApplicationStatus.Disposing)
                            {
                                ListViewSignalsAddSignalRange(signals);
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
        if (logQueue.Count > 0 && !IsDisposed && GlobalData.ApplicationStatus != CryptoApplicationStatus.Disposing)
        {
            Monitor.Enter(logQueue);
            try
            {
                List<CryptoSignal> signals = new();
                StringBuilder stringBuilder = new();

                while (logQueue.Count > 0 && !IsDisposed && GlobalData.ApplicationStatus != CryptoApplicationStatus.Disposing)
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
                        if (!this.IsDisposed && GlobalData.ApplicationStatus != CryptoApplicationStatus.Disposing)
                        {
                            if (InvokeRequired)
                                Invoke((MethodInvoker)(() => TextBoxLog.AppendText(text)));
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
        Invoke((MethodInvoker)(() => TextBoxLog.Clear()));
    }


    private void SignalArrived(CryptoSignal signal)
    {
        GlobalData.createdSignalCount++;
        string text = "signal: " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.SideText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);

        if (signal.BackTest)
            return;

        // Zet de laatste munt in de "caption" (en taskbar) van de applicatie bar (visuele controle of er meldingen zijn)
        //Invoke(new Action(() => { this.Text = signal.Symbol.Name + " " + createdSignalCount.ToString(); }));

        if (!signal.IsInvalid || (signal.IsInvalid && GlobalData.Settings.General.ShowInvalidSignals))
            GlobalData.SignalQueue.Enqueue(signal);

        // Speech and/or sound
        if (!signal.IsInvalid)
        {
            switch (signal.Strategy)
            {
                case CryptoSignalStrategy.Jump:
                    if (signal.Side == CryptoOrderSide.Buy)
                        PlaySound(signal, GlobalData.Settings.Signal.PlaySoundCandleJumpSignal, GlobalData.Settings.Signal.PlaySpeechCandleJumpSignal,
                            GlobalData.Settings.Signal.SoundCandleJumpUp, ref LastSignalSoundCandleJumpUp);
                    if (signal.Side == CryptoOrderSide.Sell)
                        PlaySound(signal, GlobalData.Settings.Signal.PlaySoundCandleJumpSignal, GlobalData.Settings.Signal.PlaySpeechCandleJumpSignal,
                            GlobalData.Settings.Signal.SoundCandleJumpDown, ref LastSignalSoundCandleJumpDown);
                    break;

                case CryptoSignalStrategy.Stobb:
                    if (signal.Side == CryptoOrderSide.Buy)
                        PlaySound(signal, GlobalData.Settings.Signal.PlaySoundStobbSignal, GlobalData.Settings.Signal.PlaySpeechStobbSignal,
                            GlobalData.Settings.Signal.SoundStobbOversold, ref LastSignalSoundStobbOversold);
                    if (signal.Side == CryptoOrderSide.Sell)
                        PlaySound(signal, GlobalData.Settings.Signal.PlaySoundStobbSignal, GlobalData.Settings.Signal.PlaySpeechStobbSignal,
                            GlobalData.Settings.Signal.SoundStobbOverbought, ref LastSignalSoundStobbOverbought);
                    break;

                case CryptoSignalStrategy.Sbm1:
                case CryptoSignalStrategy.Sbm2:
                case CryptoSignalStrategy.Sbm3:
                case CryptoSignalStrategy.Sbm4:
                case CryptoSignalStrategy.Sbm5:
                    if (signal.Side == CryptoOrderSide.Buy)
                        PlaySound(signal, GlobalData.Settings.Signal.PlaySoundSbmSignal, GlobalData.Settings.Signal.PlaySpeechSbmSignal,
                        GlobalData.Settings.Signal.SoundSbmOversold, ref LastSignalSoundSbmOversold);
                    if (signal.Side == CryptoOrderSide.Sell)
                        PlaySound(signal, GlobalData.Settings.Signal.PlaySoundSbmSignal, GlobalData.Settings.Signal.PlaySpeechSbmSignal,
                            GlobalData.Settings.Signal.SoundSbmOverbought, ref LastSignalSoundSbmOverbought);
                    break;
            }


            if (GlobalData.Settings.Telegram.SendSignalsToTelegram)
                ThreadTelegramBot.SendSignal(signal);
        }
    }



    //private void ChangeTheme(ColorSchemeTest theme, Control.ControlCollection container)
    //{
    //    //return;
    //    //foreach (Control component in container)
    //    //{
    //    //    if (component is Form)
    //    //        ((MetroFramework.Forms.MetroForm)component).StyleManager = this.StyleManager;
    //    //    if (component is MetroFramework.Controls.MetroPanel)
    //    //        ((MetroFramework.Controls.MetroPanel)component).StyleManager = this.StyleManager;
    //    //    else if (component is MetroFramework.Controls.MetroButton)
    //    //        ((MetroFramework.Controls.MetroButton)component).StyleManager = this.StyleManager;
    //    //    else if (component is MetroFramework.Controls.MetroTextBox)
    //    //        ((MetroFramework.Controls.MetroTextBox)component).StyleManager = this.StyleManager;
    //    //    else if (component is MetroFramework.Controls.MetroScrollBar)
    //    //        ((MetroFramework.Controls.MetroScrollBar)component).StyleManager = this.StyleManager;
    //    //    else if (component is MetroFramework.Controls.MetroLabel)
    //    //        ((MetroFramework.Controls.MetroLabel)component).StyleManager = this.StyleManager;
    //    //    else if (component is MetroFramework.Controls.MetroTabControl)
    //    //        ((MetroFramework.Controls.MetroTabControl)component).StyleManager = this.StyleManager;
    //    //    else if (component is MetroFramework.Controls.MetroTabPage)
    //    //        ((MetroFramework.Controls.MetroTabPage)component).StyleManager = this.StyleManager;
    //    //    //else if (component is MetroFramework.Controls.MetroListBox)
    //    //    //    ((MetroFramework.Controls.MetroListBox)component).StyleManager = this.StyleManager;

    //    //    else if (component is ListBox)
    //    //    {
    //    //    }
    //    //    else if (component is MetroFramework.Controls.MetroListView)
    //    //        ((MetroFramework.Controls.MetroListView)component).StyleManager = this.StyleManager;

    //    //    else if (component is MetroFramework.Controls.MetroComboBox)
    //    //        ((MetroFramework.Controls.MetroComboBox)component).StyleManager = this.StyleManager;
    //    //    else if (component is CheckBox)
    //    //    {
    //    //    }
    //    //    else if (component is MetroFramework.Controls.MetroListView)
    //    //        ((MetroFramework.Controls.MetroListView)component).StyleManager = this.StyleManager;
    //    //    else if (component is MenuStrip)
    //    //    {
    //    //    }
    //    //    else
    //    //        GlobalData.AddTextToLogTab(component.ToString());
    //    //    ChangeTheme(component.Controls);
    //    //}
    //}

    private void ChangeTheme(ColorSchemeTest scheme, Control container)
    {
        //return;
        foreach (Control component in container.Controls)
        {
            if (component is Form)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
            if (component is Panel)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
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
            else if (component is ListViewDoubleBuffered)
            {
                component.BackColor = scheme.Background;
                component.ForeColor = scheme.Foreground;
            }
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
            //else if (component is ToolStripMenuItem)
            //{
            //    component.BackColor = scheme.Background;
            //    component.ForeColor = scheme.Foreground;
            //}
            ChangeTheme(scheme, component);
        }
    }


    private void ApplicationMenuItemAbout_Click(object sender, EventArgs e)
    {
        AboutBox form = new()
        {
            StartPosition = FormStartPosition.CenterParent
        };
        form.ShowDialog();
    }

    private void TimerSoundHeartBeat_Tick(object sender, EventArgs e)
      => GlobalData.PlaySomeMusic("sound-heartbeat.wav");


    private static void ShowTrendInformation(CryptoSymbol symbol)
    {
        StringBuilder log = new();
        log.AppendLine("Trend " + symbol.Name);

        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Trend " + symbol.Name);

        long percentageSum = 0;
        long maxPercentageSum = 0;
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            log.AppendLine("");
            log.AppendLine("----");
            log.AppendLine("Interval " + interval.Name);

            // Wat is het maximale som (voor de eindberekening)
            maxPercentageSum += interval.Duration;

            TrendIndicator trendIndicatorClass = new(symbol, interval)
            {
                Log = log
            };
            CryptoTrendIndicator trendIndicator = trendIndicatorClass.CalculateTrend();
            if (trendIndicator == CryptoTrendIndicator.trendBullish)
                percentageSum += interval.Duration;
            else if (trendIndicator == CryptoTrendIndicator.trendBearish)
                percentageSum -= interval.Duration;


            // Ahh, dat gaat niet naar een tabel (zoals ik eerst dacht)
            //CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(interval.IntervalPeriod);
            //symbolInterval.TrendIndicator = trendIndicator;
            //symbolInterval.TrendInfoDate = DateTime.UtcNow;

            string s = "";
            if (trendIndicator == CryptoTrendIndicator.trendBullish)
                s = string.Format("{0} {1}, trend=bullish", symbol.Name, interval.IntervalPeriod);
            else if (trendIndicator == CryptoTrendIndicator.trendBearish)
                s = string.Format("{0} {1}, trend=bearish", symbol.Name, interval.IntervalPeriod);
            else
                s = string.Format("{0} {1}, trend=sideway's", symbol.Name, interval.IntervalPeriod);
            GlobalData.AddTextToLogTab(s);
            log.AppendLine(s);
        }

        if (maxPercentageSum > 0)
        {
            decimal trendPercentage = 100 * (decimal)percentageSum / (decimal)maxPercentageSum;
            string t = string.Format("{0} {1:N2}", symbol.Name, trendPercentage);
            GlobalData.AddTextToLogTab(t);
            log.AppendLine(t);
        }



        //Laad de gecachte (langere historie, minder overhad)
        string filename = GlobalData.GetBaseDir() + "Trend information.txt";
        File.WriteAllText(filename, log.ToString());
    }

    public void WindowLocationRestore()
    {
        // this is the default
        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.WindowsDefaultBounds;

        // check if the saved bounds are nonzero and visible on any screen
        if (GlobalData.Settings.General.WindowPosition != Rectangle.Empty && IsVisibleOnAnyScreen(GlobalData.Settings.General.WindowPosition))
        {
            // first set the bounds
            StartPosition = FormStartPosition.Manual;
            DesktopBounds = GlobalData.Settings.General.WindowPosition;

            // afterwards set the window state to the saved value (which could be Maximized)
            WindowState = GlobalData.Settings.General.WindowState;
        }
        else
        {
            // this resets the upper left corner of the window to windows standards
            StartPosition = FormStartPosition.WindowsDefaultLocation;

            // we can still apply the saved size
            // msorens: added gatekeeper, otherwise first time appears as just a title bar!
            if (GlobalData.Settings.General.WindowPosition != Rectangle.Empty)
            {
                Size = GlobalData.Settings.General.WindowPosition.Size;
            }
        }
    }

    private static bool IsVisibleOnAnyScreen(Rectangle rect)
      => Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rect));

    private void ApplicationCreateSignals_Click(object sender, EventArgs e)
    {
        ApplicationCreateSignals.Checked = !ApplicationCreateSignals.Checked;
        GlobalData.Settings.Signal.SignalsActive = ApplicationCreateSignals.Checked;
        GlobalData.SaveSettings();
    }

    private void ApplicationPlaySounds_Click(object sender, EventArgs e)
    {
        ApplicationPlaySounds.Checked = !ApplicationPlaySounds.Checked;
        GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
        GlobalData.SaveSettings();
    }

    private void ApplicationTradingBot_Click(object sender, EventArgs e)
    {
        ApplicationTradingBot.Checked = !ApplicationTradingBot.Checked;
        GlobalData.Settings.Trading.Active = ApplicationTradingBot.Checked;
        GlobalData.SaveSettings();
    }


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
                SignalCreate createSignal = new(symbol, interval);
                while (start <= einde)
                {
                    if (symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.TryGetValue(start, out candle))
                    {
                        if (createSignal.Prepare(start))
                        {
                            // todo, configuratie short/long
                            SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(CryptoOrderSide.Buy, GlobalData.Settings.BackTest.BackTestAlgoritm, symbol, interval, candle);
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
                backTestExcel.ExportToExcell(CryptoOrderSide.Buy, GlobalData.Settings.BackTest.BackTestAlgoritm);
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }

    }

}
