using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

using Dapper;
using Dapper.Contrib.Extensions;

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

        AddStandardSymbolCommands(ContextMenuStripPositionsOpen, false);

        ContextMenuStripPositionsOpen.Items.Add(new ToolStripSeparator());

        ToolStripMenuItem menuCommand;
        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie herberekenen";
        menuCommand.Click += CommandPositionsOpenRecalculateExecute;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie verwijderen uit database";
        menuCommand.Click += CommandPositionsOpenDeleteFromDatabase;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "DCA toevoegen aan positie";
        menuCommand.Click += CommandPositionsOpenCreateAdditionalDca;
        ContextMenuStripPositionsOpen.Items.Add(menuCommand);

        menuCommand = new ToolStripMenuItem();
        menuCommand.Text = "Positie profit nemen (buggy)";
        menuCommand.Click += CommandPositionsOpenLastPartTakeProfit;
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
            Interval = 15 * 1000 // 15 seconden
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
        //listViewPositionsOpen.Columns.Add("Net NPL%", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("BE perc", -2, HorizontalAlignment.Right);

        listViewPositionsOpen.Columns.Add("Parts", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("EntryPrice", -2, HorizontalAlignment.Right);
        listViewPositionsOpen.Columns.Add("ProfitPrice", -2, HorizontalAlignment.Right);
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

        decimal NetPnl = position.CurrentProfit();
        decimal NetPnlPerc = position.CurrentProfitPercentage();

        subItem = item1.SubItems.Add(NetPnl.ToString(position.Symbol.QuoteData.DisplayFormat));
        if (NetPnlPerc > 0) 
            subItem.ForeColor = Color.Green;
        else if (NetPnlPerc < 0)
            subItem.ForeColor = Color.Red;

        // profit percentage (NetPnl%)
        //subItem = item1.SubItems.Add(NetPnlPerc.ToString("N2"));
        //if (NetPnlPerc > 0)
        //    subItem.ForeColor = Color.Green;
        //else if (NetPnlPerc < 0)
        //    subItem.ForeColor = Color.Red;

        // tov BE percentage
        decimal bePerc = position.CurrentBreakEvenPercentage();
        subItem = item1.SubItems.Add(bePerc.ToString("N2"));
        if (bePerc > 0)
            subItem.ForeColor = Color.Green;
        else if (bePerc < 0)
            subItem.ForeColor = Color.Red;

        // PartCount is het aantal parts die we gemaakt hebben (de parts.Count), er kan nog eentje open staan
        int partCount = position.PartCount;
        if (position.ActiveDca)
            partCount--;
        // En we willen de openstaande part niet zien totdat deze echt gevuld is
        string text = partCount.ToString();
        // + ten teken dat er een openstaande DCA klaar staat (wellicht ook nog dat ie manual is)
        if (position.ActiveDca)
            text += "+";
        item1.SubItems.Add(text);
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
            List<CryptoPosition> list = [];
            if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange exchange))
            {
                foreach (var tradingAccount in GlobalData.TradeAccountList.Values)
                {
                    foreach (var positionList in tradingAccount.PositionList.Values)
                    {
                        //De muntparen toevoegen aan de userinterface
                        foreach (CryptoPosition position in positionList.Values)
                            list.Add(position);
                    }
                }
            }

            //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#start");

            // Alle positie gerelateerde zaken verversen
            Task.Run(() => {
                Invoke(new Action(() =>
                {
                    ListViewPositionsOpenAddPositions(list);
                    ClosedPositionsHaveChangedEvent();
                    dashBoardControl1.RefreshInformation(null, null);
                    //GlobalData.AddTextToLogTab("PositionsHaveChangedEvent#einde");
                }));
            });
        }
    }


    private void ListViewPositionsOpenAddPositions(List<CryptoPosition> list)
    {
        listViewPositionsOpen.BeginUpdate();
        try
        {
            List<ListViewItem> range = [];
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

                        // Soms blijven er posities hangen, daarom een extra controle (zou eigenlijk overbodig moeten zijn..)
                        if (position.Status == CryptoPositionStatus.Ready && position.Invested > 0 && position.Quantity == 0)
                            GlobalData.ThreadDoubleCheckPosition.AddToQueue(position);


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
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie herberekend");
        }

    }


    private void CommandPositionsOpenDeleteFromDatabase(object sender, EventArgs e)
    {
        if (listViewPositionsOpen.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsOpen.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            try
            {
                using CryptoDatabase databaseThread = new();
                databaseThread.Connection.Open();

                // Controleer de orders, en herbereken het geheel
                PositionTools.LoadPosition(databaseThread, position);
                //await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
                //TradeTools.CalculatePositionResultsViaTrades(databaseThread, position, saveChangesAnywhay: true);
                //FillItemClosed(position, item);


                using var transaction = databaseThread.BeginTransaction();
                databaseThread.Connection.Execute($"delete from positionstep where positionid={position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from positionpart where positionid={position.Id}", transaction);
                databaseThread.Connection.Execute($"delete from position where id={position.Id}", transaction);
                transaction.Commit();

                listViewPositionsOpen.Items.Remove(item);
                PositionTools.RemovePosition(position.TradeAccount, position, false);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig positie uit database verwijderd");
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error deleteing position {error.Message}");
            }
        }
    }


    private async void CommandPositionsOpenCreateAdditionalDca(object sender, EventArgs e)
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

            // Op welke prijs? Actueel, of nog X% eronder?
            //TradeTools.
            // todo...
            //TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
            //FillItemOpen(position, item);

            //decimal adjust = GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m;

            // Corrigeer de prijs indien de koers ondertussen lager of hoger ligt
            decimal price = (decimal)position.Symbol.LastPrice;
            if (position.Side == CryptoTradeSide.Long)
            {
                //price = step.Price - adjust;
                if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice < price)
                    price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
            }
            else
            {
                //price = step.Price + adjust;
                if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice > price)
                    price = (decimal)position.Symbol.LastPrice + position.Symbol.PriceTickSize;
            }


            // Zo laat mogelijk controleren vanwege extra calls naar de exchange
            //var resultCheckAssets = await CheckApiAndAssetsAsync(position.TradeAccount);
            //if (!resultCheckAssets.success)
            //{
            //    string text = $"{position.Symbol.Name} + DCA bijplaatsen op {price.ToString0(position.Symbol.PriceDisplayFormat)}";
            //    GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
            //    Symbol.ClearSignals();
            //    return;
            //}


            // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
            PositionTools.ExtendPosition(databaseThread, position, CryptoPartPurpose.Dca, position.Interval, position.Strategy, 
                CryptoEntryOrProfitMethod.FixedPercentage, price, DateTime.UtcNow, true);
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} handmatig een DCA toegevoegd");

            FillItemOpen(position, item);
        }

    }


    private async void CommandPositionsOpenLastPartTakeProfit(object sender, EventArgs e)
    {
        if (listViewPositionsOpen.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsOpen.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            if (!position.Symbol.LastPrice.HasValue)
                return;

            using CryptoDatabase databaseThread = new();
            databaseThread.Connection.Open();

            // Controleer de orders, en herbereken het geheel
            PositionTools.LoadPosition(databaseThread, position);
            await TradeTools.LoadTradesfromDatabaseAndExchange(databaseThread, position);
            TradeTools.CalculatePositionResultsViaTrades(databaseThread, position);
            FillItemOpen(position, item);


            // Itereer de openstaande parts
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                // voor de niet afgesloten parts...
                if (!part.CloseTime.HasValue && part.Quantity > 0)
                {
                    CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();

                    // Is er een entry order in zijn geheel gevuld (lastig indien er meerdere entries komen)
                    CryptoPositionStep step = PositionTools.FindPositionPartStep(part, entryOrderSide, true);
                    if (step != null && step.Status == CryptoOrderStatus.Filled)
                    {

                        // Is de entry prijs wel hoger/lager dan de actuele prijs?
                        //if (position.Side == CryptoTradeSide.Long && step.Price < part.BreakEvenPrice + 5 * position.Symbol.PriceTickSize)
                        //    continue;
                        //else if (position.Side == CryptoTradeSide.Short && step.Price > part.BreakEvenPrice - 5 * position.Symbol.PriceTickSize)
                        //    continue;

                        // Is de laatste prijs wel hoger/lager dan de actuele prijs?
                        if (position.Side == CryptoTradeSide.Long && position.Symbol.LastPrice.Value < part.BreakEvenPrice + 25 * position.Symbol.PriceTickSize)
                            continue;
                        else if (position.Side == CryptoTradeSide.Short && position.Symbol.LastPrice.Value > part.BreakEvenPrice - 25 * position.Symbol.PriceTickSize)
                            continue;

                        // Is er een openstaande TP die niet gevuld is?
                        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
                        step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
                        if (step != null && part.Quantity > 0)
                        {
                            decimal price = (decimal)position.Symbol.LastPrice;
                            // controle is waarschijnlijk overbodig, de extra tick is prima
                            // (tenzij het een market order moet zijn? Maar dan moet er meer aangepast worden)
                            if (position.Side == CryptoTradeSide.Long)
                                price += position.Symbol.PriceTickSize;
                            else
                                price -= position.Symbol.PriceTickSize;

                            var (cancelled, cancelParams) = await TradeTools.CancelOrder(databaseThread, position, part, step, DateTime.UtcNow, CryptoOrderStatus.ManuallyByUser);
                            if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
                                ExchangeBase.Dump(position, cancelled, cancelParams, "annuleren vanwege handmatige TP");

                            if (cancelled)
                            {
                                price = price.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
                                await TradeTools.PlaceTakeProfitOrderAtPrice(databaseThread, position, part, price, DateTime.UtcNow, "manually placing");
                                FillItemOpen(position, item);
                                GlobalData.AddTextToLogTab($"{position.Symbol.Name} {part.PartNumber} handmatig profit genomen");

                                break;
                            }

                        }
                    }

                }
            }
        }
    }

}
#endif

