using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Objects;

using CryptoScanBot.BackTest;
using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;
using CryptoScanBot.Core.Telegram;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.Zones;

using Dapper;

using Skender.Stock.Indicators;

using System.Drawing.Drawing2D;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Font = System.Drawing.Font;

namespace CryptoScanBot;



public partial class TestForm : Form
{

    [Serializable]
    public class TestObject
    {
        [JsonConverter(typeof(SecureStringConverter))]
        public string Password { get; set; } = "";
    }

    // werkt niet? (zoals ik het verwachte)
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum
    [Flags]
    public enum AttackTypeSet : int
    {
        // Decimal     // Binary
        None = 0,    // 000000
        Melee = 1,    // 000001
        Fire = 2,    // 000010
        Ice = 4,    // 000011
        Poison = 8     // 000100
    }

    //private bool ProgramExit; // = false; //Mislukte manier om excepties bij afsluiten te voorkomen (todo)

    //AttackTypeSet a = AttackTypeSet.Melee | AttackTypeSet.Poison;
    //a = AttackTypeSet.Poison;

    //AttackTypeSet a;
    //a=4;
    //a += AttackTypeSet.Melee;


    //[Flags]
    //public enum Sides
    //{
    //    Left = 1,
    //    Right = 2,
    //    Top = 4,
    //    Bottom = 8
    //}

    //--update exchange set LastTimeFetched = null;
    //--delete from candle
    //--delete from SymbolInterval
    //--delete from Symbol

    //update symbol set LastTradeDate = null;
    //delete from PositionStep
    //delete from PositionPart
    //delete from Position
    //delete from Signal


    private readonly CryptoDatabase databaseMain;
    private StringBuilder Log;
    static readonly private SemaphoreSlim Semaphore = new(3);
    public CryptoBackTestResults Results;
    public int createdSignalCount = 0;


    private readonly List<CryptoSignal> SignalList = [];
    private readonly CryptoDataGridSignal<CryptoSignal> GridSignals;

    private readonly List<CryptoSymbol> SymbolList = [];
    private readonly CryptoDataGridSymbol<CryptoSymbol> GridSymbols;

    public TestForm()
    {
        InitializeComponent();


        comboBox1.Items.Clear();
        //foreach (AlgorithmDefinition def in TradingConfig.AlgorithmDefinitionIndex.Values)
        //    comboBox1.Items.Add(def.Name);
        //comboBox1.SelectedIndex = 5;

        tabControl.SelectedTab = tabPageLog;

        GridSignals = new() { Grid = dataGridViewSignal, List = SignalList, ColumnList = GlobalData.SettingsUser.GridColumnsSignal };
        GridSignals.InitGrid();
        GridSymbols = new() { Grid = dataGridViewSymbol, List = SymbolList, ColumnList = GlobalData.SettingsUser.GridColumnsSymbol };
        GridSymbols.InitGrid();



        //Om vanuit achtergrond threads iets te kunnen loggen (kan charmanter?)
        GlobalData.LogToTelegram += new AddTextEvent(AddTextToTelegram);
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);


        GlobalData.ThreadSaveObjects = new ThreadSaveObjects();
        GlobalData.ThreadMonitorCandle = new ThreadMonitorCandle();
        GlobalData.AnalyzeSignalCreated = AnalyzeSignalCreated;
        GlobalData.ThreadZoneCalculate = new ThreadZoneCalculate();

        //string APIKEY = "?";
        //string APISECRET = "?";

        //BinanceRestClient.SetDefaultOptions(new BinanceClientOptions() { });
        //BinanceSocketClientOptions options = new();
        //options.ApiCredentials = new BinanceApiCredentials(APIKEY, APISECRET);
        //options.SpotStreamsOptions.AutoReconnect = true;
        //options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
        //BinanceSocketClient.SetDefaultOptions(options);

        //BinanceClient.SetDefaultOptions(new BinanceClientOptions()
        //{
        //    ApiCredentials = new Binance.Net.Objects.BinanceApiCredentials(APIKEY, APISECRET),
        //    //LogVerbosity = LogVerbosity.Debug,
        //    //LogWriters = new List<TextWriter> { Console.Out }
        //});

        //BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
        //{
        //    ApiCredentials = new Binance.Net.Objects.BinanceApiCredentials(APIKEY, APISECRET),
        //    //LogVerbosity = LogVerbosity.Debug,
        //    //LogWriters = new List<TextWriter> { Console.Out }
        //});



        GlobalData.LoadSettings();
        //TradingConfig.IndexStrategyInternally();

        // Basicly allemaal constanten
        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();
        GlobalData.LoadAccounts();
        GlobalData.Settings.Trading.TradeVia = CryptoAccountType.PaperTrade;
        GlobalData.SetTradingAccounts();
        GlobalData.LoadSymbols();
        //ZoneTools.LoadAllZones();

        // Na het selecteren van een account
        GlobalData.Settings.General.Exchange!.GetApiInstance().ExchangeDefaults();

        databaseMain = new();
        databaseMain.Open();

        //Alle symbols uit de database lezen 
        //GlobalData.AddTextToLogTab("Reading symbol information from database");
        //foreach (CryptoSymbol symbol in databaseMain.Connection.GetAll<CryptoSymbol>())
        //{
        //    GlobalData.AddSymbol(symbol);
        //}

        //Alle trades uit de database lezen (dat kunnen er best veel worden)
        //foreach (CryptoTrade trade in databaseMain.Connection.GetAll<CryptoTrade>())
        //{
        //    GlobalData.AddTrade(trade);
        //}

        InitEventView();

        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        //buttonBitmap_Click(null, null);


        //ColorSchemeTest theme = new ColorSchemeTest();
        //ChangeTheme(theme, this.Controls);

        //metroStyleManager1.
        // https://symbol-search.tradingview.com/symbol_search/?text=dx&type=cfd
        // https://www.tradingview.com/symbols/ICEUS-DX1!/
        //https://www.tradingview.com/chart/C0G0Mzob/?symbol=ICEUS%3ADX1%21
        //ICEUS-DX1!

        //SymbolValue valueDxy = new SymbolValue();
        //valueDxy.Name = "Dollar Index";
        //Task.Factory.StartNew(() => new TradingViewSymbolInfo().Start("TVC:DXY", valueDxy, 10));

        //SymbolValue valueSpx500 = new SymbolValue();
        //valueSpx500.Name = "SPX 500";
        //Task.Factory.StartNew(() => new TradingViewSymbolInfo().Start("SP:SPX", valueSpx500, 10));

        //SymbolValue bitcoinDominance = new SymbolValue();
        //bitcoinDominance.Name = "BTC.D";
        //Task.Factory.StartNew(() => new TradingViewSymbolInfo().Start("CRYPTOCAP:BTC.D", bitcoinDominance, 10));

        //SymbolValue marketcapTotal = new SymbolValue();
        //marketcapTotal.Name = "Maerketcap total";
        //Task.Factory.StartNew(() => new TradingViewSymbolInfo().Start("CRYPTOCAP:TOTAL3", marketcapTotal, 10));


        //Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("FX_IDC:EURUSD", "dit is een test", "N2", GlobalData.TradingViewDollarIndex, 1000));
        Button1_Click(null, null);
    }




    //private void ChangeTheme(ColorSchemeTest theme, Control.ControlCollection container)
    //{
    //    //return;
    //    foreach (Control component in container)
    //    {
    //        if (component is Form)
    //            ((MetroFramework.Forms.MetroForm)component).StyleManager = this.StyleManager;
    //        if (component is MetroFramework.Controls.MetroPanel)
    //            ((MetroFramework.Controls.MetroPanel)component).StyleManager = this.StyleManager;
    //        else if (component is MetroFramework.Controls.MetroButton)
    //            ((MetroFramework.Controls.MetroButton)component).StyleManager = this.StyleManager;
    //        else if (component is MetroFramework.Controls.MetroTextBox)
    //            ((MetroFramework.Controls.MetroTextBox)component).StyleManager = this.StyleManager;
    //        else if (component is MetroFramework.Controls.MetroScrollBar)
    //            ((MetroFramework.Controls.MetroScrollBar)component).StyleManager = this.StyleManager;
    //        else if (component is MetroFramework.Controls.MetroLabel)
    //            ((MetroFramework.Controls.MetroLabel)component).StyleManager = this.StyleManager;
    //        else if (component is MetroFramework.Controls.MetroTabControl)
    //            ((MetroFramework.Controls.MetroTabControl)component).StyleManager = this.StyleManager;
    //        else if (component is MetroFramework.Controls.MetroTabPage)
    //            ((MetroFramework.Controls.MetroTabPage)component).StyleManager = this.StyleManager;
    //        //else if (component is MetroFramework.Controls.MetroListBox)
    //        //    ((MetroFramework.Controls.MetroListBox)component).StyleManager = this.StyleManager;

    //        else if (component is ListBox)
    //        {
    //        }
    //        else if (component is MetroFramework.Controls.MetroListView)
    //            ((MetroFramework.Controls.MetroListView)component).StyleManager = this.StyleManager;

    //        else if (component is MetroFramework.Controls.MetroComboBox)
    //            ((MetroFramework.Controls.MetroComboBox)component).StyleManager = this.StyleManager;
    //        else if (component is CheckBox)
    //        {
    //        }
    //        else if (component is MetroFramework.Controls.MetroListView)
    //            ((MetroFramework.Controls.MetroListView)component).StyleManager = this.StyleManager;
    //        else if (component is MenuStrip)
    //        {
    //        }
    //        else
    //            GlobalData.AddTextToLogTab(component.ToString());
    //        ChangeTheme(theme, component.Controls);
    //    }
    //}

    /// <summary>
    /// Candles uit de database halen voor de gevraagde interval X indien deze niet aanwezig zijn
    /// </summary>
    private static void LoadSymbolCandles(CryptoSymbol symbol, CryptoInterval interval)
    {
        Semaphore.Wait();
        try
        {
            if (symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.Count == 0)
            {
                GlobalData.AddTextToLogTab(string.Format("{0} {1} Candles lezen", symbol.Name, interval.Name));

                //int aantaltotaal = 0;
                string baseStoragePath = GlobalData.GetBaseDir();
                var exchange = GlobalData.Settings.General.Exchange;
                if (exchange != null)
                {
                    string exchangeStoragePath = baseStoragePath + exchange.Name.ToLower() + @"\";
                    if (!symbol.IsBarometerSymbol() && (symbol.QuoteData!.FetchCandles && symbol.IsSpotTradingAllowed))
                        DataStore.LoadCandleForSymbol(exchangeStoragePath, symbol);
                }
            }
        }
        finally
        {
            Semaphore.Release();
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
        if (IsHandleCreated)
        {
            text = text.Trim();
            ScannerLog.Logger.Info(text);

            if (text != "")
            {
                text += "\r\n";
                if (InvokeRequired)
                    Invoke((MethodInvoker)(() => textBox1.AppendText(text)));
                else
                    textBox1.AppendText(text);
                //File.AppendAllText(@"D:\Shares\Projects\.Net\CryptoScanBot\Testjes\bin\Debug\data\backtest.txt", text);
            }
        }
    }

    private void AnalyzeSignalCreated(CryptoSignal signal) //, string MethodText, string EventText)
    {
        createdSignalCount++;

        StringBuilder stringBuilder = new();

        stringBuilder.AppendLine();
        if (!signal.BackTest)
            stringBuilder.AppendLine("Melding#" + createdSignalCount.ToString() + " " + DateTime.Now.ToLocalTime()); //+ " notification " + notification 
        else
            stringBuilder.AppendLine("Melding#" + createdSignalCount.ToString() + " (backTest)"); //+ " notification " + notification 

        string s = signal.Symbol.Name + " " + signal.Interval.Name + " " + CandleTools.GetUnixDate(signal.Candle.OpenTime).ToLocalTime() + " (" + signal.StrategyText + ") " + signal.EventText;
        stringBuilder.AppendLine(s);

        s = "Candle open " + signal.Candle.Open.ToString();
        s += " high " + signal.Candle.High.ToString();
        s += " low " + signal.Candle.Low.ToString();
        s += " close " + signal.Candle.Close.ToString();
        //s += " volume " + alarm.Stick.SignalVolume.ToString();

        stringBuilder.AppendLine(s);
        stringBuilder.AppendLine("Total 24 hour volume " + signal.Symbol.Volume.ToString("N0") + ", candles zonder volume " + signal.CandlesWithZeroVolume.ToString() + " van 60");
        stringBuilder.AppendLine("Bollingerbands " + signal.BollingerBandsPercentage?.ToString("N2") + "%" + " (low " + signal.BollingerBandsLowerBand?.ToString("N6") + " high " + signal.BollingerBandsUpperBand?.ToString("N6") + ")");


        s = "";
        if (signal.TrendIndicator == CryptoTrendIndicator.Bullish)
            s = "interval trend=bullish";
        else if (signal.TrendIndicator == CryptoTrendIndicator.Bearish)
            s = "interval trend=bearish";
        else
            s = " interval trend=sideway's?";
        stringBuilder.AppendLine(s);


        if (signal.TrendPercentage < 0)
            stringBuilder.AppendLine(string.Format("Symbol trend {0} bearish", (-signal.TrendPercentage).ToString("N2")));
        else if (signal.TrendPercentage > 0)
            stringBuilder.AppendLine(string.Format("Symbol trend {0} bullish", signal.TrendPercentage.ToString("N2")));
        else
            stringBuilder.AppendLine(string.Format("Symbol trend {0} unknown", signal.TrendPercentage.ToString("N2")));


        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab(stringBuilder.ToString());


        //Zet de laatste munt in de "caption" (en taskbar) van de applicatie bar (visuele controle of er meldingen zijn)
        Invoke(new Action(() => { this.Text = signal.Symbol.Name + " " + createdSignalCount.ToString(); }));

        //if (GlobalData.Settings.Signal.SoundSignalNotification)
        //  GlobalData.PlaySomeMusic("sound-analyser-attention.wav");


        Task.Factory.StartNew(() =>
        {
            Invoke(new Action(() =>
            {
                AddEvent(signal);
            }));
        });
    }


    private void InitEventView()
    {
        // Allow the user to edit item text.
        listView1.LabelEdit = true;
        // Allow the user to rearrange columns.
        listView1.AllowColumnReorder = true;
        // Display check boxes.
        //listView1.CheckBoxes = true;
        // Select the item and subitems when selection is made.
        listView1.FullRowSelect = true;
        // Display grid lines.
        listView1.GridLines = true;
        // Sort the items in the list in ascending order.
        //listView1.Sorting = SortOrder.None;

        //listView1.HotTracking = true; // verstoord de kleuren en is onrustig

        // Specify that each item appears on a separate line.
        listView1.View = View.Details; // Voor display van de subitems


        // Create columns for the items and subitems. Width of -2 indicates auto-size.
        listView1.Columns.Add("Candle date", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Symbol", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Interval", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Mode", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Method", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Text", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Event", -2, System.Windows.Forms.HorizontalAlignment.Left);
        listView1.Columns.Add("Price", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("Volume", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("Trend", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("Trend%", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("Last24", -2, System.Windows.Forms.HorizontalAlignment.Right);
        //listView1.Columns.Add("Last48", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("BB%", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("RSI", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("Stoch", -2, System.Windows.Forms.HorizontalAlignment.Right);
        listView1.Columns.Add("Signal", -2, System.Windows.Forms.HorizontalAlignment.Right);

        // Create two ImageList objects.
        //ImageList imageListSmall = new ImageList();
        //ImageList imageListLarge = new ImageList();


        // Initialize the ImageList objects with bitmaps.
        //imageListSmall.Images.Add(Bitmap.FromFile(@"C:\inetpub\Sites\Cryptobot\wwwroot\images\coins\act.png"));
        //imageListSmall.Images.Add(Bitmap.FromFile(@"C:\inetpub\Sites\Cryptobot\wwwroot\images\coins\act.png"));
        //imageListLarge.Images.Add(Bitmap.FromFile(@"C:\inetpub\Sites\Cryptobot\wwwroot\images\coins\act.png"));
        //imageListLarge.Images.Add(Bitmap.FromFile(@"C:\inetpub\Sites\Cryptobot\wwwroot\images\coins\act.png"));

        // AssignValues the ImageList objects to the ListView.
        //listView1.LargeImageList = imageListLarge;
        //listView1.SmallImageList = imageListSmall;


        //// TODO: Configureren icon path

        ////string iconPath = Path.GetDirectoryName((System.Reflection.Assembly.GetEntryAssembly().Location)); GlobalData.GetBaseDir();
        //string iconPath = @"C:\inetpub\Sites\Cryptobot\wwwroot\images\coins\";
        ////iconPath = iconPath + @"\data\icons\";

        //var iconArray = System.IO.Directory.EnumerateFiles(iconPath).ToArray();
        //foreach (string file in iconArray)
        //{
        //    int index = imageListSmall.Images.Count;
        //    string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
        //    try
        //    {
        //        imageListSmall.Images.Add(Bitmap.FromFile(file));
        //        IconNames.Add(fileName, index);
        //    }
        //    catch
        //    {
        //        // ignore
        //    }
        //}


        timerClearEvents.Interval = 60000; // iedere minuut
        timerClearEvents.Enabled = true;
        timerClearEvents.Tick += new System.EventHandler(this.TimerClearEvents_Tick);
    }



    private void AddEvent(CryptoSignal signal)
    {
        listView1.BeginUpdate();
        try
        {
            //int index;
            //if (!IconNames.TryGetValue(signal.Symbol.Base.ToLower(), out index))
            int index = -1;

            string s = signal.OpenDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + signal.OpenDate.AddSeconds(signal.Interval.Duration).ToLocalTime().ToString("HH:mm");
            ListViewItem item1 = new(s, index)
            {
                UseItemStyleForSubItems = false,
                Tag = signal,
            };

            s = signal.Symbol.Base + "/" + @signal.Symbol.Quote;
            decimal tickPercentage = 100 * signal.Symbol.PriceTickSize / signal.SignalPrice;
            if (tickPercentage > 0.2m)
            {
                s += tickPercentage.ToString("N2");
                item1.SubItems.Add(s).ForeColor = Color.Red;
            }
            else item1.SubItems.Add(s);

            item1.SubItems.Add(signal.Interval.Name);

            item1.SubItems.Add("buy");
            if (signal.Side == CryptoTradeSide.Long)
                item1.SubItems.Add(signal.Side.ToString()).ForeColor = Color.Green;
            else if (signal.Side == CryptoTradeSide.Short)
                item1.SubItems.Add(signal.Side.ToString()).ForeColor = Color.Red;
            else
                item1.SubItems.Add(signal.Side.ToString());

            item1.SubItems.Add(signal.StrategyText);
            item1.SubItems.Add(signal.EventText);
            item1.SubItems.Add(signal.SignalPrice.ToString0());
            item1.SubItems.Add(signal.SignalVolume.ToString0("N0"));
            item1.SubItems.Add(signal.TrendIndicator.ToString());

            if (signal.TrendPercentage < 0)
                item1.SubItems.Add(signal.TrendPercentage.ToString("N2")).ForeColor = Color.Red;
            else
                item1.SubItems.Add(signal.TrendPercentage.ToString("N2")).ForeColor = Color.Green;

            if (signal.Last24HoursChange < 0)
                item1.SubItems.Add(signal.Last24HoursChange.ToString("N2")).ForeColor = Color.Red;
            else
                item1.SubItems.Add(signal.Last24HoursChange.ToString("N2")).ForeColor = Color.Green;

            item1.SubItems.Add(signal.BollingerBandsPercentage?.ToString("N2"));

            float value = (float)signal.Rsi;
            if (value < 30)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Red;
            else if (value > 70)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Green;
            else
                item1.SubItems.Add(value.ToString("N2"));

            value = (float)signal.StochOscillator;
            if (value < 20)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Red;
            else if (value > 80)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Green;
            else
                item1.SubItems.Add(value.ToString("N2"));

            value = (float)signal.StochSignal;
            if (value < 20)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Red;
            else if (value > 80)
                item1.SubItems.Add(value.ToString("N2")).ForeColor = Color.Green;
            else
                item1.SubItems.Add(value.ToString("N2"));


            // Add the items to the ListView.
            if (listView1.Items.Count > 0)
                listView1.Items.Insert(0, item1);
            else
                listView1.Items.Add(item1);

            //listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            for (int i = 0; i <= listView1.Columns.Count - 1; i++)
            {
                listView1.Columns[i].Width = -2;
            }
        }
        finally
        {
            listView1.EndUpdate();
        }
    }



    private void ListView1_DoubleClick(object? sender, EventArgs? e)
    {
        if (listView1.SelectedItems.Count > 0)
        {
            ListViewItem item = listView1.SelectedItems[0];
            CryptoSignal signal = (CryptoSignal)item.Tag;
            string[] altradyInterval = ["1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1"];
            string href = string.Format("https://app.altrady.com/d/BINA_{0}_{1}:{2}", signal.Symbol.Quote, signal.Symbol.Base, altradyInterval[(int)signal.Interval.IntervalPeriod]);
            System.Diagnostics.Process.Start(href);
        }
    }

    private void TimerClearEvents_Tick(object? sender, EventArgs? e)
    {
        // Beetje locking is misschien handig? We zien wel?
        if (listView1.Items.Count > 0)
        {
            bool startUpdating = false;
            try
            {

                for (int index = listView1.Items.Count - 1; index > 0; index--)
                {
                    ListViewItem item = listView1.Items[index];
                    CryptoSignal signal = (CryptoSignal)item.Tag;
                    if (signal.ExpirationDate < DateTime.UtcNow)
                    {
                        if (!startUpdating)
                        {
                            listView1.BeginUpdate();
                            startUpdating = true;
                        }
                        listView1.Items.RemoveAt(index);
                    }

                }
            }
            finally
            {
                if (startUpdating)
                    listView1.EndUpdate();
            }
        }
    }


    private async void Button2_Click(object? sender, EventArgs? e)
    {
        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue("ETHUSDT", out CryptoSymbol? symbol))
            {
                //Correctie naar beneden als huidige prijs lager is (gezet door de miniticker)
                decimal price = 54m;
                price = price.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);

                decimal stopPrice = price * 0.8m; //stop is de middelste
                stopPrice = stopPrice.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);

                decimal stopLimitPrice = price * 0.75m; //Limit is de onderste
                stopLimitPrice = stopLimitPrice.Clamp(symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);


                //Aantal afronden
                decimal amount = 15m;
                decimal quantity = amount / price;
                quantity = quantity.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);

                //decimal notational = price * quantity; //En die moet boven de symbol.MinNotional liggen 
                //Dat is hij, toch een error, ook bij Altrady overigens, Binance zit fout (denk ik)

                // Plaats de buy order op Binance 
                WebCallResult<BinanceOrderOcoList> result;
                using var client = new BinanceRestClient();
                {

                    result = await client.SpotApi.Trading.PlaceOcoOrderAsync(symbol.Name, OrderSide.Sell, quantity, price: price, stopPrice, stopLimitPrice, stopLimitTimeInForce: TimeInForce.GoodTillCanceled);

                    //buyresult = buyclient.Spot.Order.PlaceTestOrder(symbol.Name, OrderSide.Buy, OrderType.Limit, BuyQuantity, price: BuyPrice, timeInForce: TimeInForce.GoodTillCancel);

                    //client.PlaceOCOOrder("BNBBTC", OrderSide.Sell, 1, 0.0028m, 0.0024m, stopLimitPrice: 0.0023m, stopLimitTimeInForce: TimeInForce.GoodTillCancel);
                    //client.Spot.Order.PlaceOcoOrder(symbol.Name, OrderSide.Sell, (decimal)quantity, (decimal)price, stopPrice, stopLimitPrice: stopLimitPrice, stopLimitTimeInForce: TimeInForce.GoodTillCancel);

                    if (!result.Success)     //"Filter failure: MIN_NOTIONAL"
                    {
                        GlobalData.AddTextToLogTab("error new buy order " + result.Error);
                    }
                    else
                    {
                        long id = result.Data.OrderReports.ToList()[1].Id;
                        WebCallResult<BinanceOrderBase> cancelResult2;
                        cancelResult2 = await client.SpotApi.Trading.CancelOrderAsync(symbol.Name, id);


                        WebCallResult<BinanceOrderOcoList> cancelResult;
                        cancelResult = await client.SpotApi.Trading.CancelOcoOrderAsync(symbol.Name, result.Data.Id);
                        if (!cancelResult.Success)
                        {
                            GlobalData.AddTextToLogTab("error new buy order " + cancelResult.Error);
                        }
                    }
                }

            }
        }
    }

    private void Button3_Click(object? sender, EventArgs? e)
    {
        decimal sumInvested = 0;
        decimal sumProfit = 0;
        decimal sumCount = 0;

        using CryptoDatabase databaseThread = new();
        databaseThread.Open();

        foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from positions " +
            "where CreateTime >= @fromDate and status=2", new { fromDate = DateTime.Today }))
        {
            sumCount++;
            sumProfit += position.Profit;
            sumInvested += position.Invested;
        }
        GlobalData.AddTextToLogTab(string.Format("Invested {0:N2}, profits {1:N2}, {2:N2}%", sumInvested, sumProfit, 100 * sumProfit / sumCount));
    }


    //static public List<CryptoCandle> CalculateHistory(CryptoCandleList candleSticks, int maxCandles)
    //{
    //    //Transporteer de candles naar de Stock list
    //    //Jammer dat we met tussen-array's moeten werken
    //    //int i = 1;
    //    List<CryptoCandle> history = new(maxCandles);
    //    Monitor.Enter(candleSticks);
    //    try
    //    {
    //        if (candleSticks.Count > 0)
    //        {
    //            long unix = candleSticks.Keys.Last();
    //            while (maxCandles-- > 0)
    //            {
    //                if (candleSticks.TryGetValue(unix, out CryptoCandle? candle))
    //                {
    //                    //In omgekeerde volgorde in de lijst zetten
    //                    if (history.Count == 0)
    //                        history.Add(candle);
    //                    else
    //                        history.Insert(0, candle);
    //                }
    //                unix -=
    //            }
    //        }

    //        ////Vanwege performance nemen we een gedeelte van de candles
    //        //for (int i = candleSticks.Values.Count - 1; i >= 0; i--)
    //        //{
    //        //    CryptoCandle candle = candleSticks.Values[i];

    //        //    //In omgekeerde volgorde in de lijst zetten
    //        //    if (history.Count == 0)
    //        //        history.Add(candle);
    //        //    else
    //        //        history.Insert(0, candle);

    //        //    maxCandles--;
    //        //    if (maxCandles == 0)
    //        //        break;
    //        //}
    //    }
    //    finally
    //    {
    //        Monitor.Exit(candleSticks);
    //    }
    //    return history;
    //}


    static public void LoadConfig(ref CryptoBackConfig config)
    {
        //Laad de gecachte 1m candlesticks (langere historie, minder overhad)
        //string x = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        string filename = GlobalData.GetBaseDir();
        filename += @"\backtest\";
        //Directory.CreateDirectory(filename);
        filename += "backtest.json";
        if (System.IO.File.Exists(filename))
        {
            string text = System.IO.File.ReadAllText(filename);
            config = JsonSerializer.Deserialize<CryptoBackConfig>(text, JsonTools.DeSerializerOptions);
        }
    }


    static public void SaveConfig(CryptoBackConfig config)
    {
        //Laad de gecachte 1m candlesticks (langere historie, minder overhad)
        //string x = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        string filename = GlobalData.GetBaseDir();
        filename += @"\backtest\";
        //Directory.CreateDirectory(filename);
        filename += "backtest.json";
        string text = JsonSerializer.Serialize(config, JsonTools.JsonSerializerIndented);
        System.IO.File.WriteAllText(filename, text);
    }


    //Calculating the angle between two lines without having to calculate the slope?
    //https://stackoverflow.com/questions/3365171/calculating-the-angle-between-two-lines-without-having-to-calculate-the-slope
    //
    // Aha:
    // A complete circle in radians is 2*pi.
    // A complete circle in degrees is 360.
    // To go from degrees to radians, it's (d/360) * 2*pi, or d*pi/180.
    //
    //public static double angleBetween2Lines(Line2D line1, Line2D line2)
    //{
    //    double angle1 = Math.Atan2(line1.getY1() - line1.getY2(), line1.getX1() - line1.getX2());
    //    double angle2 = Math.Atan2(line2.getY1() - line2.getY2(), line2.getX1() - line2.getX2());
    //    return angle1 - angle2;
    //}

    public static double ConvertRadiansToDegrees(double radians)
    {
        double degrees = radians * (180 / Math.PI);
        return (degrees);
    }
    public static double ConvertDegreesToRadians(double degrees)
    {
        double radians = degrees * (Math.PI / 180);
        return (radians);
    }



    private void Button6_Click(object? sender, EventArgs? e)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? exchange))
        //{
        //    if (exchange.SymbolListName.TryGetValue("ALCXBTC", out CryptoSymbol? symbol))
        //    {
        //        DateTime dateCandleStart = DateTime.SpecifyKind(new DateTime(2023, 02, 1, 0, 0, 0), DateTimeKind.Utc);
        //        DateTime dateCandleEinde = DateTime.SpecifyKind(new DateTime(2023, 05, 1, 0, 0, 0), DateTimeKind.Utc);

        //        int iterator = 0;
        //        long percentageSum = 0;
        //        long maxPercentageSum = 0;
        //        for (CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval1m; intervalPeriod <= CryptoIntervalPeriod.interval1d; intervalPeriod++)
        //        {
        //            if (intervalPeriod != CryptoIntervalPeriod.interval5m)
        //                continue;

        //            iterator++;
        //            if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
        //                return;
        //            LoadSymbolCandles(symbol, interval, dateCandleStart, dateCandleEinde);
        //            //CryptoCandleList candles = symbol.GetSymbolInterval(intervalPeriod).CandleList;

        //            TrendIndicator trendIndicatorClass = new(symbol, interval);
        //            CryptoTrendIndicator trendIndicator = trendIndicatorClass.CalculateTrend();

        //            if (trendIndicator == CryptoTrendIndicator.Bullish)
        //                percentageSum += (int)intervalPeriod * iterator;
        //            else if (trendIndicator == CryptoTrendIndicator.Bearish)
        //                percentageSum -= (int)intervalPeriod * iterator;

        //            // Wat is het maximale som (voor de eindberekening)
        //            maxPercentageSum += (int)intervalPeriod * iterator;

        //            // Ahh, dat gaat niet naar een tabel (zoals ik eerst dacht)
        //            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(intervalPeriod);
        //            symbolInterval.TrendIndicator = trendIndicator;
        //        }


        //        // Display
        //        GlobalData.AddTextToLogTab("");
        //        GlobalData.AddTextToLogTab("Trend " + symbol.Name);
        //        for (CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval1m; intervalPeriod <= CryptoIntervalPeriod.interval1d; intervalPeriod++)
        //        {
        //            if (intervalPeriod != CryptoIntervalPeriod.interval5m)
        //                continue;

        //            if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
        //                return;

        //            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(intervalPeriod);

        //            // De trend van dit interval
        //            string s;
        //            if (symbolInterval.TrendIndicator == CryptoTrendIndicator.Bullish)
        //                s = "trend=bullish";
        //            else if (symbolInterval.TrendIndicator == CryptoTrendIndicator.Bearish)
        //                s = "trend=bearish";
        //            else
        //                s = "trend=sideway's?";
        //            GlobalData.AddTextToLogTab(string.Format("{0} {1:N2}", interval.Name, s));
        //        }


        //        // De totale trend
        //        decimal trendPercentage = 100 * (decimal)percentageSum / (decimal)maxPercentageSum;
        //        if (trendPercentage < 0)
        //            GlobalData.AddTextToLogTab(string.Format("Symbol trend {0} bearish", (-trendPercentage).ToString("N2")));
        //        else if (trendPercentage > 0)
        //            GlobalData.AddTextToLogTab(string.Format("Symbol trend {0} bullish", trendPercentage.ToString("N2")));
        //        else
        //            GlobalData.AddTextToLogTab(string.Format("Symbol trend {0} unknown", trendPercentage.ToString("N2")));
        //    }
        //}
    }

    internal class VolatiteitStat
    {
        public string Base = "";
        public string Quote = "";
        public decimal avgDiff;
        public double stddev;
        //public double stddev1h;
        public decimal Volume;
    }


    /// <summary>
    /// Volatiteit meten van coins
    /// Bedoeld om leuke munten voor een gridbot te vinden
    /// </summary>
    private void ButtonVolatiteit_Click(object? sender, EventArgs? e)
    {
        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Lijstjes");
        GlobalData.AddTextToLogTab("");
        List<VolatiteitStat> a = [];

        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            //CryptoSymbol symbol;
            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
            //if (exchange.SymbolListName.TryGetValue("NEBLBUSD", out symbol))
            {
                if (symbol.Quote.Equals("USDT") && symbol.Status == 1 && !symbol.IsBarometerSymbol())
                {
                    int count = 0;
                    decimal diffSum = 0;
                    CryptoIntervalPeriod intervalPeriod;
                    CryptoCandleList candles;
                    DateTime dateCandleStart = DateTime.SpecifyKind(new DateTime(2024, 11, 1, 0, 0, 0), DateTimeKind.Utc);
                    DateTime dateCandleEinde = DateTime.SpecifyKind(new DateTime(2024, 12, 8, 0, 0, 0), DateTimeKind.Utc);


                    //intervalPeriod = CryptoIntervalPeriod.interval10m;
                    //if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out interval))
                    //    return;
                    //LoadSymbolCandles(symbol, interval, dateCandleStart, dateCandleEinde);
                    //candles = symbol.GetSymbolInterval(intervalPeriod).CandleList;

                    //double[] values1h = new double[candles.Count];

                    //count = 0;
                    //diffSum = 0;
                    //foreach (CryptoCandle candle in candles.Values)
                    //{
                    //    values1h[count] = (double)candle.Close;
                    //    count++;
                    //    decimal diff = (candle.High - candle.Low) / candle.Close;
                    //    diffSum += diff;

                    //}



                    intervalPeriod = CryptoIntervalPeriod.interval3m;
                    if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
                        return;
                    LoadSymbolCandles(symbol, interval);
                    candles = symbol.GetSymbolInterval(intervalPeriod).CandleList;

                    double[] values = new double[candles.Count];

                    count = 0;
                    diffSum = 0;
                    foreach (CryptoCandle candle in candles.Values)
                    {
                        values[count] = (double)candle.Close;
                        count++;
                        decimal diff = (candle.High - candle.Low) / candle.Close;
                        diffSum += diff;

                    }


                    VolatiteitStat item = new()
                    {
                        Base = symbol.Base,
                        Quote = symbol.Quote,
                        Volume = symbol.Volume,
                    };
                    if (count > 0)
                    {
                        item.avgDiff = 100 * diffSum / count;
                    }
                    item.stddev = 100 * values.StdDev();
                    //item.stddev1h = values1h.StdDev();
                    a.Add(item);

                    candles.Clear();


                }

            }
        }

        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Sortering");
        GlobalData.AddTextToLogTab("");
        a.Sort((x, y) => y.avgDiff.CompareTo(x.avgDiff));
        foreach (VolatiteitStat item in a)
        {
            GlobalData.AddTextToLogTab(string.Format("{0}/{1};{2:N2}%;{3:N2};{4:N2} ", item.Base, item.Quote, item.avgDiff, item.stddev, item.Volume));
        }

        //// Tradeview lijst
        //GlobalData.AddTextToLogTab("");
        //GlobalData.AddTextToLogTab("Tradeview lijst");
        //GlobalData.AddTextToLogTab("");
        ////a.Sort((x, y) => x.Base.CompareTo(y.Base));
        //foreach (VolatiteitStat item in a)
        //{
        //    GlobalData.AddTextToLogTab(string.Format("BINANCE:{0}{1},", item.Base, item.Quote));
        //}
    }

    private class Period21
    {
        // De hoogste en laagste candle.close
        public decimal Lowest = decimal.MaxValue;
        public decimal Highest = decimal.MinValue;

        // tsja, geen idee
        public decimal Ret = 0m;
        public decimal Pos = 0m;

        public decimal AvgTr = 0m;
    }

    private void Button7_Click(object? sender, EventArgs? e)
    {
        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Lijstjes");
        GlobalData.AddTextToLogTab("");
        //List<VolatiteitStat> a = new List<VolatiteitStat>();



        // Trend trader strategy indicator (bron code pine script)
        // Voor iedere candles van links naar rechts... onderstaande:

        //@version=5
        //indicator(title = 'Trend Trader Strategy', overlay = true)
        //Length = input.int(21, minval = 1)
        //Multiplier = input.float(3, minval = 0.000001)
        //avgTR = ta.wma(ta.atr(1), Length)
        //highestC = ta.highest(Length)
        //lowestC = ta.lowest(Length)
        //hiLimit = highestC[1] - avgTR[1] * Multiplier
        //loLimit = lowestC[1] + avgTR[1] * Multiplier
        //ret = 0.0
        //pos = 0.0
        //ret:= close > hiLimit and close > loLimit ? hiLimit : close < loLimit and close<hiLimit ? loLimit : nz(ret[1], close)
        //pos:= close > ret ? 1 : close < ret ? -1 : nz(pos[1], 0)
        //if pos != pos[1] and pos == 1
        //    alert("Color changed - Buy", alert.freq_once_per_bar_close)
        //if pos != pos[1] and pos == -1
        //    alert("Color changed - Sell", alert.freq_once_per_bar_close)
        //barcolor(pos == -1 ? color.red : pos == 1 ? color.green : color.blue)
        //plot(ret, color = color.new(color.blue, 0), title = 'Trend Trader Strategy')


        //Andere aanpak (want bovenstaande snap ik niets van)
        //https://www.prorealcode.com/prorealtime-indicators/andrew-abraham-trend-trader/
        //avrTR = weightedaverage[Length](AverageTrueRange[1](close))
        //highestC = highest[Length](high)
        //lowestC = lowest[Length](low)
        //hiLimit = highestC[1] - (avrTR[1] * Multiplier)
        //lolimit = lowestC[1] + (avrTR[1] * Multiplier)

        //if (close > hiLimit AND close > loLimit) THEN
        //     ret = hiLimit
        //ELSIF(close < loLimit AND close < hiLimit) THEN
        //     ret = loLimit
        //ELSE
        //     ret = ret[1]
        //ENDIF

        //RETURN ret coloured(238,238,0) as "Trend Trader"


        //https://usethinkscript.com/threads/trend-trader-strategy-for-thinkorswim.11643/
        //Ruby:
        //#////////////////////////////////////////////////////////////
        //#//  Copyright by HPotter v1.0 21/01/2021
        //#// This is plots the indicator developed by Andrew Abraham
        //#// in the Trading the Trend article of TASC September 1998
        //#////////////////////////////////////////////////////////////
        //# study(title="Trend Trader Strategy", overlay = true)
        //# ported by @bvaikunth 6/2022
        //# requested by @kls06541
        //input Length = 21;
        //input AtrMult = 3.0;
        //input AvgType = AverageType.simple;
        //def atr = MovingAverage(avgtype, TrueRange(high, close, low), 1);
        //def avgTR = WMA(atr, Length);
        //def highestC = Highest(high, Length);
        //def lowestC = Lowest(low, Length);
        //def UP = highestC[1] - (avgTR[1] * AtrMult);
        //def DN = lowestC[1] + (avgTR[1] * AtrMult);

        //script nz {
        //    input data = 0;
        //    input data2 = close;
        //    def ret_val = if IsNaN(data) then data else data2;
        //    plot return = ret_val;
        //}
        //def ret = if close > UP and close > DN then UP else if close < DN and close<UP then DN else nz(ret[1], close);
        //def pos = if close > ret then 1 else if close < ret then - 1 else nz(pos[1], 0);

        //plot trend = ret;

        //trend.assignvaluecolor( if pos == -1 then color.red else if pos == 1 then color.green else color.blue);


        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {

            if (exchange.SymbolListName.TryGetValue("NEBLUSDT", out CryptoSymbol? symbol))
            {
                if ((symbol.Quote.Equals("USDT")) && (symbol.Status == 1))
                {

                    CryptoIntervalPeriod intervalPeriod;



                    intervalPeriod = CryptoIntervalPeriod.interval1m;
                    if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
                        return;
                    LoadSymbolCandles(symbol, interval);
                    CryptoCandleList candles = symbol.GetSymbolInterval(intervalPeriod).CandleList;

                    List<EmaResult> ema200List = (List<EmaResult>)candles.Values.GetEma(200);
                    //List<EmaResult> ema100List = (List<EmaResult>)candles.Values.GetEma(100);
                    List<EmaResult> ema50List = (List<EmaResult>)candles.Values.GetEma(50);
                    List<RsiResult> RsiList = (List<RsiResult>)candles.Values.GetRsi();
                    List<StochResult> stochList = (List<StochResult>)Indicator.GetStoch(candles.Values, 14, 3, 1);
                    List<BollingerBandsResult> bbList = (List<BollingerBandsResult>)Indicator.GetBollingerBands(candles.Values, 20, 3);

                    GlobalData.AddTextToLogTab(symbol.Name);

                    long lastMelding = 0;
                    int length = 100;
                    decimal multiplier = 3.0m;
                    Period21 prevData = null;

                    List<CryptoCandle> viewPort = [];

                    for (int x = 0; x < candles.Count; x++)
                    {
                        CryptoCandle candle = candles.Values[x];
                        viewPort.Add(candle);

                        // Kopietje vanwege input richting wma
                        //CryptoCandle c = new()
                        //{
                        //    OpenTime = candle.OpenTime,
                        //    Close = candle.Close
                        //};
                        if (x < length)
                            continue;

                        // Hierdoor blijft een continue lijst van 21 candles (een beperkte view van 21 candles over het geheel)
                        // Qua test scheelt dit (maar in de echte wereld blijft het een 1 voor 1 aanpak)
                        viewPort.Remove(viewPort[0]);

                        // aanlooptijd indicatoren
                        if (x < 200)
                            continue;



                        // Bepaal de hoogste en laagste in een 21 candle interval (denk ik)
                        //highestC = ta.highest(Length)
                        //lowestC = ta.lowest(Length)
                        Period21 data = new();
                        foreach (CryptoCandle c2 in viewPort)
                        {
                            if ((decimal)c2.Low < data.Lowest)
                                data.Lowest = (decimal)c2.Low;
                            if ((decimal)c2.High > data.Highest)
                                data.Highest = (decimal)c2.High;
                        }


                        //avgTR = ta.wma(ta.atr(1), Length)
                        //?ehh? voorgaande atr, eigenlijk geen idee wat hier precies staat!
                        //Zou dat een wma van de laatste atr zijn? (anders weet ik het ook niet hoor)
                        List<AtrResult> atrList = (List<AtrResult>)viewPort.GetAtr(length);
                        data.AvgTr = (decimal)atrList[^1].Atr.Value;


                        // De voorgaande  waarden dus? bah
                        //hiLimit = highestC[1] - avgTR[1] * Multiplier
                        //loLimit = lowestC[1] + avgTR[1] * Multiplier
                        if (prevData != null)
                        {
                            decimal hiLimit = prevData.Highest - (prevData.AvgTr * multiplier);
                            decimal loLimit = prevData.Lowest + (prevData.AvgTr * multiplier);


                            //ret:= close > hiLimit and close > loLimit ? hiLimit : close < loLimit and close<hiLimit ? loLimit : nz(ret[1], close)
                            //     (                                   ) true       (condition                       )  true      false
                            //pos:= close > ret ? 1 : close < ret ? -1 : nz(pos[1], 0)
                            //if pos != pos[1] and pos == 1
                            //    alert("Color changed - Buy", alert.freq_once_per_bar_close)
                            //if pos != pos[1] and pos == -1
                            //    alert("Color changed - Sell", alert.freq_once_per_bar_close)
                            //barcolor(pos == -1 ? color.red : pos == 1 ? color.green : color.blue)
                            //plot(ret, color = color.new(color.blue, 0), title = 'Trend Trader Strategy')


                            // Pff, lekkere conditie hoor
                            //ret:= close > hiLimit and close > loLimit ? hiLimit : close < loLimit and close<hiLimit ? loLimit : nz(ret[1], close)
                            if ((candle.Close > hiLimit) && (candle.Close > loLimit))
                                data.Ret = hiLimit;
                            else if ((candle.Close < loLimit) && (candle.Close < hiLimit))
                                data.Ret = loLimit;
                            else
                                data.Ret = prevData.Ret;


                            ////pos:= close > ret ? 1 : close < ret ? -1 : nz(pos[1], 0)
                            //if (candle.Close > data.Ret)
                            //    data.Pos = 1m;
                            //else if (candle.Close < data.Ret)
                            //    data.Pos = -1m;
                            //else
                            //    data.Pos = prevData.Pos;


                            ////if pos != pos[1] and pos == 1
                            ////    alert("Color changed - Buy", alert.freq_once_per_bar_close)

                            //if ((data.Pos != prevData.Pos) && (data.Pos == 1))
                            //    //    alert("Color changed - Buy", alert.freq_once_per_bar_close)
                            //    data = data;

                            ////if pos != pos[1] and pos == -1
                            ////    alert("Color changed - Sell", alert.freq_once_per_bar_close)
                            //if ((data.Pos != prevData.Pos) && (data.Pos == -1))
                            //    //    alert("Color changed - Sell", alert.freq_once_per_bar_close)
                            //    data = data;


                            //GlobalData.AddTextToLogTab(string.Format("{0} {1}", candle.DateLocal.ToString(), data.Ret));




                            EmaResult ema50 = ema50List[x];
                            EmaResult ema200 = ema200List[x];
                            //BollingerBandsResult bbResult2; // = bbList[x];
                            if ((candle.Close > data.Ret) && (ema50.Ema.Value > ema200.Ema.Value))
                            {


                                // is in de afgelopen 25 candles er een oversold situatie geweest
                                // Of is er een Rsi oversold geweest

                                int count = 25;
                                for (int y = x; y > 0; y--)
                                {
                                    CryptoCandle lastCandle = candles.Values[y];

                                    float boundary;
                                    bool melding = true;

                                    if (melding)
                                    {
                                        // Stochastic Oscillator: &K en & D moeten kleiner zijn dan 20 %.
                                        StochResult stochResult = stochList[y];
                                        if (stochResult.Signal.Value > 20f)
                                            melding = false;
                                    }

                                    // Een piek-waarde onder de bollinger band (de CC methode)
                                    BollingerBandsResult bbResult = bbList[y];
                                    decimal lastCandleLowest = Math.Min(lastCandle.Open, lastCandle.Close);
                                    if (lastCandleLowest >= (decimal)bbResult.LowerBand)
                                        melding = false;

                                    if (melding)
                                    {
                                        if (lastMelding + interval.Duration != candle.OpenTime)
                                            GlobalData.AddTextToLogTab(string.Format("{0} {1} STOBB", candle.DateLocal.ToString(), data.Ret));
                                        break;
                                    }


                                    melding = true;
                                    boundary = 30f;
                                    RsiResult rsiResult = RsiList[y];
                                    if ((boundary > 0) && (rsiResult.Rsi >= boundary))
                                    {
                                        melding = false;
                                    }

                                    if (melding)
                                    {
                                        if (lastMelding + interval.Duration != candle.OpenTime)
                                            GlobalData.AddTextToLogTab(string.Format("{0} {1} RSI", candle.DateLocal.ToString(), data.Ret));
                                        break;
                                    }



                                    count--;
                                    if (count < 0)
                                        break;
                                }

                                lastMelding = candle.OpenTime;

                            }

                        }
                        prevData = data;

                    }
                }
            }
        }
    }


    private async Task Button3_Click_1Async(object? sender, EventArgs? e)
    {
        // Achtergrond hiervan:
        // Ik lijk STOB meldingen te missen in de JOEBTC chart (andere mensen hebben die melding wel gehad).
        // Maar de relealiteit in dit geval, bb.lower=0.24677 en candle.close=0.247
        // Dus technisch gezien was deze niet onder de BB en dus geen signaal
        // Blijft de vraag waarom andere scanner(s) deze wel tonen
        // (het bleek uiteindelijk wel een okay signaal qua buy/sell)
        // Dit is overigens een munt die naar een barcode chart neigt (0.42%)

        // Iets minder kritisch met vergelijkingen (>= is een > geworden)
        // (maar of dat met zoveel decimalen veel uitmaakt is de vraag)


        //GlobalData.Settings.Signal.AnalysisShowStobbOverbought = false;
        //GlobalData.Settings.Signal.AnalysisShowStobbOversold = true;

        GlobalData.Settings.Signal.Sbm.BBMinPercentage = 1.5;
        GlobalData.Settings.Signal.Sbm.BBMaxPercentage = 100.0;
        GlobalData.Settings.Signal.Stobb.BBMinPercentage = 1.5;
        GlobalData.Settings.Signal.Stobb.BBMaxPercentage = 5.0;

        GlobalData.Settings.Signal.AboveBollingerBandsSma = 0;
        GlobalData.Settings.Signal.AboveBollingerBandsUpper = 0;

        GlobalData.Settings.Signal.CandlesWithFlatPrice = 0;
        GlobalData.Settings.Signal.CandlesWithZeroVolume = 0;

        //GlobalData.Settings.Signal.AnalysisShowCandleJumpDown = false;
        //GlobalData.Settings.Signal.AnalysisShowCandleJumpUp = false;


        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue("WANBTC", out CryptoSymbol? symbol))
            {
                // Voor de SignalCreate moet ook de 1m geladen worden
                if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
                    return;
                LoadSymbolCandles(symbol, interval);


                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);

                //List<CryptoCandle> allCandles = symbolPeriod.CandleList.Values.ToList();
                //symbolPeriod.CandleList = new CryptoCandleList();
                CryptoCandleList candleList = symbolPeriod.CandleList;



                //var psar = Indicator.GetParabolicSar(candleList.Values);

                //public static partial class Extensions
                //    {
                //        public static List<decimal?> Sar(this List<Candle> source, double accelerationFactor = 0.02, double maximumAccelerationFactor = 0.2)
                //        {
                //            int outBegIdx, outNbElement;
                //            double[] sarValues = new double[source.Count];

                //            var highs = source.Select(x => Convert.ToDouble(x.High)).ToArray();
                //            var lows = source.Select(x => Convert.ToDouble(x.Low)).ToArray();

                //            var sar = TicTacTec.TA.Library.Core.Sar(0, source.Count - 1, highs, lows, 0.02, 0.2, out outBegIdx, out outNbElement, sarValues);

                //            if (sar == TicTacTec.TA.Library.Core.RetCode.Success)
                //            {
                //                // party
                //                return FixIndicatorOrdering(sarValues.ToList(), outBegIdx, outNbElement);
                //            }

                //            throw new Exception("Could not calculate SAR!");
                //        }
                //    }



                int count = -250;
                foreach (CryptoCandle candle in candleList.Values)
                {
                    //candleList.Add(candle.OpenTime, candle);

                    if (++count > 0)
                    {
                        //GlobalData.AddTextToLogTab(candle.OhlcText(symbol.Format) + " " + candle.Id.ToString());
                        SignalCreate createSignal = new(null, symbol, interval, CryptoTradeSide.Long, candle.OpenTime + 60);
                        await createSignal.AnalyzeAsync(candle.OpenTime);
                    }
                }

                GlobalData.AddTextToLogTab("Done..");

            }
        }

    }

    //private void Transform(ref point)
    //{
    //    int intWidth = 2 * 10 + (int)((hiX - loX) / 60);
    //    int intHeight = 2 * 10 + ((int)(n * hiY - n * loY));
    //}

    //Rectangle zoomTgtArea = new(300, 500, 200, 200);
    //Point zoomOrigin = Point.Empty;   // updated in MouseMove when button is pressed
    readonly float zoomFactor = 2f;

    private void PictureBox_Paint(object? sender, PaintEventArgs e)
    {
        // normal drawing
        //DrawStuff(e.Graphics);

        // for the movable zoom we want a small correction
        //Rectangle cr = pictureBox.ClientRectangle;
        //float pcw = cr.Width / (cr.Width - ZoomTgtArea.Width / 2f);
        //float pch = cr.Height / (cr.Height - ZoomTgtArea.Height / 2f);

        // now we prepare the graphics object; note: order matters!
        //e.Graphics.SetClip(zoomTgtArea);
        // we can either follow the mouse or keep the output area fixed:
        //if (cbx_fixed.Checked)
        //    e.Graphics.TranslateTransform(ZoomTgtArea.X - zoomCenter.X * zoomFactor,
        //                                    ZoomTgtArea.Y - zoomCenter.Y * zoomFactor);
        //else
        //    e.Graphics.TranslateTransform(-zoomCenter.X * zoomFactor * pcw,
        //                                    -zoomCenter.Y * zoomFactor * pch);
        // finally zoom
        e.Graphics.ScaleTransform(zoomFactor, zoomFactor);

        // and display zoomed
        //DrawStuff(e.Graphics);
    }


    private void CreateBarometerBitmap(CryptoQuoteData quoteData, CryptoInterval interval)
    {
        float blocks = 4;

        // pixel dimensies van het plaatje
        int intWidth = pictureBox1.Width;
        int intHeight = pictureBox1.Height;

        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if ((quoteData != null) && (exchange.SymbolListName.TryGetValue(Core.Const.Constants.SymbolNameBarometerPrice + quoteData.Name, out CryptoSymbol? symbol)))
            {
                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                CryptoCandleList candleList = symbolPeriod.CandleList;

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

                System.Drawing.Image bmp = new Bitmap(intWidth, intHeight);
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

                if (candleList.Values.Count > 0)
                {
                    CryptoCandle candle = candleList.Values[candleList.Values.Count - 1];
                    string text = CandleTools.GetUnixDate((long)candle.OpenTime + 60).ToLocalTime().ToString("HH:mm");

                    Font drawFont = new("Microsoft Sans Serif", this.Font.Size);
                    Rectangle rect = new(0, 0, intWidth, intHeight - 8);
                    TextRenderer.DrawText(g, text, drawFont, rect, Color.Black, Color.Transparent, TextFormatFlags.Bottom);
                }

                {
                    // experiment met 3 cirkels zoals Altrady
                    int y = 0;
                    int offset = 17; //40
                    int offsetValue = 37; //60
                                          //Rectangle rect1;
                                          //SolidBrush solidBrush;
                    Rectangle rect1 = new(0, y, intWidth, intHeight);
                    Font drawFont1 = new("Microsoft Sans Serif", this.Font.Size);
                    CryptoIntervalPeriod[] list = [CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1d];

                    foreach (CryptoIntervalPeriod intervalPeriod in list)
                    {
                        Color color;
                        BarometerData? barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(quoteData.Name, intervalPeriod);
                        if (barometerData?.PriceBarometer < 0)
                            color = Color.Red;
                        else
                            color = Color.Green;

                        y += 20;
                        //TextRenderer.DrawText(g, "1h", drawFont1, rect1, Color.Black, Color.Transparent, TextFormatFlags.Top);
                        SolidBrush solidBrush = new(color);
                        g.FillEllipse(solidBrush, offset, y, 15, 15);
                        rect1 = new(offsetValue, y, intWidth, intHeight);
                        TextRenderer.DrawText(g, barometerData?.PriceBarometer?.ToString("N2"), drawFont1, rect1, color, Color.Transparent, TextFormatFlags.Top);
                    }


                    //y += 20;
                    ////rect1 = new Rectangle(0, y, intWidth, intHeight);
                    ////TextRenderer.DrawText(g, "1H:", drawFont1, rect1, Color.Black, Color.Transparent, TextFormatFlags.Top);
                    //SolidBrush solidBrush = new SolidBrush(Color.FromArgb(255, 255, 0, 0));
                    //g.FillEllipse(solidBrush, offset, y, radius, radius);
                    //rect1 = new Rectangle(offsetValue, y, intWidth, intHeight);
                    //TextRenderer.DrawText(g, "1.29", drawFont1, rect1, Color.Green, Color.Transparent, TextFormatFlags.Top);



                    //y += 20;
                    ////rect1 = new Rectangle(0, y, intWidth, intHeight);
                    ////TextRenderer.DrawText(g, "4H:", drawFont1, rect1, Color.Black, Color.Transparent, TextFormatFlags.Top);
                    //solidBrush = new SolidBrush(Color.FromArgb(255, 0, 255, 0));
                    //g.FillEllipse(solidBrush, offset, y, radius, radius);
                    //rect1 = new Rectangle(offsetValue, y, intWidth, intHeight);
                    //TextRenderer.DrawText(g, "-0.29", drawFont1, rect1, Color.Red, Color.Transparent, TextFormatFlags.Top);



                    //y += 20;
                    ////rect1 = new Rectangle(0, y, intWidth, intHeight);
                    ////TextRenderer.DrawText(g, "1D:", drawFont1, rect1, Color.Black, Color.Transparent, TextFormatFlags.Top);
                    //solidBrush = new SolidBrush(Color.Red); //Color.FromArgb(255, 0, 0, 255)
                    //g.FillEllipse(solidBrush, offset, y, radius, radius);
                    //rect1 = new Rectangle(offsetValue, y, intWidth, intHeight);
                    //TextRenderer.DrawText(g, "-3.29", drawFont1, rect1, Color.Red, Color.Transparent, TextFormatFlags.Top);
                }




                //bmp.Save(@"e:\test.bmp");
                pictureBox1.ClientSize = new Size(intWidth, intHeight);
                Invoke((MethodInvoker)(() => { pictureBox1.Image = bmp; pictureBox1.Refresh(); }));


                ////MyImage = new Bitmap(fileToDisplay);
                ////pictureBox1.ClientSize = new Size(intWidth, intHeight);
                ////pictureBox1.Image = bmp;
                ////pictureBox1.Refresh();
            }
            else
            {

                System.Drawing.Image bmp = new Bitmap(intWidth, intHeight);
                Graphics g = Graphics.FromImage(bmp);

                Invoke((MethodInvoker)(() => { pictureBox1.Image = bmp; pictureBox1.Refresh(); }));
            }
        }
    }


    private void ButtonBitmap_Click(object? sender, EventArgs? e)
    {
        tabControl.SelectedTab = tabPageBitmap;

        // Dit is een poging om een grafiekje te maken van een munt

        //GlobalData.Settings.Signal.AnalysisShowStobbOverbought = false;
        //GlobalData.Settings.Signal.AnalysisShowStobbOversold = true;

        GlobalData.Settings.Signal.Sbm.BBMinPercentage = 1.5;
        GlobalData.Settings.Signal.Sbm.BBMaxPercentage = 100.0;
        GlobalData.Settings.Signal.Stobb.BBMinPercentage = 1.5;
        GlobalData.Settings.Signal.Stobb.BBMaxPercentage = 5.0;

        GlobalData.Settings.Signal.AboveBollingerBandsSma = 0;
        GlobalData.Settings.Signal.AboveBollingerBandsUpper = 0;

        GlobalData.Settings.Signal.CandlesWithFlatPrice = 0;
        GlobalData.Settings.Signal.CandlesWithZeroVolume = 0;

        //GlobalData.Settings.Signal.AnalysisShowCandleJumpDown = false;
        //GlobalData.Settings.Signal.AnalysisShowCandleJumpUp = false;


        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue(Core.Const.Constants.SymbolNameBarometerPrice + "USDT", out CryptoSymbol? symbol)) //"BTCUSDT"
            {
                //DateTime dateCandleStart = DateTime.SpecifyKind(new DateTime(2023, 03, 01, 05, 00, 0), DateTimeKind.Utc);
                //DateTime dateCandleEinde = DateTime.SpecifyKind(new DateTime(2023, 04, 02, 00, 00, 0), DateTimeKind.Utc);



                // Voor de SignalCreate moet ook de 1m geladen worden
                if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1h, out CryptoInterval? interval))
                    return;
                LoadSymbolCandles(symbol, interval); //, dateCandleStart, dateCandleEinde);

                CreateBarometerBitmap(symbol.QuoteData, interval);

                ////if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval3m, out interval))
                ////  return;
                ////LoadSymbolCandles(symbol, interval, dateCandleStart, dateCandleEinde);


                //CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);

                ////List<CryptoCandle> allCandles = symbolPeriod.CandleList.Values.ToList();
                ////symbolPeriod.CandleList = new CryptoCandleList();
                //CryptoCandleList candleList = symbolPeriod.CandleList;

                //// bepaal de range van de te tonen punten
                //long loX = long.MaxValue;
                //long hiX = long.MinValue;
                //float loY = float.MaxValue;
                //float hiY = float.MinValue;
                //foreach (CryptoCandle candle in candleList.Values)
                //{
                //    if (loX > candle.OpenTime)
                //        loX = candle.OpenTime;
                //    if (hiX < candle.OpenTime)
                //        hiX = candle.OpenTime;

                //    if (loY > (float)candle.Close)
                //        loY = (float)candle.Close;
                //    if (hiY < (float)candle.Close)
                //        hiY = (float)candle.Close;
                //}
                //if (loX == long.MaxValue)
                //    return;

                //// pixel dimensies van het plaatje
                //int intWidth = 150 * 2;
                //int intHeight = 50 * 2;

                //// range binnen punten
                //float screenX = hiX - loX; // unix time
                //float screenY = hiY - loY; // barometer, something like -5 .. +5
                //if (screenY < 5)
                //    screenY = 5f;

                //// factor to keep points within picture
                //float scaleX = intWidth / screenX;
                //float scaleY = intHeight / screenY;

                //// ofset to first point
                //float offsetX = 0; // start in the left of the picture
                //float offsetY = scaleY * 0.5f * screenY; // center of picture

                //// flix y (something specific with winform? what a crap)
                //scaleY = -1 * scaleY;

                //System.Drawing.Image bmp = new System.Drawing.Bitmap(intWidth, intHeight);
                //Graphics g = Graphics.FromImage(bmp);


                //// vertical lines (1% per line)
                //for (int y = -3; y <= 3; y++)
                //{
                //    PointF p1 = new PointF(0, offsetY + scaleY * y);
                //    PointF p2 = new PointF(intWidth, offsetY + scaleY * y);
                //    if (y == 0)
                //        g.DrawLine(Pens.Red, p1, p2);
                //    else
                //        g.DrawLine(Pens.Gray, p1, p2);
                //}

                //// horizontal lines (hours)
                //long intervalTime = 60 * 60; // * 1000;
                //long lastX = hiX - (hiX % intervalTime);
                //while (lastX > loX)
                //{
                //    PointF p1 = new PointF(0, 0);
                //    p1.X = offsetX + scaleX * (float)(lastX - loX);
                //    PointF p2 = new PointF(0, intHeight);
                //    p2.X = offsetX + scaleX * (float)(lastX - loX);

                //    DateTime ehh = CandleTools.GetUnixDate(lastX);
                //    //if (ehh.Hour % 2 == 0)
                //    //    g.DrawLine(Pens.Red, p1, p2);
                //    //else
                //    g.DrawLine(Pens.Gray, p1, p2);
                //    lastX -= intervalTime;
                //}


                //bool init = false;
                //PointF point1 = new PointF(0, 0);
                //PointF point2 = new PointF(0, 0);
                //for (int i = candleList.Values.Count - 1; i >= 0; i--)
                //{
                //    CryptoCandle candle = candleList.Values[i];

                //    point2.X = offsetX + scaleX * (float)(candle.OpenTime - loX);
                //    point2.Y = offsetY + scaleY * ((float)candle.Close);
                //    //GlobalData.AddTextToLogTab(candle.OhlcText(symbol.DisplayFormat) + " " + point2.X.ToString("N8") + " " + point2.Y.ToString("N8"));

                //    if (init)
                //        g.DrawLine(Pens.Red, point1, point2);

                //    point1 = point2;
                //    init = true;
                //}

                //Font drawFont = new Font(this.Font.Name, this.Font.Size); //Microsoft Sans Serif
                ////Font drawFontBold = new Font("Arial", 12, FontStyle.Bold);
                ////SolidBrush drawBrush = new SolidBrush(Color.Black);

                ////TextFormatFlags flags = TextFormatFlags.Bottom; // | TextFormatFlags.WordBreak;
                //g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit; 
                ////System.Drawing.Text.TextRenderingHint.AntiAlias;
                //TextRenderer.DrawText(g, "15:01", drawFont,
                //    new Rectangle(2, 0, intWidth, intHeight - 6), Color.Gray, Color.Transparent, TextFormatFlags.Bottom);
                ////SystemColors.ControlText, SystemColors.ControlDark

                ////g.DrawString("Hello", this.Font, 


                //bmp.Save(@"e:\test.bmp");


                ////MyImage = new Bitmap(fileToDisplay);
                ////pictureBox1.ClientSize = new Size(intWidth, intHeight);
                ////pictureBox1.Image = bmp;
                ////pictureBox1.Refresh();


            }
        }

    }

    private async void Button2_Click_1(object? sender, EventArgs? e)
    {


        await Task.Run(() =>
        {
            using SpeechSynthesizer synthesizer = new();
            {
                // to change VoiceGender and VoiceAge check out those links below
                synthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
                synthesizer.Volume = 100;  // (0 - 100)
                synthesizer.Rate = 0;     // (-10 - 10)
                                          //synthesizer.Speak("Now I'm speaking, no other function'll work"); // Synchronous
                synthesizer.Speak("Found a signal for BTCUSDT interval 1m"); // Asynchronous

            }
        }).ConfigureAwait(false);

    }

    //    private void Button1_Click_1(object? sender, EventArgs? e)
    //    {
    //        //        var message = new GelfMessage
    //        //        {
    //        //            ShortMessage = "Dit is een test logbericht",
    //        //            FullMessage = "Dit is een test logbericht met meer informatie",
    //        //            Facility = "C# App",
    //        //            Level = Gelf4Net.Level.Debug,
    //        //            Host = "localhost",
    //        //            Timestamp = DateTime.UtcNow,
    //        //            AdditionalFields =
    //        //{
    //        //    { "ApplicationName", "My C# App" },
    //        //    { "SomeOtherField", "Some value" }
    //        //}
    //        //        };

    //        //        using (var client = new GelfUdpClient("graylog-server-hostname", 12201))
    //        //        {
    //        //            client.Send(message);
    //        //        }

    //        /*

    //{


    //https://support.altrady.com/en/article/webhook-and-trading-view-signals-onbhbt/
    //{

    //"dca_orders": [
    //{
    //  "price_percentage": 0,
    //  "quantity_percentage": 0
    //}
    //],
    //}
    //}

    //         */


    //        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.altrady.com/v2/signal_bot_positions");
    //        httpWebRequest.ContentType = "application/json";
    //        httpWebRequest.Method = "POST";

    //        try
    //        {
    //            //"test": true,
    //            //"api_key": "string",
    //            //"api_secret": "string",
    //            //"side": "long",
    //            //"exchange": "string",
    //            //"symbol": "string",
    //            //"signal_price": 0,

    //            dynamic request = new JObject();
    //            request.test = true;
    //            request.api_key = "5c4b5f1c-ddde-4b59-983e-025aa90c4f30";
    //            request.api_secret = "8baab98b-e1dc-41d9-af22-f964185e82d6";
    //            request.side = "long";
    //            request.exchange = "binance";
    //            request.symbol = "btcusdt";
    //            request.signal_price = "22101.50";

    //            //"take_profit": [
    //            //{
    //            //"price_percentage": 0,
    //            //"position_percentage": 0
    //            //}
    //            //],
    //            dynamic take_ProfitList = new JArray(); // List<dynamic>();
    //            request.take_profit = take_ProfitList;

    //            dynamic take_Profit1 = new JObject();
    //            take_Profit1.price_percentage = 0.25;
    //            take_Profit1.position_percentage = 50;
    //            take_ProfitList.Add(take_Profit1);

    //            dynamic take_Profit2 = new JObject();
    //            take_Profit2.price_percentage = 0.50;
    //            take_Profit2.position_percentage = 50;
    //            take_ProfitList.Add(take_Profit2);

    //            //"stop_loss": 
    //            //{
    //            //"stop_percentage": 0,
    //            //"cool_down_amount": 0,
    //            //"cool_down_time_frame": "minute"
    //            //}

    //            dynamic stop_loss = new JObject();
    //            request.stop_loss = stop_loss;
    //            stop_loss.stop_percentage = 1.5;
    //            stop_loss.cool_down_amount = 0;
    //            stop_loss.cool_down_time_frame = "minute";

    //            string json = request.ToString();
    //            Console.WriteLine(json);

    //            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
    //            {
    //                streamWriter.Write(json);
    //                GlobalData.AddTextToLogTab(json);
    //            }


    //            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
    //            using var streamReader = new StreamReader(httpResponse.GetResponseStream());
    //            var result = streamReader.ReadToEnd();
    //            GlobalData.AddTextToLogTab(result);

    //            //string href = string.Format("https://api.altrady.com/v2/signal_bot_positions/");
    //            //webClient.UploadData(new Uri(href), downLoadFolder);
    //        }
    //        catch (Exception error)
    //        {
    //            ScannerLog.Logger.Error(error, "");
    //            GlobalData.AddTextToLogTab("error webhook " + error.ToString()); // symbol.Text + " " + 
    //        }
    //    }

    //static string Config(string what) => what switch
    //{
    //    "api_id" => "22959519",
    //    "api_hash" => "ab771edffa172f885dee0da9fcc8c9ec",
    //    "phone_number" => "+31624600002",
    //    "verification_code" => "10128",// Console.Write("Code: "); return Console.ReadLine();
    //    "first_name" => "Marius",// if sign-up is required
    //    "last_name" => "Doe",// if sign-up is required
    //    "password" => "?",// if user has enabled 2FA
    //    _ => null,// let WTelegramClient decide the default config
    //};

    //private static void CheckCandlesComplete(CryptoSymbol symbol, CryptoInterval interval)
    //{
    //SortedList<long, DateTime> dateList = new();
    //CryptoCandleList candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;

    //// Zijn de candles compleet?
    //if (candles.Count > 0)
    //{
    //    // De laatste niet meenemen (kan wellicht anders)
    //    for (int i = 0; i < candles.Values.Count - 1; i++)
    //    {
    //        CryptoCandle candle = candles.Values[i];
    //        long unix = candle.OpenTime + interval.Duration;

    //        if (!candles.ContainsKey(unix))
    //        {
    //            DateTime date; // = CandleTools.GetUnixDate(unix);
    //            long unixDay = CandleTools.GetUnixTime(unix, 1 * 24 * 60 * 60);
    //            date = CandleTools.GetUnixDate(unixDay);

    //            if (!dateList.ContainsKey(unixDay))
    //                dateList.Add(unixDay, date);
    //        }
    //    }

    //    // en dan maar hopen dat die lijst niet zo lang is
    //    if (dateList.Any())
    //    {
    //        string downLoadFolder = GlobalData.GetBaseDir();
    //        downLoadFolder += @"\backtest\Downloads\";
    //        Directory.CreateDirectory(downLoadFolder);

    //        foreach (long unix in dateList.Keys)
    //        {

    //            //hoofdpagina: //https://data.binance.vision/?prefix=data/spot/daily/klines/ACABUSD/1m/
    //            //downloadlink: https://data.binance.vision/data/spot/daily/klines/ACAUSDT/1m/ACAUSDT-1m-2022-12-02.zip                        
    //            DateTime date = CandleTools.GetUnixDate(unix);
    //            if (date == DateTime.Today)
    //                break;
    //            string name = symbol.Name + "-" + interval.Name + "-" + date.ToLocalTime().ToString("yyyy-MM-dd");
    //            GlobalData.AddTextToLogTab("Downloading " + name);

    //            using (WebClient webClient = new())
    //            {
    //                webClient.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
    //                webClient.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");

    //                string href = string.Format("https://data.binance.vision/data/spot/daily/klines/{0}/{1}/{2}.zip", symbol.Name, interval.Name, name);
    //                GlobalData.AddTextToLogTab("Downloading " + href);
    //                webClient.DownloadFile(new Uri(href), downLoadFolder + name + ".zip");
    //            }
    //            //ZipFile.CreateFromDirectory("source", "destination.zip", CompressionLevel.Optimal, false);

    //            // Extract the directory we just created.
    //            // ... Store the results in a new folder called "destination".
    //            // ... The new folder must not exist.
    //            System.IO.File.Delete(downLoadFolder + name + ".csv");
    //            ZipFile.ExtractToDirectory(downLoadFolder + name + ".zip", downLoadFolder);


    //            //dat is iets met (zie https://github.com/binance/binance-public-data/#trades-1)
    //            //    opentime,open,high,low,close,volume,closetime,Quote asset volume,Number of trades,Taker buy base asset volume, Taker buy quote asset volume, Ignore
    //            //    aantal?
    //            //    maar dan?
    //            //    quaote volume
    //            //    base volume?

    //            using CryptoDatabase databaseThread = new();
    //            databaseThread.Connection.Open();
    //            List<CryptoCandle> candleCache = new();

    //            using (var transaction = databaseThread.Connection.BeginTransaction())
    //            {
    //                using (StreamReader reader = System.IO.File.OpenText(downLoadFolder + name + ".csv"))
    //                {
    //                    string line;
    //                    while ((line = reader.ReadLine()) != null)
    //                    {
    //                        line = line.Trim();
    //                        if (line != "")
    //                        {
    //                            CryptoCandle candleTmp = new();
    //                            string[] items = line.Split(',');
    //                            candleTmp.OpenTime = long.Parse(items[0]) / 1000;
    //                            candleTmp.Open = decimal.Parse(items[1]);
    //                            candleTmp.High = decimal.Parse(items[2]);
    //                            candleTmp.Low = decimal.Parse(items[3]);
    //                            candleTmp.Close = decimal.Parse(items[4]);
    //                            candleTmp.SignalVolume = decimal.Parse(items[7]);
    //                            //6=closetime
    //                            //7=Quote asset volume (wellicht deze?)
    //                            //SaveCandle?

    //                            // Vul het aan met andere attributen
    //                            CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, candleTmp.Date,
    //                                candleTmp.Open, candleTmp.High, candleTmp.Low, candleTmp.Close, candleTmp.SignalVolume, false);
    //                            candleCache.Add(candle);
    //                        }

    //                        if (candleCache.Count > 500)
    //                        {
    //                            // dit is nog voor mssql zie ik..
    //                            databaseThread.BulkInsertCandles(candleCache, transaction);
    //                            candleCache.Clear();
    //                        }
    //                        //static public void BulkInsertCandles(this SqlConnection MyConnection, List<CryptoCandle> cache, SqlTransaction transaction)
    //                    }
    //                }
    //                System.IO.File.Delete(downLoadFolder + name + ".csv");
    //                System.IO.File.Delete(downLoadFolder + name + ".zip");

    //                if (candleCache.Any())
    //                {
    //                    // dit is nog voor mssql zie ik..
    //                    databaseThread.BulkInsertCandles(candleCache, transaction);
    //                    candleCache.Clear();
    //                }

    //                transaction.Commit();
    //            }

    //        }
    //    }
    //}
    //}

    public static bool AcceptSymbol(CryptoSymbol symbol, CryptoInterval interval, CryptoBackConfig config)
    {
        CryptoCandleList candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;

        // munten met een hele lage Satoshi waardering (1 Satoshi = 1E-8)
        CryptoCandle candle = candles.Values.Last();
        if (candle.Open < config.PriceLimit)
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} price to low {2:N3}", DateTime.Now.ToLocalTime(), symbol.Name, candle.Open));
            return false;

        }

        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
        decimal diff = 100 * (symbol.PriceTickSize) / candle.Open;
        if (diff > config.MinPercentagePriceTickSize)
        {
            GlobalData.AddTextToLogTab(string.Format("{0} {1} tick size percentage to high {2:N3}", DateTime.Now.ToLocalTime(), symbol.Name, diff));
            return false;
        }

        return true;
    }


    //private async Task BackTest(string algorithm, CryptoSymbol symbol, CryptoInterval interval, CryptoBackConfig config, string baseFolder)
    // {
    //GlobalData.AddTextToLogTab(string.Format("{0} {1} start---", DateTime.Now.ToLocalTime(), symbol.Name));

    //CryptoInterval interval1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).Interval;

    //// De symbol candles inlezen en controleren
    //ProcessPositionSemaphore.Wait();
    //try
    //{
    //    // Altijd 1m inlezen (de basis voor de emulator, blij wordt ik er niet van, maar zo werkt het systeem nu eenmaal, voila!)
    //    LoadSymbolCandles(symbol, interval1m, config.DateStart, config.DateEinde);

    //    CryptoCandleList candles = LoadSymbolCandles(symbol, interval, config.DateStart, config.DateEinde);
    //    if (candles.Count == 0)
    //    {
    //        GlobalData.AddTextToLogTab(string.Format("{0} {1} no candles", DateTime.Now.ToLocalTime(), symbol.Name));
    //        return;
    //    }

    //    CheckCandlesComplete(symbol, interval);

    //    // Extra - voor het bepalen van de 5m Flux indicatie
    //    CryptoInterval interval5m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m).Interval;
    //    LoadSymbolCandles(symbol, interval5m, config.DateStart, config.DateEinde);

    //    // Extra - voor het bepalen van de 24 Effectieve Change
    //    CryptoInterval interval15m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval15m).Interval;
    //    LoadSymbolCandles(symbol, interval15m, config.DateStart, config.DateEinde);

    //    // Extra - voor het bepalen of de munt te nieuw is
    //    CryptoInterval interval1d = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).Interval;
    //    LoadSymbolCandles(symbol, interval1d, config.DateStart.AddDays(-(GlobalData.Settings.Signal.SymbolMustExistsDays + 10)), config.DateEinde);
    //}
    //finally
    //{
    //    ProcessPositionSemaphore.Release();
    //}



    //// symbols met een lage Satoshi waardering (1 Satoshi = 1E-8) uitsluiten
    //// En munten waarvan de ticksize percentage te groot is (barcode charts)
    //if (!AcceptSymbol(symbol, interval, config))
    //    return;


    //// cooldown..
    //symbol.LastTradeDate = null;

    //SignalCreateBase backTestAlgorithm = null;
    //BackTest.BackTest backTest = new(symbol, interval1m, interval, config);
    //foreach (AlgorithmDefinition def in TradingConfig.Trading[CryptoTradeSide.Long].Strategy)
    //{
    //    if (algorithm.Equals(def.Name))
    //    {
    //        backTestAlgorithm = def.InstantiateAnalyzeLong(symbol, interval1m, null);
    //        break;
    //    }
    //}
    //if (backTestAlgorithm == null)
    //{
    //    GlobalData.AddTextToLogTab("Algoritme niet gedefinieerd");
    //    return;
    //}

    //await backTest.Execute(backTestAlgorithm, baseFolder);

    //if (config.ReleaseCandles)
    //    backTest.ClearCandles();



    //// Omdat er meer threads bezig zijn moet de queue gelocked worden
    //Monitor.Enter(Log);
    //try
    //{
    //    Results.Add(backTest.Results);

    //    Log.AppendLine(backTest.Outcome);
    //    GlobalData.AddTextToLogTab(DateTime.Now.ToLocalTime() + " " + backTest.Outcome);

    //    // report
    //    string s = Log.ToString();
    //    System.IO.File.WriteAllText(baseFolder + "Overview-" + interval.Name + ".txt", s);
    //}
    //finally
    //{
    //    Monitor.Exit(Log);
    //}
    //}


    private async Task BackTestAsync()
    {
        string algorithm = ""; //string algorithm, 
        Invoke((MethodInvoker)(() => algorithm = comboBox1.Text));

        // TODO: Zorgen dat alleen het gekozen interval en algoritme actief is in de instellingen

        createdSignalCount = 0;
        GlobalData.LoadSettings();

        CryptoBackConfig config = new();
        LoadConfig(ref config);
        if (!GlobalData.IntervalListPeriod.TryGetValue(config.IntervalPeriod, out CryptoInterval interval))
            return;

        //SignalCreate.AnalyseNotificationList.Clear();
        GlobalData.ActiveAccount.Data.PositionList.Clear();

        // Pittige configuratie geworden zie ik ;-)
        GlobalData.Settings.Signal.Active = true;
        GlobalData.Settings.Signal.Long.Interval.Clear();
        GlobalData.Settings.Signal.Long.Interval.Add(interval.Name);

        GlobalData.Settings.Signal.Long.Strategy.Clear();
        GlobalData.Settings.Signal.Long.Strategy.Clear();
        GlobalData.Settings.Signal.Long.Strategy.Add("sbm1");
        GlobalData.Settings.Signal.Long.Strategy.Add("sbm2");
        GlobalData.Settings.Signal.Long.Strategy.Add("sbm3");
        GlobalData.Settings.Signal.Long.Strategy.Add("sbm4");
        GlobalData.Settings.Signal.Long.Strategy.Add("flux");
        GlobalData.Settings.Signal.Long.Strategy.Add("stob");
        //GlobalData.Settings.Signal.ScanForNew.Strategy[CryptoOrderSide.Buy].Add(algorithm);


        GlobalData.Settings.Trading.Active = true;
        GlobalData.Settings.Trading.Long.Interval.Clear();
        GlobalData.Settings.Trading.Long.Interval.Add(interval.Name);

        GlobalData.Settings.Trading.Long.Strategy.Clear();
        GlobalData.Settings.Trading.Long.Strategy.Add("sbm1");
        GlobalData.Settings.Trading.Long.Strategy.Add("sbm2");
        GlobalData.Settings.Trading.Long.Strategy.Add("sbm3");
        GlobalData.Settings.Trading.Long.Strategy.Add("sbm4");
        GlobalData.Settings.Trading.Long.Strategy.Add("lux");
        GlobalData.Settings.Trading.Long.Strategy.Add("stob");
        //GlobalData.Settings.Trading.Monitor.Strategy[CryptoOrderSide.Buy].Add(algorithm);

        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();


        GlobalData.Settings.General.SoundTradeNotification = false;
        GlobalData.Settings.General.SoundTradeNotification = false;

        GlobalData.BackTest = true;
        GlobalData.Settings.Trading.TradeVia = CryptoAccountType.PaperTrade;

        // Instap
        GlobalData.Settings.Trading.CheckIncreasingRsi = false;
        GlobalData.Settings.Trading.CheckIncreasingMacd = false;
        GlobalData.Settings.Trading.CheckIncreasingStoch = false;

        // BUY
        GlobalData.Settings.Trading.EntryStrategy = CryptoEntryOrDcaStrategy.AfterNextSignal;
        GlobalData.Settings.Trading.GlobalBuyCooldownTime = 20;
        GlobalData.Settings.Trading.EntryOrderPrice = CryptoEntryOrDcaPricing.MarketPrice;

        // DCA
        //GlobalData.Settings.Trading.DcaPercentage = 2m;
        GlobalData.Settings.Trading.DcaOrderPrice = CryptoEntryOrDcaPricing.SignalPrice;
        GlobalData.Settings.Trading.DcaStrategy = CryptoEntryOrDcaStrategy.FixedPercentage;

        // TP
        GlobalData.Settings.Trading.ProfitPercentage = 0.75m;
        //GlobalData.Settings.Trading.DynamicTpPercentage = 0.75m;
        GlobalData.Settings.Trading.TakeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage;
        //GlobalData.Settings.Trading.LockProfits = true;

        StringBuilder samenvatting = new();
        //for (int macdCandles = 2; macdCandles <= 2; macdCandles++)
        {

            //GlobalData.Settings.Signal.MacdCandles = macdCandles;
            //samenvatting.AppendLine();
            //samenvatting.AppendLine(macdCandles.ToString());

            //for (BuyPriceStrategy strategy = BuyPriceStrategy.MarketOrder; strategy <= BuyPriceStrategy.BollingerBands; strategy++)
            //for (BuyPriceStrategy strategy = BuyPriceStrategy.MarketOrder; strategy <= BuyPriceStrategy.MarketOrder; strategy++)
            {
                BuyPriceStrategy strategy = config.BuyPriceStrategy;
                //config.BuyPriceStrategy = strategy;


                Results = new(config.QuoteMarket, null, interval, config);

                Log = new();
                Results.ShowHeader(Log);

                // Ook naar beeldscherm
                StringBuilder header = new();
                Results.ShowHeader(header, false);
                GlobalData.AddTextToLogTab(header.ToString());

                var exchange = GlobalData.Settings.General.Exchange;
                if (exchange != null)
                {
                    string baseFolder = GlobalData.GetBaseDir();
                    baseFolder += @"\backtest\" + exchange.Name + @"\" + strategy.ToString() + @"\";
                    Directory.CreateDirectory(baseFolder);

                    // De symbols van/voor de pauseer regels inlezen
                    foreach (Core.Settings.PauseTradingRule rule in GlobalData.Settings.Trading.PauseTradingRules)
                    {
                        if (exchange.SymbolListName.TryGetValue(rule.Symbol, out CryptoSymbol? symbolX))
                        {
                            if (GlobalData.IntervalListPeriod.TryGetValue(rule.Interval, out CryptoInterval? intervalX))
                            {
                                LoadSymbolCandles(symbolX, intervalX); //, config.DateStart, config.DateEinde);
                            }
                        }
                    }

                    List<string> quoteList = [];
                    Queue<CryptoSymbol> queue = new();
                    string filter = "," + config.SymbolFilter + ",";
                    foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                    {
                        if (symbol.QuoteData.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol() && symbol.IsSpotTradingAllowed)
                        {
                            if (symbol.Quote.Equals(config.QuoteMarket) && symbol.Volume >= config.VolumeLimit)
                            {
                                if (config.SymbolFilter == "" || filter.Contains("," + symbol.Base + ","))
                                {
                                    if (!quoteList.Contains(symbol.Quote))
                                        quoteList.Add(symbol.Quote);

                                    queue.Enqueue(symbol);
                                }
                            }
                        }
                    }

                    // De relevante barometer inlezen en niet alleen de USDT!

                    // Inlezen barometers
                    foreach (string quote in quoteList)
                    {
                        if (exchange.SymbolListName.TryGetValue("$BMP" + quote, out CryptoSymbol? symbol))
                        {
                            foreach (CryptoInterval intervalX in GlobalData.IntervalListPeriod.Values)
                            {
                                if (intervalX.IntervalPeriod == CryptoIntervalPeriod.interval15m)
                                    LoadSymbolCandles(symbol, intervalX); //, config.DateStart, config.DateEinde);
                                if (intervalX.IntervalPeriod == CryptoIntervalPeriod.interval30m)
                                    LoadSymbolCandles(symbol, intervalX); //, config.DateStart, config.DateEinde);
                                if (intervalX.IntervalPeriod == CryptoIntervalPeriod.interval1h)
                                    LoadSymbolCandles(symbol, intervalX); //, config.DateStart, config.DateEinde);
                                if (intervalX.IntervalPeriod == CryptoIntervalPeriod.interval4h)
                                    LoadSymbolCandles(symbol, intervalX); //, config.DateStart, config.DateEinde);
                                if (intervalX.IntervalPeriod == CryptoIntervalPeriod.interval1d)
                                    LoadSymbolCandles(symbol, intervalX); //, config.DateStart, config.DateEinde);
                            }
                        }
                    }

                    // En door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 3)
                    {
                        Task task = Task.Run(() =>
                        {
                            //BackTest(barometer, queue, interval, config, baseFolder);
                            //private void BackTest(CryptoSymbol barometer, Queue<CryptoSymbol> queue, CryptoInterval interval, CryptoBackConfig config, string baseFolder)
                            {
                                try
                                {
                                    // We hergebruiken de client binnen deze thread, teveel connecties opnenen resulteerd in een foutmelding:
                                    // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
                                    using BinanceRestClient client = new();
                                    {
                                        while (true)
                                        {
                                            CryptoSymbol symbol;

                                            // Omdat er meer threads bezig zijn moet de queue gelocked worden
                                            Monitor.Enter(queue);
                                            try
                                            {
                                                if (queue.Count > 0)
                                                    symbol = queue.Dequeue();
                                                else
                                                    break;
                                            }
                                            finally
                                            {
                                                Monitor.Exit(queue);
                                            }

                                            //symbol.TradeList.Clear();
                                            //await BackTest(algorithm, symbol, interval, config, baseFolder);
                                        }
                                    }
                                }
                                catch (Exception error)
                                {
                                    ScannerLog.Logger.Error(error, "");
                                    GlobalData.AddTextToLogTab("error back testing " + error.ToString()); // symbol.Text + " " + 
                                }
                            }
                        });
                        taskList.Add(task);
                    }
                    await Task.WhenAll(taskList).ConfigureAwait(false);


                    Results.ShowFooter(Log);

                    decimal percentage = 0m;
                    if (Results.Invested != 0m)
                        percentage = 100 * (Results.Returned - Results.Commission) / Results.Invested;

                    samenvatting.AppendLine(string.Format("{0} {1} {2} {3}", ((int)strategy).ToString(), strategy.ToString(), Results.Invested.ToString("N2"), percentage.ToString("N2")));
                    //samenvatting.AppendLine(strategy.ToString());
                    //samenvatting.AppendLine(Results.Invested.ToString());
                    //samenvatting.AppendLine(percentage.ToString("N2"));
                    //Results.ShowFooter(samenvatting);

                    // Ook naar beeldscherm
                    StringBuilder footer = new();
                    Results.ShowFooter(footer);
                    GlobalData.AddTextToLogTab(footer.ToString());
                    GlobalData.AddTextToLogTab("");
                    GlobalData.AddTextToLogTab(DateTime.Now.ToLocalTime() + " done...");

                    // report
                    string s = Log.ToString();
                    System.IO.File.WriteAllText(baseFolder + "Overview-" + interval.Name + ".txt", s);
                }

            }
            GlobalData.AddTextToLogTab(samenvatting.ToString());
        }

    }

    private void ButtonBackTest_Click(object? sender, EventArgs? e)
    {
        Task task = Task.Run(BackTestAsync);
        task.Start();
    }



    private void Button1_Click(object? sender, EventArgs? e)
    {
        tabControl.SelectedTab = tabPage1;

        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            int i = 0;
            foreach (CryptoSymbol symbol in exchange.SymbolListId.Values)
            {
                CryptoSignal signal = new()
                {
                    OpenDate = DateTime.UtcNow.AddHours(SignalList.Count),
                    SignalPrice = 0.12345m,
                    Symbol = symbol,
                    Exchange = exchange,
                    Interval = GlobalData.IntervalList[3],
                    Candle = null,
                };
                if (i % 2 == 0)
                    signal.Side = CryptoTradeSide.Long;
                else
                    signal.Side = CryptoTradeSide.Short;

                if (i % 2 == 0)
                    signal.Interval = GlobalData.IntervalList[0];
                else
                    signal.Interval = GlobalData.IntervalList[1];

                if (i % 2 == 0)
                    signal.Strategy = CryptoSignalStrategy.Sbm1;
                else
                    signal.Strategy = CryptoSignalStrategy.Stobb;

                if (i % 2 == 0)
                    signal.Symbol.LastPrice = signal.SignalPrice - 1;
                else
                    signal.Symbol.LastPrice = signal.SignalPrice + 1;

                signal.Last24HoursChange = 12345.12;
                signal.Last24HoursEffective = 82345.12;

                signal.Rsi = 12.12;
                signal.Sma200 = 11.11;
                signal.EventText = "Hello";
                signal.Sma50 = 10.09;
                signal.Sma20 = 9.08;
                signal.StochSignal = 70.03;
                signal.StochOscillator = 71.05;
                SignalList.Add(signal);

                symbol.Volume = SignalList.Count * 12345678.01m;
                signal.SignalVolume = symbol.Volume;
                signal.TrendIndicator = CryptoTrendIndicator.Bearish;
                signal.TrendPercentage = 85.85f;
                symbol.FundingRate = 0.0001m;
                SymbolList.Add(symbol);
                i++;
            }
        }

        GridSignals.AdjustObjectCount();
        GridSignals.ApplySorting();

        GridSymbols.AdjustObjectCount();
        GridSymbols.ApplySorting();
    }


    private void Button2_Click_2(object? sender, EventArgs? e)
    {
        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue("ADAUSDT", out CryptoSymbol? symbol))
            {
                CryptoSignal signal = new()
                {
                    OpenDate = DateTime.UtcNow.AddHours(SignalList.Count),
                    SignalPrice = 0.12345m,
                    Symbol = symbol,
                    Exchange = exchange,
                    Side = CryptoTradeSide.Long,
                    Interval = GlobalData.IntervalList[3],
                    Candle = null,
                };
                SignalList.Add(signal);
            }

            if (exchange.SymbolListName.TryGetValue("LEVERUSDT", out symbol))
            {
                CryptoSignal signal = new()
                {
                    OpenDate = DateTime.UtcNow.AddHours(SignalList.Count),
                    SignalPrice = 0.12345m,
                    Symbol = symbol,
                    Exchange = exchange,
                    Interval = GlobalData.IntervalList[5],
                    Side = CryptoTradeSide.Short,
                    Candle = null,
                };
                SignalList.Add(signal);
            }

            if (exchange.SymbolListName.TryGetValue("ABCUSDT", out symbol))
            {
                CryptoSignal signal = new()
                {
                    OpenDate = DateTime.UtcNow.AddHours(SignalList.Count),
                    SignalPrice = 0.12345m,
                    Symbol = symbol,
                    Exchange = exchange,
                    Side = CryptoTradeSide.Long,
                    Interval = GlobalData.IntervalList[3],
                    Candle = null,
                };
                SignalList.Add(signal);
            }

            if (exchange.SymbolListName.TryGetValue("ACAUSDT", out symbol))
            {
                CryptoSignal signal = new()
                {
                    OpenDate = DateTime.UtcNow.AddHours(SignalList.Count),
                    SignalPrice = 0.12345m,
                    Symbol = symbol,
                    Exchange = exchange,
                    Interval = GlobalData.IntervalList[5],
                    Side = CryptoTradeSide.Short,
                    Candle = null,
                };
                SignalList.Add(signal);
            }
        }

        GridSignals.AdjustObjectCount();
    }

    private void Button3_Click_2(object? sender, EventArgs? e)
    {
        //GridSignals.UpdatePriceDifferences();
    }

    private void Button4_Click(object? sender, EventArgs? e)
    {
        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue("PAXGUSDT", out CryptoSymbol? symbol))
            {
                CryptoSignal signal = new()
                {
                    OpenDate = DateTime.UtcNow.AddHours(SignalList.Count),
                    SignalPrice = 0.12345m,
                    Symbol = symbol,
                    Exchange = exchange,
                    Side = CryptoTradeSide.Long,
                    Interval = GlobalData.IntervalList[3],
                    Candle = null,
                };
                SignalList.Insert(0, signal);
            }

            GridSignals.AdjustObjectCount();
        }
    }


}
