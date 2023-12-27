using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

namespace CryptoSbmScanner;

#if TRADEBOT
public partial class FrmMain
{
    private ContextMenuStrip ContextMenuStripPositionsOpen;
    private ListViewHeaderContext listViewPositionsOpen;
    private System.Windows.Forms.Timer TimerRefreshSomething;



    private void ListViewPositionsOpenConstructor()
    {
        ContextMenuStripPositionsOpen = new ContextMenuStrip();

        ToolStripMenuItem menuCommand;

        // Commands
        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Activate trading app";
        menuCommand.Tag = Command.ActivateTradingApp;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsOpen.Items.Insert(0, menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "TradingView browser";
        menuCommand.Tag = Command.ActivateTradingviewIntern;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "TradingView extern";
        menuCommand.Tag = Command.ActivateTradingviewExtern;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);


        ContextMenuStripPositionsOpen.Items.Add(new ToolStripSeparator());


        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Herberekenen";
        menuCommand.Click += CommandPositionsOpenRecalculateExecute;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie informatie (Excel)";
        menuCommand.Tag = Command.ExcelPositionInformation;
        menuCommand.Click += Commands.ExecuteCommandCommandViaTag;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);



        // ruzie (component of events raken weg), dan maar dynamisch
        listViewPositionsOpen = new()
        {
            Dock = DockStyle.Fill,
            Location = new Point(4, 3)
        };
        listViewPositionsOpen.ColumnClick += ListViewPositionsOpenColumnClick;
        listViewPositionsOpen.Tag = Command.ActivateTradingApp;
        listViewPositionsOpen.DoubleClick += Commands.ExecuteCommandCommandViaTag;

        tabPagePositionsOpen.Controls.Add(listViewPositionsOpen);

        listViewPositionsOpen.ListViewItemSorter = new ListViewColumnSorterPosition()
        {
            SortColumn = 1,
            ClosedPositions = false,
            SortOrder = SortOrder.Descending
        };

        listViewPositionsOpen.ContextMenuStrip = ContextMenuStripPositionsOpen;

        TimerRefreshSomething = new()
        {
            Interval = 20 * 1000 // 20 seconden
        };
        TimerRefreshSomething.Tick += TimerRefreshSomething_Tick;
		TimerRefreshSomething.Enabled = true;

        ListViewPositionsOpenInitColumns();
    }

    private void ListViewPositionsOpenColumnClick(object sender, ColumnClickEventArgs e)
    {
        listViewPositionsOpen.BeginUpdate();
        try
        {
            // Perform the sort with these new sort options.
            ListViewColumnSorterPosition listViewColumnSorter = (ListViewColumnSorterPosition)listViewPositionsOpen.ListViewItemSorter;
            listViewColumnSorter.ClickedOnColumn(e.Column);
            listViewPositionsOpen.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
            listViewPositionsOpen.Sort();
        }
        finally
        {
            listViewPositionsOpen.EndUpdate();
        }
    }

    private void ListViewPositionsOpenInitColumns()
    {
        // Create columns and subitems. Width of -2 indicates auto-size
        listViewPositionsOpen.Columns.Add("ID", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Datum", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Update", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Duration", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Account", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Exchange", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewPositionsOpen.Columns.Add("Status", -2, HorizontalAlignment.Left);

        listViewPositionsOpen.Columns.Add("Invested", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Returned", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Commission", -2, HorizontalAlignment.Right);

        listViewPositionsOpen.Columns.Add("BreakEven", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Quantity", -2, HorizontalAlignment.Right);

        listViewPositionsOpen.Columns.Add("Open", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Net NPL", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("Percentage", -2, HorizontalAlignment.Right);

        listViewPositionsOpen.Columns.Add("Parts", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("BuyPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("SellPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("FundingRate", -2, HorizontalAlignment.Right);
        //listViewPositionsOpen.Columns.Add("LastPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        listViewPositionsOpen.SetSortIcon(
              ((ListViewColumnSorterPosition)listViewPositionsOpen.ListViewItemSorter).SortColumn, 
              ((ListViewColumnSorterPosition)listViewPositionsOpen.ListViewItemSorter).SortOrder);

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
        item1.SubItems.Add(position.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        item1.SubItems.Add(position.DurationText());
        item1.SubItems.Add(position.TradeAccount.Name);
        item1.SubItems.Add(position.Symbol.Exchange.Name);
        item1.SubItems.Add(position.Symbol.Name);
        item1.SubItems.Add(position.Interval.Name);
        item1.SubItems.Add(position.StrategyText);

        subItem = item1.SubItems.Add(position.SideText);
        if (position.Side == CryptoTradeSide.Long)
            subItem.ForeColor = Color.Green;
        else if (position.Side == CryptoTradeSide.Short)
            subItem.ForeColor = Color.Red;

        subItem = item1.SubItems.Add(position.Status.ToString());
        if (position.Status == CryptoPositionStatus.Waiting)
            subItem.ForeColor = Color.Red;

        item1.SubItems.Add(position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
        item1.SubItems.Add(position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
        item1.SubItems.Add(position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));


        if (position.Status == CryptoPositionStatus.Waiting)
            item1.SubItems.Add(position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat));
        else
            item1.SubItems.Add(position.BreakEvenPrice.ToString(position.Symbol.PriceDisplayFormat));
        item1.SubItems.Add(position.Quantity.ToString0(position.Symbol.QuantityDisplayFormat));

        subItem = item1.SubItems.Add((position.Invested - position.Returned).ToString(position.Symbol.QuoteData.DisplayFormat));
        if (position.Invested - position.Returned > 7 * position.Symbol.QuoteData.EntryAmount) // een indicatie (beetje willekeurig)
            subItem.ForeColor = Color.Red;

        // Netto NPL (het bedrag wat je krijgt als je nu zou verkopen)
        decimal NetPnl = position.MarketValue() - position.Commission;
        decimal priceDiff = position.MarketValuePercentage();

        subItem = item1.SubItems.Add(NetPnl.ToString(position.Symbol.QuoteData.DisplayFormat));
        if (priceDiff > 0)  //if (NetPnl > position.Invested - position.Returned - position.Commission)
            subItem.ForeColor = Color.Green;
        else if (priceDiff < 0) //else if (NetPnl < position.Invested - position.Returned - position.Commission)
            subItem.ForeColor = Color.Red;

        // profit percentage
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
        item1.SubItems.Add(position.EntryPrice?.ToString(position.Symbol.PriceDisplayFormat));
        if (position.ProfitPrice.HasValue)
            item1.SubItems.Add(position.ProfitPrice?.ToString(position.Symbol.PriceDisplayFormat));
        else
            item1.SubItems.Add("null");

        if (position.Symbol.FundingRate != 0.0m)
        {
            subItem = item1.SubItems.Add(position.Symbol.FundingRate.ToString());
            if (position.Symbol.FundingRate > 0)
                subItem.ForeColor = Color.Green;
            else if (position.Symbol.FundingRate < 0)
                subItem.ForeColor = Color.Red;
        }
        else
            item1.SubItems.Add("");

        //item1.SubItems.Add(position.Symbol.LastPrice?.ToString(position.Symbol.PriceDisplayFormat));
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


    private void PositionsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if (IsHandleCreated) //!ProgramExit &&  components != null && 
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

            // Alel positie gerelateerde zaken verversen
            Task.Run(() => {
                Invoke(new Action(() =>
                {
                    ListViewPositionsOpenAddPositions(list);
                    ClosedPositionsHaveChangedEvent();
                    dashBoardControl1.RefreshInformation(null, null);
                }));
            });


            //// Gesloten posities
            //Task.Run(() => {
            //    Invoke(new Action(() =>
            //    {
            //        ClosedPositionsHaveChangedEvent();
            //    }));
            //});


            //// Statistiek
            //Task.Run(() => {
            //    Invoke(new Action(() =>
            //    {
            //        dashBoardControl1.RefreshInformation(null, null);
            //    }));
            //});
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


    private void ListViewPositionsOpenInitCaptions()
    {
        string text = GlobalData.ExternalUrls.GetTradingAppName(GlobalData.Settings.General.TradingApp, GlobalData.Settings.General.ExchangeName);
        listViewPositionsOpen.ContextMenuStrip.Items[0].Text = text;
    }

    private async void CommandPositionsOpenRecalculateExecute(object sender, EventArgs e)
    {
        if (listViewPositionsOpen.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsOpen.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
            FillItemOpen(position, item);
        }

    }

}
#endif

