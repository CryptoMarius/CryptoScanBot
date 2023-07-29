using System.Text;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private ListViewDoubleBuffered listViewPositionsOpen;
    private System.Windows.Forms.Timer TimerRefreshSomething;

    //public void Dispose()
    //{
    //    if (TimerClearEvents != null) { TimerClearEvents.Dispose(); TimerClearEvents = null; }
    //}

    private void ListViewPositionsOpenConstructor()
    {
        ListViewColumnSorterPosition listViewColumnSorter = new()
        {
            SortColumn = 1,
            ClosedPositions = false,
            SortOrder = SortOrder.Descending
        };

        // ruzie (component of events raken weg), dan maar dynamisch
        listViewPositionsOpen = new()
        {
            CheckBoxes = false
        };
        listViewPositionsOpen.CheckBoxes = false;
        listViewPositionsOpen.AllowColumnReorder = false;
        listViewPositionsOpen.Dock = DockStyle.Fill;
        listViewPositionsOpen.Location = new Point(4, 3);
        listViewPositionsOpen.GridLines = true;
        listViewPositionsOpen.View = View.Details;
        listViewPositionsOpen.FullRowSelect = true;
        listViewPositionsOpen.HideSelection = true;
        listViewPositionsOpen.BorderStyle = BorderStyle.None;
        listViewPositionsOpen.ContextMenuStrip = contextMenuStripPositionsOpen;
        listViewPositionsOpen.ListViewItemSorter = listViewColumnSorter;
        //listViewPositionsOpen.ColumnClick += ListViewSignals_ColumnClick;
        listViewPositionsOpen.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
        listViewPositionsOpen.DoubleClick += ListViewPositionOpen_MenuItem_DoubleClick;
        tabPagePositionsOpen.Controls.Add(listViewPositionsOpen);

        TimerRefreshSomething = new()
        {
            Interval = 20 * 1000 // 20 seconden
        };
        TimerRefreshSomething.Tick += TimerRefreshSomething_Tick;
		TimerRefreshSomething.Enabled = true;

        ListViewPositionsOpenInitColumns();
    }




    private void ListViewPositionsOpenInitColumns()
    {
        // Create columns and subitems. Width of -2 indicates auto-size
        listViewPositionsOpen.Columns.Add("ID", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Datum", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Account", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Exchange", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Status", -2, HorizontalAlignment.Left);

        //listViewPositionsOpen.Columns.Add("Price", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("BreakEven", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Quantity", -2, HorizontalAlignment.Right);
        //listViewPositionsOpen.Columns.Add("Stijging", -2, HorizontalAlignment.Right);

        listViewPositionsOpen.Columns.Add("Invested", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Returned", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Open", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Commission", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Net NPL", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Percentage", -2, HorizontalAlignment.Right);

        listViewPositionsOpen.Columns.Add("Parts", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("BuyPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("SellPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("LastPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        for (int i = 0; i <= listViewPositionsOpen.Columns.Count - 1; i++)
        {
            //if (i != 5)
                listViewPositionsOpen.Columns[i].Width = -2;
        }
    }


    private static void FillItemOpen(CryptoPosition position, ListViewItem item1)
    {
        // Omdat het item via een range wordt toegevoegd is deze niet beschikbaar
        //if (index % 2 == 0)
        //    item1.BackColor = Color.LightGray;

        ListViewItem.ListViewSubItem subItem;
        item1.SubItems.Clear();

        item1.Text = position.Id.ToString();
        item1.SubItems.Add(position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        item1.SubItems.Add(position.TradeAccount.Name);
        item1.SubItems.Add(position.Symbol.Exchange.Name);
        item1.SubItems.Add(position.Symbol.Name);
        item1.SubItems.Add(position.Interval.Name);
        item1.SubItems.Add(position.StrategyText);

        subItem = item1.SubItems.Add(position.SideText);
        if (position.Side == CryptoOrderSide.Buy)
            subItem.ForeColor = Color.Green;
        else if (position.Side == CryptoOrderSide.Sell)
            subItem.ForeColor = Color.Red;

        subItem = item1.SubItems.Add(position.Status.ToString());
        if (position.Status == CryptoPositionStatus.Waiting)
            subItem.ForeColor = Color.Red;

        if (position.Status == CryptoPositionStatus.Waiting)
            item1.SubItems.Add(position.BuyPrice?.ToString(position.Symbol.PriceDisplayFormat));
        else
            item1.SubItems.Add(position.BreakEvenPrice.ToString(position.Symbol.PriceDisplayFormat));
        item1.SubItems.Add(position.Quantity.ToString0(position.Symbol.QuantityDisplayFormat));

        item1.SubItems.Add(position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));

        item1.SubItems.Add(position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
        subItem = item1.SubItems.Add((position.Invested - position.Returned).ToString(position.Symbol.QuoteData.DisplayFormat));
        if (position.Invested - position.Returned > 7 * position.Symbol.QuoteData.BuyAmount) // een indicatie (beetje willekeurig)
            subItem.ForeColor = Color.Red;

        item1.SubItems.Add(position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));

        // profit bedrag
        decimal NetPnl = position.MarketValue;
        subItem = item1.SubItems.Add(NetPnl.ToString(position.Symbol.QuoteData.DisplayFormat));
        if (NetPnl > position.Invested - position.Returned - position.Commission)
            subItem.ForeColor = Color.Green;
        else if (NetPnl < position.Invested - position.Returned - position.Commission)
            subItem.ForeColor = Color.Red;

        // profit percentage
        decimal priceDiff = position.MarketValuePercentage;
        subItem = item1.SubItems.Add(priceDiff.ToString("N2"));
        if (priceDiff > 0)
            subItem.ForeColor = Color.Green;
        else if (priceDiff < 0)
            subItem.ForeColor = Color.Red;

        //subItem = item1.SubItems.Add(position.Percentage.ToString("N2"));
        //if (position.Percentage > 0)
        //    subItem.ForeColor = Color.Green;
        //else
        //    if (position.Percentage < 0)
        //    subItem.ForeColor = Color.Red;

        item1.SubItems.Add(position.PartCount.ToString());
        item1.SubItems.Add(position.BuyPrice?.ToString(position.Symbol.PriceDisplayFormat));
        if (position.SellPrice.HasValue)
            item1.SubItems.Add(position.SellPrice?.ToString(position.Symbol.PriceDisplayFormat));
        else
            item1.SubItems.Add("null");
        item1.SubItems.Add(position.Symbol.LastPrice?.ToString(position.Symbol.PriceDisplayFormat));
    }


    private static ListViewItem AddOpenPosition(CryptoPosition position)
    {
        ListViewItem item1 = new("", -1)
        {
            UseItemStyleForSubItems = false
        };
        FillItemOpen(position, item1);
        return item1;
    }


    private void OpenPositionsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if (components != null && IsHandleCreated) //!ProgramExit && 
        {
            List<CryptoPosition> list = new();
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
            {
                foreach (var tradingAccount in GlobalData.TradeAccountList.Values)
                {
                    foreach (var positionList in tradingAccount.PositionList.Values)
                    {
                        //De muntparen toevoegen aan de userinterface
                        foreach (CryptoPosition position in positionList.Values)
                        {
                            list.Add(position);
                        }
                    }
                }
            }
            // Openstaande posities
            Task.Run(() => {
                Invoke(new Action(() =>
                {
                    ListViewPositionsOpenAddPositions(list);
                }));
            });


            ClosedPositionsHaveChangedEvent();
        }
    }


    private void ListViewPositionsOpenAddPositions(List<CryptoPosition> list)
    {
        listViewPositionsOpen.BeginUpdate();
        try
        {
            List<ListViewItem> range = new();
            foreach (CryptoPosition position in list)
            {
                ListViewItem item = AddOpenPosition(position);
                item.Tag = position;
                range.Add(item);
            }

            listViewPositionsOpen.Clear();
            ListViewPositionsOpenInitColumns();
            listViewPositionsOpen.Items.AddRange(range.ToArray());

            // Deze redelijk kostbaar? (alles moet gecontroleerd worden)
            for (int i = 0; i <= listViewPositionsOpen.Columns.Count - 1; i++)
            {
                listViewPositionsOpen.Columns[i].Width = -2;
            }
        }
        finally
        {
            listViewPositionsOpen.EndUpdate();
        }
    }



    private void ListViewPositionOpen_MenuItem_DoubleClick(object sender, EventArgs e)
    {
        if (listViewPositionsOpen.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsOpen.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;
            LinkTools.ActivateExternalTradingApp(GlobalData.Settings.General.TradingApp, position.Symbol, position.Interval);
        }
    }


    private void TimerRefreshSomething_Tick(object sender, EventArgs e)
    {
        // Een paar velden bijwerken
        if (listViewPositionsOpen.Items.Count > 0)
        {
            bool startedUpdating = false;
            try
            {
                //ListViewItem.ListViewSubItem subItem;

                for (int index = listViewPositionsOpen.Items.Count - 1; index >= 0; index--)
                {
                    ListViewItem item = listViewPositionsOpen.Items[index];
                    CryptoPosition position = (CryptoPosition)item.Tag;
                    {
                        startedUpdating = true;

                        FillItemOpen(position, item);

                        //// Status
                        //subItem = item.SubItems[4];
                        //subItem.Text = position.Status.ToString();

                        //// profit bedrag
                        //subItem = item.SubItems[10];
                        //decimal profit = position.MarketValue;
                        //subItem.Text = profit.ToString(position.Symbol.QuoteData.DisplayFormat);
                        //if (profit > position.Invested)
                        //    subItem.ForeColor = Color.Green;
                        //else if (profit < position.Invested)
                        //    subItem.ForeColor = Color.Red;

                        //// profit percentage
                        //subItem = item.SubItems[11];
                        //double priceDiff = 0;
                        //if (position.BreakEvenPrice != 0)
                        //    priceDiff = (double)(100 * ((position.Symbol.LastPrice / position.BreakEvenPrice) - 1));
                        //subItem.Text = priceDiff.ToString("N2");
                        //if (priceDiff > 0)
                        //    subItem.ForeColor = Color.Green;
                        //else if (priceDiff < 0)
                        //    subItem.ForeColor = Color.Red;

                        //// TODO - Iets met een DCA indicator
                    }

                }
            }
            finally
            {
                if (startedUpdating)
                    listViewPositionsOpen.EndUpdate();
            }
        }
    }

    //private void ListBoxSymbolsMenuItemPositionCalculate_Click(object sender, EventArgs e)
    //{
    //    if (listViewPositionsOpen.SelectedItems.Count > 0)
    //    {
    //        ListViewItem item = listViewPositionsOpen.SelectedItems[0];
    //        CryptoPosition position = (CryptoPosition)item.Tag;

    //        // todo: Geschikt maken voor paperTrading!
    //        //TradeTools.CheckPosition(databaseMain, position);
    //    }
    //}


    ///// <summary>
    ///// Alle posities van deze munt sluiten
    ///// </summary>
    ///// <param name="sender"></param>
    ///// <param name="e"></param>
    //private void ListBoxSymbolsMenuItemPositieClose_Click(object sender, EventArgs e)
    //{
    //    //// Neem de door de gebruiker geselecteerde coin
    //    //string symbolName = listBoxSymbols.Text.ToString();
    //    //if (string.IsNullOrEmpty(symbolName))
    //    //{
    //    //    GlobalData.AddTextToLogTab("Er is geen symbol gekozen");
    //    //    return;
    //    //}

    //    //if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out CryptoSbmScanner.Model.CryptoExchange exchange))
    //    //{
    //    //    // Bestaat de coin? (uiteraard, net geladen)
    //    //    if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
    //    //    {
    //    //        GlobalData.AddTextToLogTab("Symbol niet gevonden (niet geladen?)");
    //    //        return;
    //    //    }

    //    //    symbol.Exchange.PositionListSemaphore.Wait();
    //    //    try
    //    //    {
    //    //        foreach (CryptoPosition position in symbol.PositionList.Values)
    //    //        {
    //    //            position.Profit = 0;
    //    //            position.Invested = 0;
    //    //            position.Percentage = 0;
    //    //            position.CloseTime = DateTime.UtcNow;
    //    //            position.Status = CryptoPositionStatus.positionTakeOver;
    //    //            databaseMain.Connection.Update<CryptoPosition>(position);
    //    //        }

    //    //        symbol.PositionList.Clear();
    //    //    }
    //    //    finally
    //    //    {
    //    //        symbol.Exchange.PositionListSemaphore.Release();
    //    //    }
    //    //}
    //}

    //private void PositionsToolStripMenuItem_Click(object sender, EventArgs e)
    //{
    //    //try
    //    //{
    //    //    //StringBuilder stringBuilder = new StringBuilder();
    //    //    //BinanceTools.ShowPositions(stringBuilder);
    //    //    //GlobalData.AddTextToLogTab(stringBuilder.ToString());
    //    //    GlobalData.AddTextToLogTab("");

    //    //    // nu iets duidelijker
    //    //    if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out CryptoSbmScanner.Model.CryptoExchange exchange))
    //    //    {
    //    //        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
    //    //        {
    //    //            symbol.Exchange.PositionListSemaphore.Wait();
    //    //            try
    //    //            {
    //    //                foreach (CryptoPosition position in symbol.PositionList.Values)
    //    //                {
    //    //                    StringBuilder stringBuilder = new StringBuilder();
    //    //                    Helper.ShowPosition(stringBuilder, position);
    //    //                    GlobalData.AddTextToLogTab(stringBuilder.ToString());
    //    //                }
    //    //            }
    //    //            finally
    //    //            {
    //    //                symbol.Exchange.PositionListSemaphore.Release();
    //    //            }
    //    //        }
    //    //    }

    //    //}
    //    //catch (Exception error)
    //    //{
    //    //    GlobalData.Logger.Error(error);
    //    //    GlobalData.AddTextToLogTab("ERROR postion display " + error.ToString());
    //    //}
    //}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async void DebugCandleDumpToolStripMenuItem1Async_Click(object sender, EventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
#if TRADEBOT

        if (listViewPositionsOpen.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsOpen.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await PositionTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            PositionTools.CalculatePositionResultsViaTrades(databaseThread, position);
            FillItemOpen(position, item);

            StringBuilder strings = new();
            PositionTools.DumpPosition(position, strings);
            GlobalData.AddTextToLogTab(strings.ToString());
        }
#endif
    }

    private void DebugPositionOpenDumpExcelToolStripMenuItemAsync_Click(object sender, EventArgs e)
    {
        if (listViewPositionsOpen.SelectedItems.Count > 0)
        {
            for (int index = 0; index < listViewPositionsOpen.SelectedItems.Count; index++)
            {
                ListViewItem item = listViewPositionsOpen.SelectedItems[index];
                CryptoPosition position = (CryptoPosition)item.Tag;

                Task.Run(() => {
                    Invoke(new Action(() =>
                    {
                        string filename = new PositionDumpDebug().ExportToExcell(position);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filename) { UseShellExecute = true });
                    }));
                });

            }
        }
    }

}

