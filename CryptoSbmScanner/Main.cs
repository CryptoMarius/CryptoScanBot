using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Binance.Net.Objects;
using Binance.Net.Clients;
using Microsoft.Win32;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.Drawing.Drawing2D;
using TradingView;
using Microsoft.Web.WebView2.Core;
using Google.Protobuf.WellKnownTypes;
using System.Text;
using System.IO;

namespace CryptoSbmScanner
{

    public partial class FrmMain : Form //MetroFramework.Forms.MetroForm //Form //MaterialForm //
    {
        private bool ProgramExit; // = false; //Mislukte manier om excepties bij afsluiten te voorkomen (todo)
        private int createdSignalCount; // = 0; // Tellertje met het aantal meldingen (komt in de taakbalk c.q. applicatie titel)
        private Microsoft.Web.WebView2.WinForms.WebView2 _webViewAltradyRef = null;
        private ColorSchemeTest theme = new ColorSchemeTest();

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
            labelVersion.Text = "Version " + appVersion;
            this.Text = appName;


            // Om vanuit achtergrond threads iets te kunnen loggen (kan charmanter?) 
            GlobalData.PlaySound += new PlayMediaEvent(PlaySound);
            GlobalData.PlaySpeech += new PlayMediaEvent(PlaySpeech);
            GlobalData.LogToTelegram += new AddTextEvent(AddTextToTelegram);
            GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);
            GlobalData.SetCandleTimerEnableEvent += new SetCandleTimerEnable(SetCandleTimerEnableHandler);

            // Niet echt een text event, meer misbruik van dit event type (en luiigheid)
            GlobalData.SymbolsHaveChangedEvent += new AddTextEvent(SymbolsHaveChangedEvent);
            GlobalData.ConnectionWasLostEvent += new AddTextEvent(ConnectionWasLostEvent);
            GlobalData.ConnectionWasRestoredEvent += new AddTextEvent(ConnectionWasRestoredEvent);


            // Wat event handles zetten
            comboBoxBarometerQuote.SelectedIndexChanged += ShowBarometerStuff;
            comboBoxBarometerInterval.SelectedIndexChanged += ShowBarometerStuff;
            // suffe Visual Studio houdt er niet van als jet het opsplitst in 3 partial forms (maakt het opnieuw aan)
            listBoxSymbols.DoubleClick += new System.EventHandler(this.listBoxSymbols_DoubleClick);
            listViewSignals.DoubleClick += new System.EventHandler(this.listViewSignalsMenuItem_DoubleClick);
            listViewSymbolPrices.DoubleClick += new System.EventHandler(this.listViewSymbolPrices_DoubleClick);


            // Instelling laden waaronder de API enzovoort
            GlobalData.LoadSettings();
            WindowLocationRestore();
            ApplySettings();

            _webViewAltradyRef = webViewAltrady;
            tabControl.TabPages.Remove(tabPageAltrady);

            BinanceClient.SetDefaultOptions(new BinanceClientOptions() { });
            BinanceSocketClientOptions options = new BinanceSocketClientOptions();
            options.SpotStreamsOptions.AutoReconnect = true;
            options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
            BinanceSocketClient.SetDefaultOptions(options);

            // De intervallen laden
            GlobalData.InitializeIntervalList();
            ResumeComputer(false);


            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                GlobalData.AddTextToLogTab("Exiting!");
                ProgramExit = true;
            };

            SystemEvents.PowerModeChanged += OnPowerChange;


            InitEventView();
            UpdateInfoAndbarometerValues();

            //MetroStyleManager.Theme = newTheme;
            //MetroStyleManager.Default.Theme = (MetroThemeStyle)MetroColorStyle.Magenta;

        }


        private void ApplySettings()
        {
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
            listboxSymbolsInitCaptions();
            listViewSignalsInitCaptions();
            if (GlobalData.Settings.Signal.SoundHeartBeatMinutes > 0)
                timerSoundHeartBeat.Interval = GlobalData.Settings.Signal.SoundHeartBeatMinutes * 1000 * 60;
            timerSoundHeartBeat.Enabled = GlobalData.Settings.Signal.SoundHeartBeatMinutes > 0;

            // Interval voor het ophalen van de candles bijwerken
            timerCandles.Enabled = false;
            timerCandles.Interval = GlobalData.Settings.General.GetCandleInterval * 60 * 1000;
            timerCandles.Enabled = timerCandles.Interval > 0;

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

            listViewInformation.GridLines = false;
            listViewInformation.View = View.Details;

            listViewSymbolPrices.GridLines = false;
            listViewSymbolPrices.View = View.Details;

            ApplicationPlaySounds.Checked = GlobalData.Settings.Signal.SoundsActive;
            ApplicationCreateSignals.Checked = GlobalData.Settings.Signal.SignalsActive;

            this.Refresh();
        }




        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
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
            GlobalData.ApplicationStatus = ApplicationStatus.AppStatusPrepare;
            GlobalData.ThreadCreateSignal = new ThreadCreateSignal(BinanceShowNotification);

            // Iets met netwerk verbindingen wat nog niet "up" is?
            if (sleepAwhile)
                Thread.Sleep(5000);

            Task.Run(async () => { await new ThreadLoadData().ExecuteAsync(); });
        }

        private void CloseCryptoScannerSession()
        {
            GlobalData.ApplicationStatus = ApplicationStatus.AppStatusExiting;

            // De socket streams
            if (GlobalData.TaskBinanceStreamPriceTicker != null)
                GlobalData.TaskBinanceStreamPriceTicker.StopAsync();

            // Threads (of tasks)
            GlobalData.ThreadCreateSignal.Stop();

            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                foreach (BinanceStream1mCandles binanceStream1mCandles in quoteData.BinanceStream1mCandles)
                {
                    binanceStream1mCandles.StopAsync();
                }
                quoteData.BinanceStream1mCandles.Clear();
            }

            WindowLocationSave();
            GlobalData.Settings.General.SelectedBarometerQuote = comboBoxBarometerQuote.Text;
            GlobalData.Settings.General.SelectedBarometerInterval = comboBoxBarometerInterval.Text;
            GlobalData.SaveSettings();

            // Hier moet alles bewaard worden (zie threadLoadData)
            DataStore dataStore = new DataStore();
            dataStore.SaveCandles();
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
                if (extraLineFeed)
                    text += "\r\n\r\n";
                else
                    text += "\r\n";

                if (InvokeRequired)
                    Invoke((MethodInvoker)(() => TextBoxLog.AppendText(text)));
                else
                    TextBoxLog.AppendText(text);
            }
        }



        private void ToolStripMenuItemRefresh_Click_1(object sender, EventArgs e)
        {
            BinanceFetchCandles binanceFetchCandles = new BinanceFetchCandles();
            Task.Run(async () => { await binanceFetchCandles.ExecuteAsync(); }); // niet wachten tot deze klaar is
        }



        private void CreateBarometerBitmap(CryptoQuoteData quoteData, CryptoInterval interval)
        {
            float blocks = 4;

            // pixel dimensies van het plaatje
            int intWidth = pictureBox1.Width;
            int intHeight = pictureBox1.Height;

            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                CryptoSymbol symbol;
                if ((quoteData != null) && (exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice + quoteData.Name, out symbol)))
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
                        PointF p1 = new PointF(0, offsetY + scaleY * y);
                        PointF p2 = new PointF(intWidth, offsetY + scaleY * y);
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

                        PointF p1 = new PointF(0, 0);
                        p1.X = offsetX + scaleX * (float)(lastX - loX);
                        PointF p2 = new PointF(0, intHeight);
                        p2.X = offsetX + scaleX * (float)(lastX - loX);
                        g.DrawLine(Pens.Gray, p1, p2);
                        lastX -= intervalTime;
                    }


                    bool init = false;
                    PointF point1 = new PointF(0, 0);
                    PointF point2 = new PointF(0, 0);
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

                    if (candleList.Values.Count > 0)
                    {
                        CryptoCandle candle = candleList.Values[candleList.Values.Count - 1];
                        Rectangle rect = new Rectangle(0, 0, intWidth, intHeight - 8);
                        string text = CandleTools.GetUnixDate((long)candle.OpenTime + 60).ToLocalTime().ToString("HH:mm");

                        Font drawFont = new Font("Microsoft Sans Serif", this.Font.Size);
                        TextRenderer.DrawText(g, text, drawFont, rect, Color.Black, Color.Transparent, TextFormatFlags.Bottom);
                        //g.DrawString(text, drawFont, Brushes.Black, rect);

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
                BarometerTools barometerTools = new BarometerTools();
                barometerTools.Execute();

                CryptoExchange exchange;
                if (!GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
                {
                    return;
                }

                {
                    string baseCoin = "";
                    CryptoQuoteData quoteData;
                    Invoke((MethodInvoker)(() => baseCoin = comboBoxBarometerQuote.Text));
                    if (!GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out quoteData))
                    {
                        return;
                    }

                    BarometerData barometerData;

                    //barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1h];
                    //ApplyMoodColor(panelBarometer1h, labelBarometer1hValue, "1h: ", barometerData.PriceBarometer);

                    //barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval4h];
                    //ApplyMoodColor(panelBarometer4h, labelBarometer4hValue, "4h: ", barometerData.PriceBarometer);

                    //barometerData = quoteData.BarometerList[(long)CryptoIntervalPeriod.interval1d];
                    //ApplyMoodColor(panelBarometer1d, labelBarometer1dValue, "24h: ", barometerData.PriceBarometer);


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
                    CryptoInterval interval;
                    if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out interval))
                    {
                        return;
                    }

                    barometerData = quoteData.BarometerList[(int)intervalPeriod];
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
        private List<SymbolHist> symbolHistList = new List<SymbolHist>(3);


        private void ShowSymbolPrice(SymbolHist hist, ListViewItem item, CryptoExchange exchange, CryptoQuoteData quoteData, string baseCoin, SymbolValue tvValues)
        {
            // Not a really charming way to display items, but voila, it works for now..

            if (baseCoin == "FNG")
            {
                ListViewItem.ListViewSubItem subItem = item.SubItems[0];
                subItem.Text = "Fear and Greed";

                subItem = item.SubItems[1];
                subItem.Text = FearAndGreedIndex;

                subItem = item.SubItems[2];
                subItem.Text = "";

                subItem = item.SubItems[3];
                subItem.Text = tvValues.Name;

                decimal value = tvValues.Lp;
                subItem = item.SubItems[4];
                subItem.Text = value.ToString(tvValues.DisplayFormat);
                if (value < tvValues.LastValue)
                    subItem.ForeColor = Color.Red;
                else if (value > tvValues.LastValue)
                    subItem.ForeColor = Color.Green;
                tvValues.LastValue = value;

                return;
            }

            CryptoSymbol symbol;
            if (!exchange.SymbolListName.TryGetValue(baseCoin + quoteData.Name, out symbol))
                exchange.SymbolListName.TryGetValue(baseCoin + "USDT", out symbol);

            if (symbol != null)
            {
                decimal value;
                ListViewItem.ListViewSubItem subItem = item.SubItems[0];
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


                subItem = item.SubItems[3];
                subItem.Text = tvValues.Name;

                value = tvValues.Lp;
                subItem = item.SubItems[4];
                subItem.Text = value.ToString(tvValues.DisplayFormat);
                if (value < tvValues.LastValue)
                    subItem.ForeColor = Color.Red;
                else if (value > tvValues.LastValue)
                    subItem.ForeColor = Color.Green;
                tvValues.LastValue = value;


                hist.Symbol = symbol.Name;
                hist.PricePrev = symbol.LastPrice;
                hist.VolumePrev = (decimal)symbol.Volume;
            }
            else
            {
                item.Text = "";
                item.SubItems[0].Text = "";
                item.SubItems[1].Text = "";
                item.SubItems[2].Text = "";
                item.SubItems[3].Text = "";

                hist.Symbol = "";
                hist.PricePrev = 0m;
                hist.VolumePrev = 0m;
            }
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
            // Plan een verversing omdat er een connection timeout was.
            // Dit kan een aantal berekeningen onderbroken hebben
            // (er komen een aantal reconnects, daarom circa 20 seconden)
            if ((components != null) && (!ProgramExit) && (IsHandleCreated))
            {
                Invoke((MethodInvoker)(() => timerCandles.Enabled = false));
                Invoke((MethodInvoker)(() => timerCandles.Interval = 20 * 1000));
                Invoke((MethodInvoker)(() => timerCandles.Enabled = true));
            }
        }


        private void SetCandleTimerEnableHandler(bool value)
        {
            if ((components != null) && (!ProgramExit) && (IsHandleCreated))
            {
                if (InvokeRequired)
                    Invoke((MethodInvoker)(() => timerCandles.Enabled = value));
                else
                    timerCandles.Enabled = value;
            }
        }


        private void listViewSymbolPrices_DoubleClick(object sender, EventArgs e)
        {
            if (listViewSymbolPrices.SelectedItems.Count > 0)
            {
                for (int index = 0; index < listViewSymbolPrices.SelectedItems.Count; index++)
                {
                    ListViewItem item = listViewSymbolPrices.SelectedItems[index];

                    CryptoExchange exchange;
                    if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
                    {
                        CryptoSymbol symbol;
                        if (exchange.SymbolListName.TryGetValue(item.Text, out symbol))
                        {
                            CryptoInterval interval;
                            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out interval))
                                return;

                            ActivateTradingApp(symbol, interval);
                            return;
                        }
                    }
                }
            }
        }


        private void UpdateInfoAndbarometerValuesIntern(ListViewItem item, int? count, CryptoQuoteData quoteData, CryptoIntervalPeriod cryptoIntervalPeriod)
        {
            ListViewItem.ListViewSubItem subItem;

            subItem = item.SubItems[1];
            if (GlobalData.ApplicationStatus != ApplicationStatus.AppStatusRunning)
                subItem.Text = "?";
            else
                subItem.Text = count?.ToString("N0");


            if (quoteData == null)
            {
                subItem = item.SubItems[3];
                subItem.BackColor = Color.White;
                subItem = item.SubItems[4];
                subItem.Text = "?";
            }
            else
            {
                BarometerData barometerData = quoteData.BarometerList[(long)cryptoIntervalPeriod];
                decimal? value = barometerData.PriceBarometer;
                subItem = item.SubItems[3];
                if (!value.HasValue)
                    subItem.BackColor = Color.White;
                else if (value < 0.0M)
                    subItem.BackColor = Color.Red;
                else
                    subItem.BackColor = Color.Green;

                subItem = item.SubItems[4];
                subItem.Text = value?.ToString("N2");
                if (!value.HasValue)
                {
                    subItem.Text = "?";
                    subItem.ForeColor = Color.Black;
                }
                else if (value < 0.0M)
                    subItem.ForeColor = Color.Red;
                else
                    subItem.ForeColor = Color.Green;

            }

        }

        private void UpdateInfoAndbarometerValues()
        {
            // Tel het aantal ontvangen 1m candles (via alle uitstaande streams)
            // Elke minuut komt er van elke munt een candle (indien er gehandeld is).
            int CandlesKLinesCount = 0;
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                for (int i = 0; i < quoteData.BinanceStream1mCandles.Count; i++)
                {
                    BinanceStream1mCandles binanceStream1mCandles = quoteData.BinanceStream1mCandles[i];
                    int currentCount = binanceStream1mCandles.CandlesKLinesCount;
                    CandlesKLinesCount += currentCount;
                }
            }

            listViewInformation.BeginUpdate();
            try
            {
                if (listViewInformation.Columns.Count == 0)
                {
                    listViewInformation.Columns.Add("Binance price ticker", -2, HorizontalAlignment.Left);
                    listViewInformation.Columns.Add("8.123.123", -2, HorizontalAlignment.Right);
                    listViewInformation.Columns.Add("Int", -2, HorizontalAlignment.Right);
                    listViewInformation.Columns.Add("12", -2, HorizontalAlignment.Right);
                    listViewInformation.Columns.Add("Val", -2, HorizontalAlignment.Right);
                    listViewInformation.Columns.Add("", -2, HorizontalAlignment.Right); // filler

                    listViewInformation.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

                    ListViewItem item1 = new ListViewItem("Binance price ticker count", -1);
                    item1.UseItemStyleForSubItems = false;
                    item1.SubItems.Add("?");
                    item1.SubItems.Add("1H:");
                    item1.SubItems.Add("");
                    item1.SubItems.Add("");
                    listViewInformation.Items.Add(item1);

                    item1 = new ListViewItem("Binance 1m stream count", -1);
                    item1.UseItemStyleForSubItems = false;
                    item1.SubItems.Add("?");
                    item1.SubItems.Add("4H:");
                    item1.SubItems.Add("");
                    item1.SubItems.Add("");
                    listViewInformation.Items.Add(item1);

                    item1 = new ListViewItem("Scanner analyse count", -1);
                    item1.UseItemStyleForSubItems = false;
                    item1.SubItems.Add("?");
                    item1.SubItems.Add("1D:");
                    item1.SubItems.Add("");
                    item1.SubItems.Add("");
                    listViewInformation.Items.Add(item1);
                }



                string baseCoin;
                CryptoQuoteData quoteData;
                //Invoke((MethodInvoker)(() => baseCoin = comboBoxBarometerQuote.Text));
                baseCoin = comboBoxBarometerQuote.Text;
                GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out quoteData);

                int count;
                if (GlobalData.TaskBinanceStreamPriceTicker != null)
                    count = GlobalData.TaskBinanceStreamPriceTicker.tickerCount;
                else
                    count = 0;
                UpdateInfoAndbarometerValuesIntern(listViewInformation.Items[0], count, quoteData, CryptoIntervalPeriod.interval1h);
                UpdateInfoAndbarometerValuesIntern(listViewInformation.Items[1], CandlesKLinesCount, quoteData, CryptoIntervalPeriod.interval4h);
                if (GlobalData.ThreadCreateSignal != null)
                    count = GlobalData.ThreadCreateSignal.analyseCount;
                else
                    count = 0;
                UpdateInfoAndbarometerValuesIntern(listViewInformation.Items[2], count, quoteData, CryptoIntervalPeriod.interval1d);


                for (int i = 0; i <= listViewInformation.Columns.Count - 1; i++)
                {
                    listViewInformation.Columns[i].Width = -2;
                }
            }
            finally
            {
                listViewInformation.EndUpdate();
            }

        }

        private string FearAndGreedIndex = "";
        private DateTime? FearAndGreedIndexDate = null;

        private void timerBarometer_Tick(object sender, EventArgs e)
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
                UpdateInfoAndbarometerValues();


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
                try
                {
                    if ((FearAndGreedIndexDate == null) || (DateTime.UtcNow >= FearAndGreedIndexDate))
                    {
                        System.Net.WebRequest request = WebRequest.Create("https://api.alternative.me/fng/");
                        WebResponse response = request.GetResponse();
                        //if (response.IsSuccessStatusCode)
                        {
                            System.IO.Stream data = response.GetResponseStream();
                            string html = String.Empty;
                            using (System.IO.StreamReader sr = new System.IO.StreamReader(data))
                            {
                                html = sr.ReadToEnd();
                                var jsonData = (JObject)JsonConvert.DeserializeObject(html);
                                FearAndGreedIndex = jsonData["data"][0]["value"].Value<string>();
                                //FearAndGreedIndex = jsonData["data"][0]["value"].Value<string>();
                                FearAndGreedIndexDate = DateTime.UtcNow.AddHours(1);
                            }
                        }
                    }
                }
                catch
                {
                    FearAndGreedIndex = "Connection-Error";
                }



                // Toon de prijzen en volume van een aantal symbols
                CryptoExchange exchange;
                if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
                {
                    listViewSymbolPrices.BeginUpdate();
                    try
                    {
                        if (listViewSymbolPrices.Columns.Count == 0)
                        {
                            listViewSymbolPrices.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
                            listViewSymbolPrices.Columns.Add("Price", -2, HorizontalAlignment.Right);
                            listViewSymbolPrices.Columns.Add("Volume", -2, HorizontalAlignment.Right);
                            listViewSymbolPrices.Columns.Add("Market", -2, HorizontalAlignment.Left);
                            listViewSymbolPrices.Columns.Add("Value", -2, HorizontalAlignment.Right);
                            listViewSymbolPrices.Columns.Add("", -2, HorizontalAlignment.Right); // filler

                            listViewSymbolPrices.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

                            for (int i = 0; i < 4; i++)
                            {
                                ListViewItem item1 = new ListViewItem("", -1);
                                item1.UseItemStyleForSubItems = false;
                                item1.SubItems.Add("");
                                item1.SubItems.Add("");
                                item1.SubItems.Add("");
                                item1.SubItems.Add("");
                                item1.SubItems.Add("");
                                listViewSymbolPrices.Items.Add(item1);

                                symbolHistList.Add(new SymbolHist());
                            }
                        }


                        string baseCoin = "";
                        CryptoQuoteData quoteData;
                        Invoke((MethodInvoker)(() => baseCoin = comboBoxBarometerQuote.Text));
                        if (GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out quoteData))
                        {
                            ShowSymbolPrice(symbolHistList[0], listViewSymbolPrices.Items[0], exchange, quoteData, "BNB", GlobalData.TradingViewDollarIndex);
                            ShowSymbolPrice(symbolHistList[1], listViewSymbolPrices.Items[1], exchange, quoteData, "ETH", GlobalData.TradingViewBitcoinDominance);
                            ShowSymbolPrice(symbolHistList[2], listViewSymbolPrices.Items[2], exchange, quoteData, "BTC", GlobalData.TradingViewSpx500);
                            ShowSymbolPrice(symbolHistList[3], listViewSymbolPrices.Items[3], exchange, quoteData, "FNG", GlobalData.TradingViewMarketCapTotal);
                        }

                        for (int i = 0; i <= listViewSymbolPrices.Columns.Count - 1; i++)
                        {
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
                    Uri uri = new Uri(href);
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


        private void symbolFilter_KeyDown(object sender, KeyEventArgs e)
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
            GlobalData.Settings.Signal.SoundsActive = ApplicationPlaySounds.Checked;
            GlobalData.Settings.Signal.SignalsActive = ApplicationCreateSignals.Checked;
            GlobalData.Settings.General.SelectedBarometerQuote = comboBoxBarometerQuote.Text;
            GlobalData.Settings.General.SelectedBarometerInterval = comboBoxBarometerInterval.Text;
            try
            {
                FrmSettings form = new FrmSettings();
                form.StartPosition = FormStartPosition.CenterParent;
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

        private void mainMenuClearAll_Click(object sender, EventArgs e)
        {
            TextBoxLog.Clear();
            createdSignalCount = 0;
            listViewSignalsMenuItemClearSignals_Click(null, null);

            GlobalData.TaskBinanceStreamPriceTicker.tickerCount = 0;
            GlobalData.ThreadCreateSignal.analyseCount = 0;

            this.Text = "";
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

            listViewSignalsInitColumns();

            timerClearEvents.Interval = 60000; // iedere minuut
            timerClearEvents.Enabled = true;
            timerClearEvents.Tick += new System.EventHandler(this.timerClearOldSignals_Tick);
        }



        private void PlaySound(CryptoSignal signal, bool playSound, bool playSpeech, string soundName, ref long lastSound)
        {
            // Reduce the amount of sounds/speech
            if (signal.EventTime > lastSound)
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


        private void BinanceShowNotification(CryptoSignal signal) //, string MethodText, string EventText)
        {
            createdSignalCount++;
            string text = "signal: " + signal.Symbol.Name + " " + signal.Interval.Name + " " + signal.ModeText + " " + signal.StrategyText + " " + signal.EventText;
            GlobalData.AddTextToLogTab(text);

            // Zet de laatste munt in de "caption" (en taskbar) van de applicatie bar (visuele controle of er meldingen zijn)
            Invoke(new Action(() => { this.Text = signal.Symbol.Name + " " + createdSignalCount.ToString(); }));

            Task.Factory.StartNew(() =>
            {
                Invoke(new Action(() =>
                {
                    listViewSignalsAddSignal(signal);
                }));
            });



            // Play a sound
            switch (signal.Strategy)
            {
                case SignalStrategy.strategyCandlesJumpUp:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundCandleJumpSignal, GlobalData.Settings.Signal.PlaySpeechCandleJumpSignal,
                        GlobalData.Settings.Signal.SoundCandleJumpUp, ref LastSignalSoundCandleJumpUp);
                    break;
                case SignalStrategy.strategyCandlesJumpDown:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundCandleJumpSignal, GlobalData.Settings.Signal.PlaySpeechCandleJumpSignal,
                        GlobalData.Settings.Signal.SoundCandleJumpDown, ref LastSignalSoundCandleJumpDown);
                    break;

                case SignalStrategy.strategyStobbOversold:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundStobbSignal, GlobalData.Settings.Signal.PlaySpeechStobbSignal,
                        GlobalData.Settings.Signal.SoundStobbOversold, ref LastSignalSoundStobbOversold);
                    break;
                case SignalStrategy.strategyStobbOverbought:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundStobbSignal, GlobalData.Settings.Signal.PlaySpeechStobbSignal,
                        GlobalData.Settings.Signal.SoundStobbOverbought, ref LastSignalSoundStobbOverbought);
                    break;

                case SignalStrategy.strategySbm1Oversold:
                case SignalStrategy.strategySbm2Oversold:
                case SignalStrategy.strategySbm3Oversold:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundSbmSignal, GlobalData.Settings.Signal.PlaySpeechSbmSignal,
                        GlobalData.Settings.Signal.SoundSbmOversold, ref LastSignalSoundSbmOversold);
                    break;

                case SignalStrategy.strategySbm1Overbought:
                case SignalStrategy.strategySbm2Overbought:
                case SignalStrategy.strategySbm3Overbought:
                    PlaySound(signal, GlobalData.Settings.Signal.PlaySoundSbmSignal, GlobalData.Settings.Signal.PlaySpeechSbmSignal,
                        GlobalData.Settings.Signal.SoundSbmOverbought, ref LastSignalSoundSbmOverbought);
                    break;
            }

        }



        async private void ActivateTradingViewBrowser(string symbolname = "BTCBUSD")
        {
            CryptoInterval interval;
            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out interval))
                return;

            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                CryptoSymbol symbol;
                if (exchange.SymbolListName.TryGetValue(symbolname, out symbol))
                {
                    // https://stackoverflow.com/questions/63404822/how-to-disable-cors-in-wpf-webview2
                    var userPath = GlobalData.GetBaseDir();
                    var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userPath);
                    await webViewTradingView.EnsureCoreWebView2Async(webView2Environment);

                    var href = TradingView.GetRef(symbol, interval);
                    Uri uri = new Uri(href);
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


        private void applicationMenuItemAbout_Click(object sender, EventArgs e)
        {
            AboutBox form = new AboutBox();
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog();
        }

        private void timerSoundHeartBeat_Tick(object sender, EventArgs e)
        {
            GlobalData.PlaySomeMusic("sound-heartbeat.wav");
        }


        private void ShowTrendInformation(CryptoSymbol symbol)
        {

            StringBuilder log = new StringBuilder();
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

                TrendIndicator trendIndicatorClass = new TrendIndicator(symbol, interval);
                trendIndicatorClass.Log = log;
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

        private void timerCandles_Tick(object sender, EventArgs e)
        {
            // De reguliere verversing herstellen (igv een connection timeout)
            if ((components != null) && (!ProgramExit) && (IsHandleCreated))
            {
                // Plan een volgende verversing omdat er bv een connection timeout was.
                // Dit kan een aantal berekeningen onderbroken hebben
                Invoke((MethodInvoker)(() => timerCandles.Enabled = false));
                Invoke((MethodInvoker)(() => timerCandles.Interval = GlobalData.Settings.General.GetCandleInterval * 60 * 1000));
                Invoke((MethodInvoker)(() => timerCandles.Enabled = timerCandles.Interval > 0));
            }

            BinanceFetchCandles binanceFetchCandles = new BinanceFetchCandles();
            Task.Run(async () => { await binanceFetchCandles.ExecuteAsync(); }); // niet wachten tot deze klaar is

        }



        public void WindowLocationSave()
        {
            GlobalData.Settings.General.WindowPosition = DesktopBounds;

            // only save the WindowState if Normal or Maximized
            switch (WindowState)
            {
                case FormWindowState.Normal:
                case FormWindowState.Maximized:
                    GlobalData.Settings.General.WindowState = WindowState;
                    break;

                default:
                    GlobalData.Settings.General.WindowState = FormWindowState.Normal;
                    break;
            }
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

        private bool IsVisibleOnAnyScreen(Rectangle rect)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(rect))
                {
                    return true;
                }
            }

            return false;
        }

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
    }

}
