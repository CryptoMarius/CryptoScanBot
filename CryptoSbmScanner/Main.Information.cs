using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using System.Net.Http.Json;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private ListViewDoubleBuffered listViewInformation;

    private void ListViewInformationConstructor()
    {
        // 
        // listViewInformation
        // 
        listViewInformation = new()
        {
            GridLines = false,
            Scrollable = false,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = true,
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Control,
            Activation = ItemActivation.OneClick,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left, // | AnchorStyles.Right;
            Location = new Point(526, 0),
            Size = new Size(1000, 93),
        };
        listViewInformation.DoubleClick += new System.EventHandler(ListViewInformation_DoubleClick);

        panelTop.Controls.Add(listViewInformation);
    }


    private void TimerShowBarometer_Tick(object sender, EventArgs e)
    {
        if (GlobalData.ApplicationStatus != ApplicationStatus.AppStatusExiting)
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



            // Toon de prijzen en volume van een aantal symbols
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
            {
                listViewInformation.BeginUpdate();
                try
                {
                    if (listViewInformation.Columns.Count == 0)
                    {
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Left); // Symbol
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Right); // Price
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Right); // Volume
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Left); // Space

                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Left); // Market
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Right); // Value
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Left); // Space

                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Left); // Caption
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Right); // Count
                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Left); // Space

                        listViewInformation.Columns.Add("", -2, HorizontalAlignment.Right); // filler

                        listViewInformation.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

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
                            listViewInformation.Items.Add(item1);
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
                        ShowSymbolPrice(symbolHistList[0], listViewInformation.Items[0], exchange, quoteData, "BTC", GlobalData.TradingViewDollarIndex, "Binance price ticker count", text);

                        text = candlesKLineCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[1], listViewInformation.Items[1], exchange, quoteData, "BNB", GlobalData.TradingViewBitcoinDominance, "Binance 1m stream count", text);

                        text = PositionMonitor.AnalyseCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[2], listViewInformation.Items[2], exchange, quoteData, "ETH", GlobalData.TradingViewSpx500, "Scanner analyse count", text);

                        text = createdSignalCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[3], listViewInformation.Items[3], exchange, quoteData, "XRP", GlobalData.TradingViewMarketCapTotal, "Scanner signal count", text);

#if SQLDATABASE
                        text = GlobalData.TaskSaveCandles.QueueCount.ToString("N0");
                        ShowSymbolPrice(symbolHistList[4], listViewInformation.Items[4], exchange, quoteData, "ADA", GlobalData.FearAndGreedIndex, "Database Buffer", text);
#else
                        ShowSymbolPrice(symbolHistList[4], listViewInformation.Items[4], exchange, quoteData, "ADA", GlobalData.FearAndGreedIndex, "", "");
#endif
                    }

                    for (int i = 0; i <= listViewInformation.Columns.Count - 1; i++)
                    {
                        if (i == 3 || i == 6 || i == 9)
                            listViewInformation.Columns[i].Width = 25;
                        else
                            listViewInformation.Columns[i].Width = -2;
                    }
                }
                finally
                {
                    listViewInformation.EndUpdate();
                }
            }
        }
    }


    private void ListViewInformation_DoubleClick(object sender, EventArgs e)
    {
        if (listViewInformation.SelectedItems.Count > 0)
        {
            Point mousePos = listViewInformation.PointToClient(Control.MousePosition);
            ListViewHitTestInfo hitTest = listViewInformation.HitTest(mousePos);
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

                string href;
                if (tvValues.Ticker == null)
                    href = tvValues.Url;
                else
                    href = string.Format("https://www.tradingview.com/chart/?symbol={0}&interval=60", tvValues.Ticker);

                Uri uri = new(href);
                webViewTradingView.Source = uri;
                tabControl.SelectedTab = tabPageBrowser;
            }
            else tabControl.SelectedTab = tabPageSignals;
        }
    }

}