using System.Drawing.Drawing2D;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.TradingView;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Intern;
using CryptoScanBot.Core.Barometer;

namespace CryptoScanBot.TradingView;


public partial class DashBoardInformation : UserControl
{
    private class InformationRow
    {
        public Label SymbolName { get; set; }
        public Label SymbolPrice { get; set; }
        public Label SymbolVolume { get; set; }

        public Label Symbol2Name { get; set; }
        public Label Symbol2Value { get; set; }
        public SymbolValue Symbol2Object { get; set; }

        public Label InfoName { get; set; }
        public Label InfoValue { get; set; }
    }

    private class SymbolHist
    {
        public string Symbol = "";
        public decimal? PricePrev = 0m;
        public decimal VolumePrev = 0m;
    }

    private static int BarometerLastMinute = 0;
    private readonly List<SymbolHist> SymbolHistList = new(5);
    private readonly List<InformationRow> InformationRowList = new(5);

    public DashBoardInformation()
    {
        InitializeComponent();

        // Wat event handles zetten
        EditBarometerQuote.SelectedIndexChanged += ShowBarometerStuff;
        EditBarometerInterval.SelectedIndexChanged += ShowBarometerStuff;

        // De 5 items toevoegen
        for (int i = 0; i <= 4; i++)
            SymbolHistList.Add(new SymbolHist());


        InformationRowList.Add(new()
        {
            SymbolName = labelSymbol1Name,
            SymbolPrice = labelSymbol1Price,
            SymbolVolume = labelSymbol1Volume,

            Symbol2Name = label16,
            Symbol2Value = label21,
            Symbol2Object = GlobalData.TradingViewDollarIndex,

            InfoName = label26,
            InfoValue = label31,
        }
        );

        InformationRowList.Add(new()
        {
            SymbolName = labelSymbol2Name,
            SymbolPrice = label7,
            SymbolVolume = label12,

            Symbol2Name = label17,
            Symbol2Value = label22,
            Symbol2Object = GlobalData.TradingViewBitcoinDominance,

            InfoName = label27,
            InfoValue = label32,
        });

        InformationRowList.Add(new()
        {
            SymbolName = labelSymbol3Name,
            SymbolPrice = label8,
            SymbolVolume = label13,

            Symbol2Name = label18,
            Symbol2Value = label23,
            Symbol2Object = GlobalData.TradingViewSpx500,

            InfoName = label28,
            InfoValue = label33,
        });

        InformationRowList.Add(new()
        {
            SymbolName = labelSymbol4Name,
            SymbolPrice = label9,
            SymbolVolume = label14,

            Symbol2Name = label19,
            Symbol2Value = label24,
            Symbol2Object = GlobalData.TradingViewMarketCapTotal,

            InfoName = label29,
            InfoValue = label34,
        });

        InformationRowList.Add(new()
        {
            SymbolName = labelSymbol5Name,
            SymbolPrice = label10,
            SymbolVolume = label15,

            Symbol2Name = label20,
            Symbol2Value = label25,
            Symbol2Object = GlobalData.FearAndGreedIndex,

            InfoName = label30,
            InfoValue = label35,
        });


        // Initieel alles even leeg maken en de OnClick zetten
        labelBm1h.Text = "";
        labelBm4h.Text = "";
        labelBm1d.Text = "";
        labelBmTime.Text = "";

        foreach (var x in InformationRowList)
        {
            x.SymbolName.Text = "";
            x.SymbolName.BackColor = Color.Transparent;
            x.SymbolName.Click += ListViewInformation_DoubleClick;
            x.SymbolPrice.Text = "";
            x.SymbolPrice.BackColor = Color.Transparent;
            x.SymbolPrice.Click += ListViewInformation_DoubleClick;
            x.SymbolVolume.Text = "";
            x.SymbolVolume.BackColor = Color.Transparent;
            x.SymbolVolume.Click += ListViewInformation_DoubleClick;

            x.Symbol2Name.Text = "";
            x.Symbol2Name.BackColor = Color.Transparent;
            x.Symbol2Name.Click += ListViewInformation_DoubleClick;
            x.Symbol2Value.Text = "";
            x.Symbol2Value.BackColor = Color.Transparent;
            x.Symbol2Value.Click += ListViewInformation_DoubleClick;

            x.InfoName.Text = "";
            x.InfoName.BackColor = Color.Transparent;
            x.InfoName.Click += ListViewInformation_DoubleClick;
            x.InfoValue.Text = "";
            x.InfoValue.BackColor = Color.Transparent;
            x.InfoValue.Click += ListViewInformation_DoubleClick;
        }
    }


    public void PickupBarometerProperties()
    {
        GlobalData.Settings.General.SelectedBarometerQuote = EditBarometerQuote.Text;
        GlobalData.Settings.General.SelectedBarometerInterval = EditBarometerInterval.Text;
    }


    public void InitializeBarometer()
    {
        // De Editen bijwerken
        EditBarometerQuote.BeginUpdate();
        EditBarometerInterval.BeginUpdate();
        try
        {
            EditBarometerQuote.SelectedIndexChanged -= ShowBarometerStuff;
            EditBarometerInterval.SelectedIndexChanged -= ShowBarometerStuff;

            // Enkel de actieve quotes erin zetten (default=usdt)
            EditBarometerQuote.Items.Clear();
            foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (cryptoQuoteData.FetchCandles)
                    EditBarometerQuote.Items.Add(cryptoQuoteData.Name);
            }
            if (EditBarometerQuote.Items.Count == 0)
                EditBarometerQuote.Items.Add("USDT");

            try { EditBarometerQuote.SelectedIndex = EditBarometerQuote.Items.IndexOf(GlobalData.Settings.General.SelectedBarometerQuote); } catch { }
            if (EditBarometerQuote.SelectedIndex < 0)
                EditBarometerQuote.SelectedIndex = 0;


            // De intervallen in de combox zetten (default=1h)
            EditBarometerInterval.Items.Clear();
            EditBarometerInterval.Items.Add("1H");
            EditBarometerInterval.Items.Add("4H");
            EditBarometerInterval.Items.Add("1D");

            try { EditBarometerInterval.SelectedIndex = EditBarometerInterval.Items.IndexOf(GlobalData.Settings.General.SelectedBarometerInterval); } catch { }
            if (EditBarometerInterval.SelectedIndex < 0)
                EditBarometerInterval.SelectedIndex = 0;
        }
        finally
        {
            EditBarometerQuote.SelectedIndexChanged += ShowBarometerStuff;
            EditBarometerInterval.SelectedIndexChanged += ShowBarometerStuff;

            EditBarometerInterval.EndUpdate();
            EditBarometerQuote.EndUpdate();
        }
    }


    public void CreateBarometerBitmap(Core.Model.CryptoExchange exchange, CryptoQuoteData quoteData, CryptoInterval interval)
    {
        float blocks = 6;

        // pixel dimensies van het plaatje
        int intWidth = pictureBox1.Width;
        int intHeight = pictureBox1.Height;

        if (quoteData != null && exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice + quoteData.Name, out CryptoSymbol? symbol))
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
                
                // In Bybit Futures zijn er (vanwege storingen) hoge waarden ingevuld
                if (candle.Close > -50 && candle.Close < 50)
                {
                    if (loY > (float)candle.Close)
                        loY = (float)candle.Close;
                    if (hiY < (float)candle.Close)
                        hiY = (float)candle.Close;
                }
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
                Rectangle rect = new(0, 0, 25, intHeight);
                SolidBrush solidBrush = new(this.BackColor);
                g.FillRectangle(solidBrush, rect);
            }

            // Barometer met daarin de 3 cirkels ter indicatie
            {
                //int y = 4;
                //int offset = (int)(intWidth / 2) - 15;
                //int offsetValue = 20;
                //Pen blackPen = new Pen(Color.Black, 1);
                //Rectangle rect1 = new(0, 0, intWidth, intHeight);
                //Font drawFont1 = new(this.Font.Name, this.Font.Size);s
                CryptoIntervalPeriod[] list = { CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1d };

                foreach (CryptoIntervalPeriod intervalPeriod in list)
                {

                    Color color;
                    BarometerData? barometerData = GlobalData.ActiveAccount.Data.GetBarometer(quoteData.Name, intervalPeriod);
                    if (barometerData?.PriceBarometer < 0)
                        color = Color.Red;
                    else
                        color = Color.Green;

                    //TextRenderer.DrawText(g, "1h", drawFont1, rect1, Color.Black, Color.Transparent, TextFormatFlags.Top);
                    //SolidBrush solidBrush = new(color);
                    //g.FillEllipse(solidBrush, offset, y, 14, 14);

                    //rect1 = new Rectangle(offsetValue, y, intWidth, intHeight);
                    //TextRenderer.DrawText(g, barometerData?.PriceBarometer?.ToString("N2"), drawFont1, rect1, color, Color.Transparent, TextFormatFlags.Top);
                    //y += 19;
                    //offset += 19;

                    string text = barometerData?.PriceBarometer?.ToString("N2");
                    if (intervalPeriod == CryptoIntervalPeriod.interval1h)
                        Invoke((MethodInvoker)(() => { labelBm1h.Text = text + " 1h"; labelBm1h.ForeColor = color; }));
                    else if (intervalPeriod == CryptoIntervalPeriod.interval4h)
                        Invoke((MethodInvoker)(() => { labelBm4h.Text = text + " 4h"; labelBm4h.ForeColor = color; }));
                    else if (intervalPeriod == CryptoIntervalPeriod.interval1d)
                        Invoke((MethodInvoker)(() => { labelBm1d.Text = text + " 1d"; labelBm1d.ForeColor = color; }));
                }

            }

            // Barometer tijd
            if (candleList.Values.Count > 0)
            {
                CryptoCandle candle = candleList.Values[candleList.Values.Count - 1];
                //Rectangle rect = new(6, 0, intWidth, intHeight - 8);
                string text = CandleTools.GetUnixDate((long)candle.OpenTime + 60).ToLocalTime().ToString("HH:mm");

                //Font drawFont = new(this.Font.Name, this.Font.Size);
                //TextRenderer.DrawText(g, text, drawFont, rect, Color.Black, Color.Transparent, TextFormatFlags.Bottom);
                Invoke((MethodInvoker)(() => { labelBmTime.Text = text; }));
            }

            //bmp.Save(@"e:\test.bmp");
            Invoke((MethodInvoker)(() => { pictureBox1.Image = bmp; pictureBox1.Refresh(); }));
        }
        else
        {
            // TODO: Een kruis door de bitmap zetten zodat we iets zien (het is nu een lege bitmap)

            Image bmp = new Bitmap(intWidth, intHeight);
            Graphics g = Graphics.FromImage(bmp);

            Invoke((MethodInvoker)(() => { pictureBox1.Image = bmp; pictureBox1.Refresh(); }));
        }
    }


    public void BinanceBarometerAll()
    {
        try
        {
            if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running)
                return;

            //#if TRADEBOT
            //            if (GlobalData.Settings.Trading.Active)
            //                TradingRules.CheckNeedBotPause();
            //#endif

            // Bereken de laatste barometer waarden
            BarometerTools barometerTools = new();
            barometerTools.ExecuteAsync();

            if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange exchange))
                return;

            string baseCoin = "";
            Invoke((MethodInvoker)(() => baseCoin = EditBarometerQuote.Text));
            if (!GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out CryptoQuoteData quoteData))
                return;

            // Het grafiek gedeelte
            // Toon de waarden van de geselecteerde basismunt
            int baseIndex = 0;
            Invoke((MethodInvoker)(() => baseIndex = EditBarometerInterval.SelectedIndex));

            CryptoIntervalPeriod intervalPeriod = CryptoIntervalPeriod.interval1h;
            if (baseIndex == 0)
                intervalPeriod = CryptoIntervalPeriod.interval1h;
            else if (baseIndex == 1)
                intervalPeriod = CryptoIntervalPeriod.interval4h;
            else if (baseIndex == 2)
                intervalPeriod = CryptoIntervalPeriod.interval1d;
            else
                return;
            if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
                return;

            BarometerData? barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(quoteData.Name, intervalPeriod);
            CreateBarometerBitmap(exchange, quoteData, interval);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }


    public void ShowBarometerStuff(object? sender, EventArgs? e)
    {
        // Dan wordt de basecoin bewaard voor een volgende keer
        GlobalData.Settings.General.SelectedBarometerQuote = EditBarometerQuote.Text;
        GlobalData.Settings.General.SelectedBarometerInterval = EditBarometerInterval.Text;
        Task.Run(BinanceBarometerAll);
    }


    private static void ShowSymbolPrice(SymbolHist hist, InformationRow item, Core.Model.CryptoExchange exchange,
        CryptoQuoteData quoteData, string baseCoin, string caption, string valueText)
    {
        // Not a really charming way to display items, but voila, it works for now..
        Label label;


        // subitem 0, 1 en 2
        if (exchange.SymbolListName.TryGetValue(baseCoin + quoteData.Name, out CryptoSymbol? symbol) || exchange.SymbolListName.TryGetValue(baseCoin + "USDT", out symbol))
        {
            decimal value;
            item.SymbolName.Text = symbol.Name;

            label = item.SymbolPrice;
            if (symbol.LastPrice.HasValue)
            {
                value = (decimal)symbol.LastPrice;
                label.Text = value.ToString(symbol.PriceDisplayFormat);
                label.ForeColor = Color.Black;

                if (symbol.Name.Equals(hist.Symbol) && hist.PricePrev.HasValue)
                {
                    if (value < hist.PricePrev)
                        label.ForeColor = Color.Red;
                    else if (value > hist.PricePrev)
                        label.ForeColor = Color.Green;
                }
            }
            else
                label.Text = "";


            label = item.SymbolVolume;
            value = (decimal)symbol.Volume;
            label.Text = value.ToString("N0");
            label.ForeColor = Color.Black;
            if (symbol.Name.Equals(hist.Symbol))
            {
                if (value < hist.VolumePrev)
                    label.ForeColor = Color.Red;
                else if (value > hist.VolumePrev)
                    label.ForeColor = Color.Green;
            }
            else
                label.Text = "";

            hist.Symbol = symbol.Name;
            hist.PricePrev = symbol.LastPrice;
            hist.VolumePrev = (decimal)symbol.Volume;
        }
        else
        {
            item.SymbolName.Text = "";
            item.SymbolPrice.Text = "";
            item.SymbolVolume.Text = "";

            hist.Symbol = "";
            hist.PricePrev = 0m;
            hist.VolumePrev = 0m;
        }



        // subitem 4 en 5
        SymbolValue tvValues = item.Symbol2Object;
        if (tvValues != null)
        {
            label = item.Symbol2Name;
            label.Text = tvValues.Name;
            label.Tag = tvValues;

            decimal value = tvValues.Lp;
            label = item.Symbol2Value;
            label.Text = value.ToString(tvValues.DisplayFormat);
            if (value < tvValues.LastValue)
                label.ForeColor = Color.Red;
            else if (value > tvValues.LastValue)
                label.ForeColor = Color.Green;
            tvValues.LastValue = value;
        }
        else
        {
            item.Symbol2Name.Text = "";
            item.Symbol2Value.Text = "";
        }


        // subitem 7 en 8
        item.InfoName.Text = caption;
        item.InfoValue.Text = valueText;

    }

    private void TimerShowInformationInternal()
    {
        // this.BeginUpdate(); die bestaat niet helaas

        if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            labelAppicationStatus.Text = "";
        else
            labelAppicationStatus.Text = GlobalData.ApplicationStatus.ToString();

        // DEBUG CPU belasting
        //GlobalData.AddTextToLogTab("Information.TimerShowInformationInternal");

        //if (GlobalData.ApplicationStatus != CryptoApplicationStatus.AppStatusExiting)
        {
            // De prijzen van candles zijn pas na 20 a 30 seconden na iedere minuut bekend
            // (er zit een vertraging het verkrijgen en verwerken van de candles)
            // 25 seconden is vrij vrij ruim genomen, zou redelijk goed moeten zijn.
            // Een andere methode zou zijn om het aantal symbols in de barometer te tellen 
            // en indien voldoende deze als valide te beschouwen (ook een lastige aanpak)

            // Dus we willen deze alleen uitrekenen indien het +/- 30 seconden is
            // Voorkom dat het te vaak loopt (kost redelijk wat CPU i.c.m. volume)

            if ((DateTime.Now.Second > 10) && (DateTime.Now.Minute != BarometerLastMinute))
            {
                BarometerLastMinute = DateTime.Now.Minute;
                Task.Run(BinanceBarometerAll);
            }

            // Toon de prijzen en volume van een aantal symbols
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange exchange))
            {
                string baseCoin = "";
                Invoke((MethodInvoker)(() => baseCoin = EditBarometerQuote.Text));
                if (GlobalData.Settings.QuoteCoins.TryGetValue(baseCoin, out CryptoQuoteData quoteData))
                {
                    string text, symbol;
                    string e = GlobalData.Settings.General.ExchangeName;
                    if (ExchangeHelper.PriceTicker != null)
                    {
                        // The heavy cpu strain of the Kucoin priceticker is killing the scanner, so its disabled
                        if (!ExchangeHelper.PriceTicker.Enabled)
                            text = "disabled";
                        else
                            text = ExchangeHelper.PriceTicker.Count().ToString("N0");
                        symbol = GlobalData.Settings.ShowSymbolInformation[0];
                        ShowSymbolPrice(SymbolHistList[0], InformationRowList[0], exchange, quoteData, symbol, "Price ticker count", text);
                    }
                    if (ExchangeHelper.KLineTicker != null)
                    {
                        text = ExchangeHelper.KLineTicker.Count().ToString("N0");
                        symbol = GlobalData.Settings.ShowSymbolInformation[1];
                        ShowSymbolPrice(SymbolHistList[1], InformationRowList[1], exchange, quoteData, symbol, "Kline ticker count", text);
                    }

                    text = PositionMonitor.AnalyseCount.ToString("N0");
                    symbol = GlobalData.Settings.ShowSymbolInformation[2];
                    ShowSymbolPrice(SymbolHistList[2], InformationRowList[2], exchange, quoteData, symbol, "Scanner analyse count", text);

                    text = GlobalData.CreatedSignalCount.ToString("N0");
                    symbol = GlobalData.Settings.ShowSymbolInformation[3];
                    ShowSymbolPrice(SymbolHistList[3], InformationRowList[3], exchange, quoteData, symbol, "Scanner signal count", text);

                    symbol = GlobalData.Settings.ShowSymbolInformation[4];
                    if (GlobalData.Settings.Trading.Active)
                    {
                        int positionCount = 0; // hmm, in welk tradeAccount? Even quick en dirty
                        foreach (var tradeAccount in GlobalData.TradeAccountList.Values)
                        {
                            if (tradeAccount.Data.PositionList.Count != 0)
                            {
                                //De muntparen toevoegen aan de userinterface
                                foreach (var position in tradeAccount.Data.PositionList.Values)
                                {
                                    positionCount++;
                                }
                            }
                        }
                        text = $"({GlobalData.Settings.Trading.SlotsMaximalLong}/{GlobalData.Settings.Trading.SlotsMaximalShort}) {positionCount}";
                        ShowSymbolPrice(SymbolHistList[4], InformationRowList[4], exchange, quoteData, symbol, "Openstaande posities", text);
                    }
                    else
                        ShowSymbolPrice(SymbolHistList[4], InformationRowList[4], exchange, quoteData, symbol, "", "");
                }
            }
        }
    }

    private void ListViewInformation_DoubleClick(object? sender, EventArgs? e)
    {
        if (sender is Label target)
        {
            foreach (var x in InformationRowList)
            {
                if (x.SymbolName == target || x.SymbolPrice == target || x.SymbolVolume == target)
                {
                    string symbolName = x.SymbolName.Text;
                    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange? exchange))
                    {
                        if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                        {
                            if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out CryptoInterval? interval))
                                return;

                            LinkTools.ActivateExternalTradingApp(GlobalData.Settings.General.TradingApp, symbol, interval);
                            return;
                        }
                    }
                }

                if (x.Symbol2Name == target || x.Symbol2Value == target)
                {
                    SymbolValue tvValues = x.Symbol2Object;
                    if (tvValues == null)
                        return;

                    string href;
                    if (tvValues.Ticker == null)
                        href = tvValues.Url;
                    else
                        href = string.Format("https://www.tradingview.com/chart/?symbol={0}&interval=60", tvValues.Ticker);

                    Uri uri = new(href);
                    LinkTools.WebViewTradingView.Source = uri;
                    LinkTools.TabControl.SelectedTab = LinkTools.TabPageBrowser;
                }

                //if (x.InfoName == target || x.InfoValue == target)
                //{
                //}
            }
        }
    }

    public void TimerShowInformation_Tick(object? sender, EventArgs? e)
    {
        if (IsHandleCreated)
        {
            Invoke((MethodInvoker)(() => TimerShowInformationInternal()));
        }
    }
}
