using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;
using CryptoSbmScanner.Signal;
using CryptoSbmScanner.TradingView;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;

namespace CryptoSbmScanner;


public partial class FrmMain : Form //MetroFramework.Forms.MetroForm //Form //MaterialForm //
{
    private bool ProgramExit; // = false; //Mislukte manier om excepties bij afsluiten te voorkomen (todo)
    private int createdSignalCount; // = 0; // Tellertje met het aantal meldingen (komt in de taakbalk c.q. applicatie titel)
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _webViewAltradyRef = null;
    private readonly ColorSchemeTest theme = new();
    private readonly HttpClient httpClient;

    private System.Windows.Forms.Timer TimerGetCandles = new();
    private System.Windows.Forms.Timer TimerSoundHeartBeat = new();
    private System.Windows.Forms.Timer TimerRestartStreams = new();
    private System.Windows.Forms.Timer TimerCheckDataStream = new();

    public class ColorSchemeTest
    {
        public Color Background { get; set; } = Color.Black;
        public Color Foreground { get; set; } = Color.White;
    }


    public class FGIndex
    {
        public FGIndexData[] Data { get; set; }
    }

    public class FGIndexData
    {
        public string Value { get; set; }
    }


    public FrmMain()
    {
        InitializeComponent();

        httpClient = new HttpClient();

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appName = assembly.Name.ToString();
        string appVersion = assembly.Version.ToString();
        labelVersion.Text = "Version " + appVersion;
        this.Text = appName + " " + appVersion;


        // Om vanuit achtergrond threads iets te kunnen loggen of te doen
        GlobalData.PlaySound += new PlayMediaEvent(PlaySound);
        GlobalData.PlaySpeech += new PlayMediaEvent(PlaySpeech);
        GlobalData.LogToTelegram += new AddTextEvent(AddTextToTelegram);
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);
        GlobalData.SetCandleTimerEnableEvent += new SetCandleTimerEnable(SetCandleTimerEnableHandler);

        // Niet echt een text event, meer misbruik van het event type
        //GlobalData.AssetsHaveChangedEvent += new AddTextEvent(AssetsHaveChangedEvent); (todo)
        GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
        GlobalData.ConnectionWasLostEvent += new AddTextEvent(ConnectionWasLostEvent);
        GlobalData.ConnectionWasRestoredEvent += new AddTextEvent(ConnectionWasRestoredEvent);


        // Wat event handles zetten
        comboBoxBarometerQuote.SelectedIndexChanged += ShowBarometerStuff;
        comboBoxBarometerInterval.SelectedIndexChanged += ShowBarometerStuff;
        // suffe Visual Studio houdt er niet van als je het opsplitst in 3 partial forms (maakt het soms opnieuw aan)
        listBoxSymbols.DoubleClick += new System.EventHandler(this.ListBoxSymbols_DoubleClick);
        listViewSignals.DoubleClick += new System.EventHandler(this.ListViewSignalsMenuItem_DoubleClick);
        listViewSymbolPrices.DoubleClick += new System.EventHandler(this.ListViewSymbolPrices_DoubleClick);


        // Instelling laden waaronder de API enzovoort
        GlobalData.LoadSettings();
        WindowLocationRestore();
        ApplySettings();

        // Altrady tabblad verbergen, is enkel een browser om dat extra dialoog in externe browser te vermijden
        _webViewAltradyRef = webViewAltrady;
        tabControl.TabPages.Remove(tabPageAltrady);

        BinanceClient.SetDefaultOptions(new BinanceClientOptions() { });
        BinanceSocketClientOptions options = new();
        options.SpotStreamsOptions.AutoReconnect = true;
        options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
        BinanceSocketClient.SetDefaultOptions(options);


        GlobalData.InitializeGlobalData();
        GlobalData.InitializeIntervalList(); // De intervallen laden
        ResumeComputer(false);


        AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
        {
            GlobalData.AddTextToLogTab("Exiting!");
            ProgramExit = true;
        };

        SystemEvents.PowerModeChanged += OnPowerChange;


        InitEventView();
    }


    private void ApplySettings()
    {
        // fix duplicate storage of bool's (need migration)
        FrmSettings.FixStupidCheckboxes(GlobalData.Settings);


        comboBoxBarometerQuote.BeginUpdate();
        comboBoxBarometerInterval.BeginUpdate();
        try
        {
            comboBoxBarometerQuote.SelectedIndexChanged -= ShowBarometerStuff;
            comboBoxBarometerInterval.SelectedIndexChanged -= ShowBarometerStuff;

            // Enkel de actieve quotes erin zetten (default=busd)
            comboBoxBarometerQuote.Items.Clear();
            foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (cryptoQuoteData.FetchCandles)
                    comboBoxBarometerQuote.Items.Add(cryptoQuoteData.Name);
            }
            if (comboBoxBarometerQuote.Items.Count == 0)
                comboBoxBarometerQuote.Items.Add("BUSD");

            comboBoxBarometerQuote.SelectedIndex = comboBoxBarometerQuote.Items.IndexOf(GlobalData.Settings.General.SelectedBarometerQuote);
            //comboBoxBarometerQuote.Text = GlobalData.Settings.General.SelectedBarometerQuote;
            if (comboBoxBarometerQuote.SelectedIndex < 0)
                comboBoxBarometerQuote.SelectedIndex = 0;


            // De intervallen in de combox zetten (default=1h)
            comboBoxBarometerInterval.Items.Clear();
            comboBoxBarometerInterval.Items.Add("1H");
            comboBoxBarometerInterval.Items.Add("4H");
            comboBoxBarometerInterval.Items.Add("1D");

            comboBoxBarometerInterval.SelectedIndex = comboBoxBarometerInterval.Items.IndexOf(GlobalData.Settings.General.SelectedBarometerInterval);
            //comboBoxBarometerInterval.Text = GlobalData.Settings.General.SelectedBarometerInterval;
            if (comboBoxBarometerInterval.SelectedIndex < 0)
                comboBoxBarometerInterval.SelectedIndex = 0;
        }
        finally
        {
            comboBoxBarometerQuote.SelectedIndexChanged += ShowBarometerStuff;
            comboBoxBarometerInterval.SelectedIndexChanged += ShowBarometerStuff;

            comboBoxBarometerInterval.EndUpdate();
            comboBoxBarometerQuote.EndUpdate();
        }


        if ((GlobalData.Settings.General.FontSize != this.Font.Size) || (GlobalData.Settings.General.FontName.Equals(this.Font.Name)))
        {
            this.Font = new System.Drawing.Font(GlobalData.Settings.General.FontName, GlobalData.Settings.General.FontSize,
                System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            //this.applicationMenuStrip.Font.Size = GlobalData.Settings.General.FontSize;
        }

        ListboxSymbolsInitCaptions();
        ListViewSignalsInitCaptions();


        InitTimerInterval(ref TimerSoundHeartBeat, GlobalData.Settings.Signal.SoundHeartBeatMinutes); // x minutes
        TimerSoundHeartBeat.Tick += TimerSoundHeartBeat_Tick;

        // Restart data stream's every day
        InitTimerInterval(ref TimerRestartStreams, 24 * 60); // 24 hours
        TimerRestartStreams.Tick += TimerRestartStreams_Tick;

        // Check data stream's (om toch zeker te zijn van nieuwe candles)
        InitTimerInterval(ref TimerCheckDataStream, 5); // 5 minutes
        TimerCheckDataStream.Tick += TimerCheckDataStream_Tick;

        // Interval voor het ophalen van de exchange info (delisted coins) + bijwerken candles 
        InitTimerInterval(ref TimerGetCandles, GlobalData.Settings.General.GetCandleInterval);
        TimerGetCandles.Tick += TimerCandles_Tick;


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

        listViewSignals.GridLines = true;
        listViewSignals.View = View.Details;
        listViewSignals.FullRowSelect = true;
        listViewSignals.HideSelection = true;

        listViewSymbolPrices.GridLines = false;
        listViewSymbolPrices.View = View.Details;
        listViewSymbolPrices.FullRowSelect = true;
        listViewSymbolPrices.HideSelection = true;

#if !TRADEBOT
        ApplicationTradingBot.Visible = false;
        GlobalData.Settings.Bot.Active.Active = false;
#endif

        ApplicationTradingBot.Checked = GlobalData.Settings.Bot.Active;
        ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive;
        ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.SignalsActive;

        this.Refresh();
    }


    private static void InitTimerInterval(ref System.Windows.Forms.Timer timer, int minutes)
    {
        timer.Enabled = false;
        // stom ding verwacht > 0 (beetje vreemd, maar voila)
        if (minutes > 0)
            timer.Interval = minutes * 60 * 1000;
        timer.Enabled = minutes > 0;
    }


    private void OnPowerChange(object s, PowerModeChangedEventArgs e)
    {
        GlobalData.AddTextToLogTab("Debug: OnPowerChange");
        switch (e.Mode)
        {
            case PowerModes.Resume:
                GlobalData.AddTextToLogTab("PowerModes.Resume");
                ResumeComputer(true);
                break;
            case PowerModes.Suspend:
                GlobalData.AddTextToLogTab("PowerModes.Suspend");
                CloseCryptoScannerSession();
                break;
        }
    }


    private void ResumeComputer(bool sleepAwhile)
    {
        GlobalData.AddTextToLogTab("Debug: ResumeComputer");
        GlobalData.ApplicationStatus = ApplicationStatus.AppStatusPrepare;
        GlobalData.ThreadCreateSignal = new ThreadCreateSignal(BinanceShowNotification);
#if TRADEBOT
        GlobalData.TaskMonitorSignal = new ThreadMonitorSignal();
#endif

        // Iets met netwerk verbindingen wat nog niet "up" is?
        if (sleepAwhile)
            Thread.Sleep(5000);

        Task.Run(async () => { await ThreadLoadData.ExecuteAsync(); });
    }

    private void CloseCryptoScannerSession()
    {
        GlobalData.AddTextToLogTab("Debug: CloseCryptoScannerSession");
        GlobalData.ApplicationStatus = ApplicationStatus.AppStatusExiting;

        // De socket streams
        GlobalData.TaskBinanceStreamPriceTicker?.StopAsync();

        // Threads (of tasks)
        GlobalData.ThreadCreateSignal?.Stop();
#if TRADEBOT
        GlobalData.TaskMonitorSignal?.Stop();
#endif

        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            foreach (BinanceStream1mCandles binanceStream1mCandles in quoteData.BinanceStream1mCandles)
            {
                binanceStream1mCandles?.StopAsync();
            }
            quoteData.BinanceStream1mCandles.Clear();
        }

        WindowLocationSave();
        GlobalData.Settings.General.SelectedBarometerQuote = comboBoxBarometerQuote.Text;
        GlobalData.Settings.General.SelectedBarometerInterval = comboBoxBarometerInterval.Text;
        GlobalData.SaveSettings();

        DataStore.SaveCandles();
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseCryptoScannerSession();

            if (components != null)
            {
                _webViewAltradyRef.Dispose();
                components.Dispose();
            }
        }

        base.Dispose(disposing);
    }


    private void PlaySound(string text, bool test = false)
    {
        if ((!ProgramExit) && (IsHandleCreated))
        {
            if (GlobalData.Settings.Signal.SoundsActive)
                ThreadSoundPlayer.AddToQueue(text);
        }
    }

    private void PlaySpeech(string text, bool test = false)
    {
        if ((!ProgramExit) && (IsHandleCreated))
        {
            if (GlobalData.Settings.Signal.SoundsActive || test)
                ThreadSpeechPlayer.AddToQueue(text);
        }
    }

    private void AddTextToTelegram(string text, bool extraLineFeed = false)
    {
        if ((!ProgramExit) && (IsHandleCreated))
        {
            //return; //t'ding crasht en is niet fijn
            //ThreadTelegramBot.SendMessage(text);
        }
    }

    private void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        if ((components != null) && (!ProgramExit) && (IsHandleCreated))
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
        }
    }



    private void ToolStripMenuItemRefresh_Click_1(object sender, EventArgs e)
    {
        Task.Run(async () => { await BinanceFetchCandles.ExecuteAsync(); }); // niet wachten tot deze klaar is
    }



    private void CreateBarometerBitmap(CryptoQuoteData quoteData, CryptoInterval interval)
    {
        float blocks = 6;

        // pixel dimensies van het plaatje
        int intWidth = pictureBox1.Width;
        int intHeight = pictureBox1.Height;

        if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
        {
            if ((quoteData != null) && (exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice + quoteData.Name, out CryptoSymbol symbol)))
            {
                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                SortedList<long, CryptoCandle> candleList = symbolPeriod.CandleList;

                // determine range of data
                long loX = long.MaxValue;
                long hiX = long.MinValue;
                float loY = float.MaxValue;
                float hiY = float.MinValue;
                int candleCount = (int)(blocks * 60);
                for (int i = candleList.Values.Count - 1; i >= 0; i--)
                {
                    CryptoCandle candle = candleList.Values[i];
                    if (loX > candle.OpenTime)
                        loX = candle.OpenTime;
                    if (hiX < candle.OpenTime)
                        hiX = candle.OpenTime;

                    if (loY > (float)candle.Close)
                        loY = (float)candle.Close;
                    if (hiY < (float)candle.Close)
                        hiY = (float)candle.Close;
                    if (candleCount-- < 0)
                        break;
                }
                if (loX == long.MaxValue)
                    return;


                // ranges x and y
                float screenX = hiX - loX; // unix time
                float screenY = hiY - loY; // barometer, something like -5 .. +5
                if (screenY < 5)
                    screenY = 5f; // from -2 to +2
                if (hiY > 0.5 * screenY)
                    screenY = +2 * hiY;
                if (loY < -0.5 * screenY)
                    screenY = -2 * loY;



                // factor to keep points within picture
                float scaleX = intWidth / screenX;
                float scaleY = intHeight / screenY;

                // ofset to first point
                float offsetX = 0; // start in the left of the picture
                float offsetY = scaleY * 0.5f * screenY; // center of picture

                // flix y (specific for winform - what a crap)
                scaleY = -1 * scaleY;

                Image bmp = new Bitmap(intWidth, intHeight);
                Graphics g = Graphics.FromImage(bmp);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // horizontal lines (1% per line)
                for (int y = -3; y <= 3; y++)
                {
                    PointF p1 = new(0, offsetY + scaleY * y);
                    PointF p2 = new(intWidth, offsetY + scaleY * y);
                    if (y == 0)
                        g.DrawLine(Pens.Red, p1, p2);
                    else
                        g.DrawLine(Pens.Gray, p1, p2);
                }

                // vertical lines (show hours)
                //Pen pen = new Pen(Color.Gray, 0.5F);
                long intervalTime = 60 * 60;
                long lastX = hiX - (hiX % intervalTime);
                while (lastX > loX)
                {
                    //DateTime ehh = CandleTools.GetUnixDate(lastX);
                    //GlobalData.AddTextToLogTab(ehh.ToLocalTime() + " " + lastX.ToString() + " intervaltime=" + intervalTime.ToString());

                    PointF p1 = new(0, 0)
                    {
                        X = offsetX + scaleX * (float)(lastX - loX)
                    };
                    PointF p2 = new(0, intHeight)
                    {
                        X = offsetX + scaleX * (float)(lastX - loX)
                    };
                    g.DrawLine(Pens.Gray, p1, p2);
                    lastX -= intervalTime;
                }


                bool init = false;
                PointF point1 = new(0, 0);
                PointF point2 = new(0, 0);
                candleCount = (int)(blocks * 60);
                for (int i = candleList.Values.Count - 1; i >= 0; i--)
                {
                    CryptoCandle candle = candleList.Values[i];

                    point2.X = offsetX + scaleX * (float)(candle.OpenTime - loX);
                    point2.Y = offsetY + scaleY * ((float)candle.Close);
                    //GlobalData.AddTextToLogTab(candle.OhlcText(symbol.DisplayFormat) + " " + point2.X.ToString("N8") + " " + point2.Y.ToString("N8"));

                    if (init)
                    {
                        if (candle.Close < 0)
                            g.DrawLine(Pens.Red, point1, point2);
                        else
                            g.DrawLine(Pens.DarkGreen, point1, point2);
                    }

                    point1 = point2;
                    init = true;
                    if (candleCount-- < 0)
                        break;
                }


                // Maak de linkerkant ff grijs en zet het stuff erover heen
                {
                    // hoe breed?
                    Rectangle rect = new(0, 0, 55, intHeight);
                    SolidBrush solidBrush = new(panelTop.BackColor);
                    g.FillRectangle(solidBrush, rect);
                }

                // Barometer met 3 cirkels zoals Altrady
                {
                    int y = 1;
                    int offset = 4;
                    int offsetValue = 20;
                    //Pen blackPen = new Pen(Color.Black, 1);
                    Rectangle rect1 = new(0, 0, intWidth, intHeight);
                    Font drawFont1 = new("Microsoft Sans Serif", this.Font.Size);
                    CryptoIntervalPeriod[] list = { CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1d };

                    foreach (CryptoIntervalPeriod intervalPeriod in list)
                    {
                        Color color;
                        BarometerData barometerData = quoteData.BarometerList[(int)intervalPeriod];
                        if (barometerData?.PriceBarometer < 0)
                            color = Color.Red;
                        else
                            color = Color.Green;

                        //TextRenderer.DrawText(g, "1h", drawFont1, rect1, Color.Black, Color.Transparent, TextFormatFlags.Top);
                        SolidBrush solidBrush = new(color);
                        g.FillEllipse(solidBrush, offset, y, 14, 14);
                        //g.DrawEllipse(blackPen, offset, y, 14, 14);
                        rect1 = new Rectangle(offsetValue, y, intWidth, intHeight);
                        TextRenderer.DrawText(g, barometerData?.PriceBarometer?.ToString("N2"), drawFont1, rect1, color, Color.Transparent, TextFormatFlags.Top);
                        y += 19;
                    }

                }

                // Barometer tijd
                if (candleList.Values.Count > 0)
                {
                    CryptoCandle candle = candleList.Values[candleList.Values.Count - 1];
                    Rectangle rect = new(6, 0, intWidth, intHeight - 8);
                    string text = CandleTools.GetUnixDate((long)candle.OpenTime + 60).ToLocalTime().ToString("HH:mm");

                    Font drawFont = new("Microsoft Sans Serif", this.Font.Size);
                    TextRenderer.DrawText(g, text, drawFont, rect, Color.Black, Color.Transparent, TextFormatFlags.Bottom);

                }



                //bmp.Save(@"e:\test.bmp");
                Invoke((MethodInvoker)(() => { pictureBox1.Image = bmp; pictureBox1.Refresh(); }));
            }
            else
            {

                Image bmp = new Bitmap(intWidth, intHeight);
                Graphics g = Graphics.FromImage(bmp);

                Invoke((MethodInvoker)(() => { pictureBox1.Image = bmp; pictureBox1.Refresh(); }));
            }
        }
    }



    private void BinanceBarometerAll()
    {
        try
        {
            if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusPrepare)
                return;

            // Bereken de laatste barometer waarden
            BarometerTools barometerTools = new();
            barometerTools.Execute();

            if (!GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
            {
                return;
            }

            {
                string baseCoin = "";
                Invoke((MethodInvoker)(() => baseCoin = comboBoxBarometerQuote.Text));
                if (!GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out CryptoQuoteData quoteData))
                {
                    return;
                }

                // Het grafiek gedeelte
                // Toon de waarden van de geselecteerde basismunt
                int baseIndex = 0;
                CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval1h;
                Invoke((MethodInvoker)(() => baseIndex = comboBoxBarometerInterval.SelectedIndex));
                if (baseIndex == 0)
                    intervalPeriod = CryptoIntervalPeriod.interval1h;
                else if (baseIndex == 1)
                    intervalPeriod = CryptoIntervalPeriod.interval4h;
                else if (baseIndex == 2)
                    intervalPeriod = CryptoIntervalPeriod.interval1d;
                else
                {
                    return;
                }
                if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
                {
                    return;
                }

                BarometerData barometerData = quoteData.BarometerList[(int)intervalPeriod];
                CreateBarometerBitmap(quoteData, interval);
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab(error.ToString() + "\r\n");
        }
    }

    private void ShowBarometerStuff(object sender, EventArgs e)
    {
        // Dan wordt de basecoin bewaard voor een volgende keer
        GlobalData.Settings.General.SelectedBarometerQuote = comboBoxBarometerQuote.Text;
        GlobalData.Settings.General.SelectedBarometerInterval = comboBoxBarometerInterval.Text;
        new Thread(BinanceBarometerAll).Start();
    }

    static int barometerLastMinute = 0;

    private class SymbolHist
    {
        public string Symbol = "";
        public decimal? PricePrev = 0m;
        public decimal VolumePrev = 0m;
    }
    private readonly List<SymbolHist> symbolHistList = new(3);


    private static void ShowSymbolPrice(SymbolHist hist, ListViewItem item, Model.CryptoExchange exchange,
        CryptoQuoteData quoteData, string baseCoin, TradingView.SymbolValue tvValues, string caption, string valueText)
    {
        // Not a really charming way to display items, but voila, it works for now..
        ListViewItem.ListViewSubItem subItem;


        // subitem 0, 1 en 2
        if (exchange.SymbolListName.TryGetValue(baseCoin + quoteData.Name, out CryptoSymbol symbol) || exchange.SymbolListName.TryGetValue(baseCoin + "USDT", out symbol))
        {
            decimal value;
            subItem = item.SubItems[0];
            subItem.Text = symbol.Name;


            subItem = item.SubItems[1];
            if (symbol.LastPrice.HasValue)
            {
                value = (decimal)symbol.LastPrice;
                subItem.Text = value.ToString(symbol.DisplayFormat);
                subItem.ForeColor = Color.Black;

                if (symbol.Name.Equals(hist.Symbol) && hist.PricePrev.HasValue)
                {
                    if (value < hist.PricePrev)
                        subItem.ForeColor = Color.Red;
                    else if (value > hist.PricePrev)
                        subItem.ForeColor = Color.Green;
                }
            }
            else
                subItem.Text = "";


            subItem = item.SubItems[2];
            value = (decimal)symbol.Volume;
            subItem.Text = value.ToString("N0");
            subItem.ForeColor = Color.Black;
            if (symbol.Name.Equals(hist.Symbol))
            {
                if (value < hist.VolumePrev)
                    subItem.ForeColor = Color.Red;
                else if (value > hist.VolumePrev)
                    subItem.ForeColor = Color.Green;
            }
            else
                subItem.Text = "";

            hist.Symbol = symbol.Name;
            hist.PricePrev = symbol.LastPrice;
            hist.VolumePrev = (decimal)symbol.Volume;
        }
        else
        {
            item.SubItems[0].Text = "";
            item.SubItems[1].Text = "";
            item.SubItems[2].Text = "";

            hist.Symbol = "";
            hist.PricePrev = 0m;
            hist.VolumePrev = 0m;
        }



        // subitem 4 en 5
        if (tvValues != null)
        {
            subItem = item.SubItems[4];
            subItem.Text = tvValues.Name;
            subItem.Tag = tvValues;

            decimal value = tvValues.Lp;
            subItem = item.SubItems[5];
            subItem.Text = value.ToString(tvValues.DisplayFormat);
            if (value < tvValues.LastValue)
                subItem.ForeColor = Color.Red;
            else if (value > tvValues.LastValue)
                subItem.ForeColor = Color.Green;
            tvValues.LastValue = value;
        }
        else
        {
            item.SubItems[4].Text = "";
            item.SubItems[5].Text = "";
        }


        // subitem 7 en 8
        item.SubItems[7].Text = caption;
        item.SubItems[8].Text = valueText;

    }


    private void ConnectionWasLostEvent(string text, bool extraLineFeed = false)
    {
        // Plan een verversing omdat er een connection timeout was.
        // Dit kan een aantal berekeningen onderbroken hebben
        // (er komen een aantal reconnects, daarom circa 20 seconden)
        if ((components != null) && (!ProgramExit) && (IsHandleCreated))
        {
            // anders krijgen we alleen maar fouten dat er geen candles zijn
            GlobalData.ApplicationStatus = ApplicationStatus.AppStatusPrepare;
        }
    }


    private void ConnectionWasRestoredEvent(string text, bool extraLineFeed = false)
    {
        if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
        {
            // Plan een verversing omdat er een connection timeout was.
            // Dit kan een aantal berekeningen onderbroken hebben
            // (er komen een aantal reconnects, daarom circa 20 seconden)
            if ((components != null) && (!ProgramExit) && (IsHandleCreated))
            {
                Invoke((MethodInvoker)(() => TimerGetCandles.Enabled = false));
                Invoke((MethodInvoker)(() => TimerGetCandles.Interval = 20 * 1000));
                Invoke((MethodInvoker)(() => TimerGetCandles.Enabled = true));
            }
        }
    }


    private void SetCandleTimerEnableHandler(bool value)
    {
        if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
        {
            if ((components != null) && (!ProgramExit) && (IsHandleCreated))
            {
                if (InvokeRequired)
                    Invoke((MethodInvoker)(() => TimerGetCandles.Enabled = value));
                else
                    TimerGetCandles.Enabled = value;
            }
        }
    }


    private void ListViewSymbolPrices_DoubleClick(object sender, EventArgs e)
    {
        if (listViewSymbolPrices.SelectedItems.Count > 0)
        {
            Point mousePos = listViewSymbolPrices.PointToClient(Control.MousePosition);
            ListViewHitTestInfo hitTest = listViewSymbolPrices.HitTest(mousePos);
            ListViewItem item = hitTest.Item;
            if (item == null)
                return;

            int col = hitTest.Item.SubItems.IndexOf(hitTest.SubItem);

            if (col < 4)
            {
                if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
                {
                    if (exchange.SymbolListName.TryGetValue(item.Text, out CryptoSymbol symbol))
                    {
                        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
                            return;

                        ActivateTradingApp(symbol, interval);
                        return;
                    }
                }
            }
            else if (col < 7)
            {
                ListViewItem.ListViewSubItem subItem = item.SubItems[4];
                TradingView.SymbolValue tvValues = (TradingView.SymbolValue)subItem.Tag;
                if (tvValues == null)
                    return;

                if (tvValues.Url != "")
                {
                    string href = tvValues.Url;
                    Uri uri = new(href);
                    webViewTradingView.Source = uri;
                    tabControl.SelectedTab = tabPageBrowser;
                }
                else
                {
                    string href = string.Format("https://www.tradingview.com/chart/?symbol={0}&interval=60", tvValues.Ticker);
                    Uri uri = new(href);
                    webViewTradingView.Source = uri;
                    tabControl.SelectedTab = tabPageBrowser;
                }
            }
            else tabControl.SelectedTab = tabPageSignals;
        }
    }

    private async void TimerBarometer_Tick(object sender, EventArgs e)
    {

        if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
        {
            // De prijzen van candles zijn pas na 20 a 30 seconden na iedere minuut bekend
            // (er zit een vertraging het verkrijgen en verwerken van de candles)
            // 25 seconden is vrij vrij ruim genomen, zou redelijk goed moeten zijn.
            // Een andere methode zou zijn om het aantal symbols in de barometer te tellen 
            // en indien voldoende deze als valide te beschouwen (ook een lastige aanpak)

            // Dus we willen deze alleen uitrekenen indien het +/- 30 seconden is
            // Voorkom dat het te vaak loopt (kost redelijk wat CPU i.c.m. volume)

            if ((DateTime.Now.Second > 10) && (DateTime.Now.Minute != barometerLastMinute))
            {
                barometerLastMinute = DateTime.Now.Minute;
                new Thread(BinanceBarometerAll).Start();
            }
            //UpdateInfoAndbarometerValues();


            //// De Fear and Greed index (elke 24 uur een nieuwe waarde)
            ///{
            //           "name": "Fear and Greed Index",
            //"data": [

            //    {
            //      "value": "53",
            //		"value_classification": "Neutral",
            //		"timestamp": "1674345600",
            //		"time_until_update": "29260"

            //    }
            //    ],
            //    "metadata": {
            //    "error": null

            //    }
            //}
            // TODO: Nog even netjes verstoppen in een aparte thread/task ipv het hier op te halen
            try
            {
                if (GlobalData.FearAndGreedIndex.LastCheck == null || DateTime.UtcNow >= GlobalData.FearAndGreedIndex.LastCheck)
                {
                    var jsonData = await httpClient.GetFromJsonAsync<FGIndex>("https://api.alternative.me/fng/");
                    string value = jsonData.Data[0].Value;
                    //FearAndGreedIndex = jsonData["data"][0]["value"].Value<string>();
                    GlobalData.FearAndGreedIndex.Lp = decimal.Parse(value);
                    GlobalData.FearAndGreedIndex.LastCheck = DateTime.UtcNow.AddHours(1); // = Next check
                }
            }
            catch
            {
                //FearAndGreedIndex = "Connection-Error"; // jammer..
                //GlobalData.FearAndGreedIndex.LastValue = decimal.Parse(FearAndGreedIndex);
            }



            // Toon de prijzen en volume van een aantal symbols
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
            {
                listViewSymbolPrices.BeginUpdate();
                try
                {
                    if (listViewSymbolPrices.Columns.Count == 0)
                    {
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Left); // Symbol
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Right); // Price
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Right); // Volume
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Left); // Space

                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Left); // Market
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Right); // Value
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Left); // Space

                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Left); // Caption
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Right); // Count
                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Left); // Space

                        listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Right); // filler

                        listViewSymbolPrices.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

                        // exact 4 items toevoegen
                        for (int i = 0; i <= 4; i++)
                        {
                            ListViewItem item1 = new("", -1)
                            {
                                UseItemStyleForSubItems = false
                            };
                            // Coins
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");

                            // Extra info
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");

                            // Binance en F&G
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");
                            item1.SubItems.Add("");
                            listViewSymbolPrices.Items.Add(item1);
                            symbolHistList.Add(new SymbolHist());
                        }
                    }


                    // Tel het aantal ontvangen 1m candles (via alle uitstaande streams)
                    // Elke minuut komt er van elke munt een candle (indien er gehandeld is).
                    int candlesKLineCount = 0;
                    foreach (CryptoQuoteData quoteData1 in GlobalData.Settings.QuoteCoins.Values)
                    {
                        for (int i = 0; i < quoteData1.BinanceStream1mCandles.Count; i++)
                        {
                            BinanceStream1mCandles binanceStream1mCandles = quoteData1.BinanceStream1mCandles[i];
                            candlesKLineCount += binanceStream1mCandles.CandlesKLinesCount;
                        }
                    }

                    string baseCoin = "";
                    Invoke((MethodInvoker)(() => baseCoin = comboBoxBarometerQuote.Text));
                    if (GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out CryptoQuoteData quoteData))
                    {
                        string text = GlobalData.TaskBinanceStreamPriceTicker?.tickerCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[0], listViewSymbolPrices.Items[0], exchange, quoteData, "BTC", GlobalData.TradingViewDollarIndex, "Binance price ticker count", text);

                        text = candlesKLineCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[1], listViewSymbolPrices.Items[1], exchange, quoteData, "BNB", GlobalData.TradingViewBitcoinDominance, "Binance 1m stream count", text);

                        text = GlobalData.ThreadCreateSignal?.analyseCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[2], listViewSymbolPrices.Items[2], exchange, quoteData, "ETH", GlobalData.TradingViewSpx500, "Scanner analyse count", text);

                        text = createdSignalCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[3], listViewSymbolPrices.Items[3], exchange, quoteData, "XRP", GlobalData.TradingViewMarketCapTotal, "Scanner signal count", text);

                        ShowSymbolPrice(symbolHistList[4], listViewSymbolPrices.Items[4], exchange, quoteData, "ADA", GlobalData.FearAndGreedIndex, "", "");
                    }

                    for (int i = 0; i <= listViewSymbolPrices.Columns.Count - 1; i++)
                    {
                        if (i == 3 || i == 6 || i == 9)
                            listViewSymbolPrices.Columns[i].Width = 25;
                        else
                            listViewSymbolPrices.Columns[i].Width = -2;
                    }
                }
                finally
                {
                    listViewSymbolPrices.EndUpdate();
                }
            }
        }
    }


    private void ActivateTradingApp(CryptoSymbol symbol, CryptoInterval interval)
    {
        string href;
        switch (GlobalData.Settings.General.TradingApp)
        {
            case TradingApp.Altrady:
                href = Altrady.GetRef(symbol, interval);
                // Een poging om de externe browser te vermijden
                Uri uri = new(href);
                _webViewAltradyRef.Source = uri;
                //System.Diagnostics.Process.Start(href);
                break;
            case TradingApp.AltradyWeb:
                href = Altrady.GetRef(symbol, interval);
                System.Diagnostics.Process.Start(href);
                break;
            case TradingApp.Hypertrader:
                href = HyperTrader.GetRef(symbol, interval);
                System.Diagnostics.Process.Start(href);
                break;
        }
    }


    private void SymbolFilter_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SymbolsHaveChangedEvent("");
        }
    }


    private void ToolStripMenuItemSettings_Click(object sender, EventArgs e)
    {
        // Dan wordt de basecoin en coordinaten etc. bewaard voor een volgende keer
        WindowLocationSave();
        GlobalData.Settings.Bot.Active = ApplicationTradingBot.Checked;
        GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
        GlobalData.Settings.Signal.SignalsActive = ApplicationCreateSignals.Checked;
        GlobalData.Settings.General.SelectedBarometerQuote = comboBoxBarometerQuote.Text;
        GlobalData.Settings.General.SelectedBarometerInterval = comboBoxBarometerInterval.Text;
        try
        {
            FrmSettings form = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            ChangeTheme(theme, form);
            form.InitSettings(GlobalData.Settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                GlobalData.SaveSettings();
                ApplySettings();
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }
    }

    private class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }
    }

    private void MainMenuClearAll_Click(object sender, EventArgs e)
    {
        //this.Text = "";
        TextBoxLog.Clear();
        createdSignalCount = 0;
        ListViewSignalsMenuItemClearSignals_Click(null, null);

        GlobalData.ThreadCreateSignal.analyseCount = 0;
        GlobalData.TaskBinanceStreamPriceTicker.tickerCount = 0;

        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            for (int i = 0; i < quoteData.BinanceStream1mCandles.Count; i++)
            {
                BinanceStream1mCandles binanceStream1mCandles = quoteData.BinanceStream1mCandles[i];
                binanceStream1mCandles.CandlesKLinesCount = 0;
            }
        }
    }


    private void InitEventView()
    {
        // Allow the user to edit item text.
        listViewSignals.LabelEdit = false;
        // Allow the user to rearrange columns.
        listViewSignals.AllowColumnReorder = false;
        // Display check boxes.
        listViewSignals.CheckBoxes = false;
        // Select the item and subitems when selection is made.
        listViewSignals.FullRowSelect = true;
        // Display grid lines.
        listViewSignals.GridLines = true;
        // Sort the items in the list in ascending order.
        //listView1.Sorting = SortOrder.None;
        // Hippe blauwe underline (bah)
        //listView1.HotTracking = true; // verstoord de kleuren en is onrustig
        // Specify that each item appears on a separate line.
        listViewSignals.View = View.Details; // Voor display van de subitems

        ListViewSignalsInitColumns();

        InitTimerInterval(ref timerClearEvents, 1);
        timerClearEvents.Tick += new System.EventHandler(this.TimerClearOldSignals_Tick);
    }



    private static void PlaySound(CryptoSignal signal, bool playSound, bool playSpeech, string soundName, ref long lastSound)
    {
        // Reduce the amount of sounds/speech
        if (signal.EventTime > lastSound && signal.Mode != SignalMode.modeInfo2)
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


    //testen!
    private readonly Queue<string> logQueue = new();
    private readonly Queue<CryptoSignal> signalQueue = new();


    private void TimerAddSignal_Tick(object sender, EventArgs e)
    {
        // Speed up adding signals
        if (signalQueue.Count > 0)
        {
            Monitor.Enter(signalQueue);
            try
            {
                List<CryptoSignal> signals = new();

                while (signalQueue.Count > 0)
                {
                    CryptoSignal signal = signalQueue.Dequeue();
                    if (signal != null)
                        signals.Add(signal);
                }

                // verwerken..
                Task.Factory.StartNew(() =>
                {
                    Invoke(new Action(() =>
                    {
                        ListViewSignalsAddSignalRange(signals);
                    }));
                });
            }
            finally
            {
                Monitor.Exit(signalQueue);
            }
        }


        // Speed up adding text
        if (logQueue.Count > 0)
        {
            Monitor.Enter(logQueue);
            try
            {
                List<CryptoSignal> signals = new();

                StringBuilder stringBuilder = new();

                while (logQueue.Count > 0)
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
                        if (InvokeRequired)
                            Invoke((MethodInvoker)(() => TextBoxLog.AppendText(text)));
                        else
                            TextBoxLog.AppendText(text);
                    }));
                });
            }
            finally
            {
                Monitor.Exit(logQueue);
            }
        }
    }

    private void BinanceShowNotification(CryptoSignal signal)
    {
        createdSignalCount++;
        string text = "signal: " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.ModeText + " " + signal.StrategyText + " " + signal.EventText;
        GlobalData.AddTextToLogTab(text);

        if (signal.BackTest)
            return;

        // Zet de laatste munt in de "caption" (en taskbar) van de applicatie bar (visuele controle of er meldingen zijn)
        //Invoke(new Action(() => { this.Text = signal.Symbol.Name + " " + createdSignalCount.ToString(); }));


        //Monitor.Enter(signalQueue);
        try
        {
            signalQueue.Enqueue(signal);
        }
        finally
        {
            //Monitor.Exit(signalQueue);
        }
        //Task.Factory.StartNew(() =>
        //{
        //    Invoke(new Action(() =>
        //    {
        //        listViewSignalsAddSignal(signal);
        //    }));
        //});



        // Speech and/or sound
        if (signal.Mode != SignalMode.modeInfo2) // disqualifield signals
        {
            switch (signal.Strategy)
            {
                case SignalStrategy.candlesJumpUp:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundCandleJumpSignal, GlobalData.Settings.Signal.PlaySpeechCandleJumpSignal,
                        GlobalData.Settings.Signal.SoundCandleJumpUp, ref LastSignalSoundCandleJumpUp);
                    break;
                case SignalStrategy.candlesJumpDown:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundCandleJumpSignal, GlobalData.Settings.Signal.PlaySpeechCandleJumpSignal,
                        GlobalData.Settings.Signal.SoundCandleJumpDown, ref LastSignalSoundCandleJumpDown);
                    break;

                case SignalStrategy.stobbOversold:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundStobbSignal, GlobalData.Settings.Signal.PlaySpeechStobbSignal,
                        GlobalData.Settings.Signal.SoundStobbOversold, ref LastSignalSoundStobbOversold);
                    break;
                case SignalStrategy.stobbOverbought:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundStobbSignal, GlobalData.Settings.Signal.PlaySpeechStobbSignal,
                        GlobalData.Settings.Signal.SoundStobbOverbought, ref LastSignalSoundStobbOverbought);
                    break;

                case SignalStrategy.sbm1Oversold:
                case SignalStrategy.sbm2Oversold:
                case SignalStrategy.sbm3Oversold:
                case SignalStrategy.sbm4Oversold:
                case SignalStrategy.sbm5Oversold:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundSbmSignal, GlobalData.Settings.Signal.PlaySpeechSbmSignal,
                        GlobalData.Settings.Signal.SoundSbmOversold, ref LastSignalSoundSbmOversold);
                    break;

                case SignalStrategy.sbm1Overbought:
                case SignalStrategy.sbm2Overbought:
                case SignalStrategy.sbm3Overbought:
                case SignalStrategy.sbm4Overbought:
                case SignalStrategy.sbm5Overbought:

                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundSbmSignal, GlobalData.Settings.Signal.PlaySpeechSbmSignal,
                        GlobalData.Settings.Signal.SoundSbmOverbought, ref LastSignalSoundSbmOverbought);
                    break;
            }
        }
    }



    private async Task ActivateTradingViewBrowser(string symbolname = "BTCBUSD")
    {
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval interval))
            return;

        if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolname, out CryptoSymbol symbol))
            {
                // https://stackoverflow.com/questions/63404822/how-to-disable-cors-in-wpf-webview2
                var userPath = GlobalData.GetBaseDir();
                var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userPath);
                await webViewTradingView.EnsureCoreWebView2Async(webView2Environment);

                var href = Intern.TradingView.GetRef(symbol, interval);
                Uri uri = new(href);
                webViewTradingView.Source = uri;
            }
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

    private void TimerCandles_Tick(object sender, EventArgs e)
    {
        // Ophalen van exchange info en candles bijwerken
        if (GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
        {
            // De reguliere verversing herstellen (igv een connection timeout)
            if ((components != null) && (!ProgramExit) && (IsHandleCreated))
            {
                // Plan een volgende verversing omdat er bv een connection timeout was.
                // Dit kan een aantal berekeningen onderbroken hebben
                Invoke((MethodInvoker)(() => InitTimerInterval(ref TimerGetCandles, GlobalData.Settings.General.GetCandleInterval)));
            }

            Task.Run(async () => { await BinanceFetchCandles.ExecuteAsync(); }); // niet wachten tot deze klaar is
        }
        else
            Invoke((MethodInvoker)(() => TimerGetCandles.Enabled = false));
    }



    public void WindowLocationSave()
    {
        GlobalData.Settings.General.WindowPosition = DesktopBounds;

        // only save the WindowState if Normal or Maximized
        GlobalData.Settings.General.WindowState = WindowState switch
        {
            FormWindowState.Normal or FormWindowState.Maximized => WindowState,
            _ => FormWindowState.Normal,
        };
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
        GlobalData.Settings.Bot.Active = ApplicationTradingBot.Checked;
        GlobalData.SaveSettings();
    }


    private void BacktestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            AskSymbolDialog form = new()
            {
                StartPosition = FormStartPosition.CenterParent
            };
            if (form.ShowDialog() == DialogResult.OK)
            {
                GlobalData.SaveSettings();

                if (!GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
                {
                    MessageBox.Show("Exchange bestaat niet");
                    return;
                }

                // Bestaat de coin? (uiteraard, net geladen)
                if (!exchange.SymbolListName.TryGetValue(GlobalData.Settings.BackTestSymbol, out CryptoSymbol symbol))
                {
                    MessageBox.Show("Symbol bestaat niet");
                    return;
                }

                CryptoInterval interval = null;
                //CryptoInterval interval = GlobalData.IntervalList => (Name == GlobalData.Settings.BackTestInterval); ???
                foreach (CryptoInterval intervalX in GlobalData.IntervalList)
                {
                    if (intervalX.Name == GlobalData.Settings.BackTestInterval)
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

                long unix = CandleTools.GetUnixTime(GlobalData.Settings.BackTestTime, interval.Duration);
                if (!symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.TryGetValue(unix, out CryptoCandle candle))
                {
                    MessageBox.Show("Candle bestaat niet");
                    return;
                }

                long einde = candle.OpenTime;
                long start = einde - 2 * 60 * interval.Duration;
                SignalCreate createSignal = new(symbol, interval, true, BinanceShowNotification);
                while (start <= einde)
                {
                    if (symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.TryGetValue(start, out candle))
                    {
                        if (createSignal.Prepare(start))
                        {
                            SignalBase algorithm = SignalHelper.GetSignalAlgorithm(GlobalData.Settings.BackTestAlgoritm, symbol, interval, candle);
                            if (algorithm != null)
                            {
                                if (algorithm.IndicatorsOkay(candle) && algorithm.IsSignal())
                                {
                                    //createSignal.PrepareAndSendSignal(algorithm);
                                    algorithm.ExtraText = "Signal!";
                                }
                                candle.ExtraText = algorithm.ExtraText;
                            }
                        }
                    }
                    start += interval.Duration;
                }

                //SignalBase algorithm = createSignal.AnalyseSymbolUsingStrategy(start, SignalStrategy.sbm1Oversold);
                createSignal.ExportToExcell(GlobalData.Settings.BackTestAlgoritm);
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }

    }

    private void TimerRestartStreams_Tick(object sender, EventArgs e)
    {
        TimerRestartStreams.Enabled = false;
        try
        {
            GlobalData.AddTextToLogTab("Restart data streams");

            CloseCryptoScannerSession();
            ResumeComputer(true);
        }
        finally
        {
            InitTimerInterval(ref TimerRestartStreams, 4 * 60); // reset interval (back to 4h)
        }
    }

    int lastCandlesKLineCount = 0;

    private void TimerCheckDataStream_Tick(object sender, EventArgs e)
    {
        int candlesKLineCount = 0;
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            for (int i = 0; i < quoteData.BinanceStream1mCandles.Count; i++)
            {
                BinanceStream1mCandles binanceStream1mCandles = quoteData.BinanceStream1mCandles[i];
                candlesKLineCount += binanceStream1mCandles.CandlesKLinesCount;
            }
        }

        if (lastCandlesKLineCount != 0 && candlesKLineCount == lastCandlesKLineCount)
        {
            GlobalData.AddTextToLogTab("Debug: De 1m data stream is gestopt!");

            // Schedule a rest of the streams
            InitTimerInterval(ref TimerRestartStreams, 1);
        }
        lastCandlesKLineCount = candlesKLineCount;
    }
}
