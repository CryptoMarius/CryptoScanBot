﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

using Dapper;

using NPOI.SS.Formula.Functions;

namespace CryptoSbmScanner;

public partial class FrmMain
{
    private ListViewDoubleBuffered listViewPositionsClosed;
    
    //public void Dispose()
    //{
    //    if (TimerClearEvents != null) { TimerClearEvents.Dispose(); TimerClearEvents = null; }
    //}

    private void ListViewPositionsClosedConstructor()
    {
        ListViewColumnSorterPosition listViewColumnSorter = new()
        {
            SortOrder = SortOrder.Descending
        };

        // ruzie (component of events raken weg), dan maar dynamisch
        listViewPositionsClosed = new()
        {
            CheckBoxes = false
        };
        listViewPositionsClosed.CheckBoxes = false;
        listViewPositionsClosed.AllowColumnReorder = false;
        listViewPositionsClosed.Dock = DockStyle.Fill;
        listViewPositionsClosed.Location = new Point(4, 3);
        listViewPositionsClosed.GridLines = true;
        listViewPositionsClosed.View = View.Details;
        listViewPositionsClosed.FullRowSelect = true;
        listViewPositionsClosed.HideSelection = true;
        listViewPositionsClosed.BorderStyle = BorderStyle.None;
        listViewPositionsClosed.ContextMenuStrip = contextMenuStripPositionsOpen;
        listViewPositionsClosed.ListViewItemSorter = listViewColumnSorter;
        //listViewPositionsClosed.ColumnClick += ListViewSignals_ColumnClick;
        listViewPositionsClosed.SetSortIcon(listViewColumnSorter.SortColumn, listViewColumnSorter.SortOrder);
        listViewPositionsClosed.DoubleClick += new System.EventHandler(ListViewPositionClosed_MenuItem_DoubleClick);
        tabPagePositionsClosed.Controls.Add(listViewPositionsClosed);

        //TimerClearEvents = new();
        //InitTimerInterval(ref TimerClearEvents, 1 * 60);
        //TimerClearEvents.Tick += new System.EventHandler(TimerClearOldSignals_Tick);

        ListViewPositionsClosedInitColumns();
    }




    private void ListViewPositionsClosedInitColumns()
    {
        // TODO: Positie kolommen kiezen..

        // Create columns and subitems. Width of -2 indicates auto-size
        listViewPositionsClosed.Columns.Add("Datum", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Closed", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Account", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Interval", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Mode", -2, HorizontalAlignment.Left);
        listViewPositionsClosed.Columns.Add("Status", -2, HorizontalAlignment.Left);

        //listViewPositionsClosed.Columns.Add("Quantity", -2, HorizontalAlignment.Right);
        //listViewPositionsClosed.Columns.Add("Price", -2, HorizontalAlignment.Right);
        //listViewPositionsClosed.Columns.Add("BreakEven", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("Invested", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Returned", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Commission", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("Profit", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("Percentage", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("BuyPrice", -2, HorizontalAlignment.Right);
        listViewPositionsClosed.Columns.Add("SellPrice", -2, HorizontalAlignment.Right);

        listViewPositionsClosed.Columns.Add("", -2, HorizontalAlignment.Right); // filler

        for (int i = 0; i <= listViewPositionsClosed.Columns.Count - 1; i++)
        {
            if (i != 5)
                listViewPositionsClosed.Columns[i].Width = -2;
        }
    }



    private static ListViewItem AddClosedPosition(CryptoPosition position)
    {
        string s = position.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        ListViewItem item1 = new(s, -1)
        {
            UseItemStyleForSubItems = false
        };

        ListViewItem.ListViewSubItem subItem;
        item1.SubItems.Add(position.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));

        item1.SubItems.Add(position.TradeAccount.Name);
        item1.SubItems.Add(position.Symbol.Name);
        item1.SubItems.Add(position.Interval.Name);
        item1.SubItems.Add(position.StrategyText);

        subItem = item1.SubItems.Add(position.ModeText);
        if (position.Mode == CryptoTradeDirection.Long)
            subItem.ForeColor = Color.Green;
        else if (position.Mode == CryptoTradeDirection.Short)
            subItem.ForeColor = Color.Red;

        subItem = item1.SubItems.Add(position.Status.ToString());
        if (position.Status == CryptoPositionStatus.Waiting)
            subItem.ForeColor = Color.Red;

        //item1.SubItems.Add(position.Quantity.ToString0("N8"));
        //item1.SubItems.Add(position.BuyPrice.ToString(position.Symbol.DisplayFormat));
        //item1.SubItems.Add(position.BreakEvenPrice.ToString(position.Symbol.DisplayFormat));

        item1.SubItems.Add(position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
        item1.SubItems.Add(position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
        item1.SubItems.Add(position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));


        subItem = item1.SubItems.Add(position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
        if (position.Profit > 0)
            subItem.ForeColor = Color.Green;
        else if (position.Profit < 0)
            subItem.ForeColor = Color.Red;

        subItem = item1.SubItems.Add(position.Percentage.ToString("N2"));
        if (position.Percentage > 0)
            subItem.ForeColor = Color.Green;
        else
            if (position.Percentage < 0)
            subItem.ForeColor = Color.Red;

        item1.SubItems.Add(position.BuyPrice.ToString(position.Symbol.PriceDisplayFormat));
        item1.SubItems.Add(position.SellPrice?.ToString(position.Symbol.PriceDisplayFormat));
        return item1;
    }


    private void ClosedPositionsHaveChangedEvent(string text, bool extraLineFeed = false)
    {
        if ((components != null) && (!ProgramExit) && (IsHandleCreated))
        {
            // Gesloten posities
            Task.Factory.StartNew(() =>
            {
                Invoke(new Action(() =>
                {
                    ListViewPositionsClosedAddPositions(GlobalData.PositionsClosed);
                }));
            });
        }
    }


    private void ListViewPositionsClosedAddPositions(List<CryptoPosition> list)
    {
        listViewPositionsClosed.BeginUpdate();
        try
        {
            List<ListViewItem> range = new();
            foreach (CryptoPosition position in list)
            {
                ListViewItem item = AddClosedPosition(position);
                item.Tag = position;
                range.Add(item);
            }

            listViewPositionsClosed.Clear();
            ListViewPositionsClosedInitColumns();
            listViewPositionsClosed.Items.AddRange(range.ToArray());

            // Deze redelijk kostbaar? (alles moet gecontroleerd worden)
            for (int i = 0; i <= listViewPositionsClosed.Columns.Count - 1; i++)
            {
                listViewPositionsClosed.Columns[i].Width = -2;
            }
        }
        finally
        {
            listViewPositionsClosed.EndUpdate();
        }
    }



    private void ListViewPositionClosed_MenuItem_DoubleClick(object sender, EventArgs e)
    {
        if (listViewPositionsClosed.SelectedItems.Count > 0)
        {
            //for (int index = 0; index < listViewPositionsClosed.SelectedItems.Count; index++)
            //{
            ListViewItem item = listViewPositionsClosed.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            switch (GlobalData.Settings.General.DoubleClickAction)
            {
                case DoubleClickAction.activateTradingApp:
                    ActivateTradingApp(position.Symbol, position.Interval);
                    break;
                case DoubleClickAction.activateTradingAppAndTradingViewInternal:
                    //ListViewSignalsMenuItemActivateTradingApps_Click(sender, e);
                    break;
                case DoubleClickAction.activateTradingViewBrowerInternal:
                    //ListViewSignalsMenuItemActivateTradingViewInternal_Click(sender, e);
                    break;
                case DoubleClickAction.activateTradingViewBrowerExternal:
                    //ListViewSignalsMenuItemActivateTradingviewExternal_Click(sender, e);
                    break;
            }
            //}
        }
    }

    //private void ListBoxSymbolsMenuItemPositionCalculate_Click(object sender, EventArgs e)
    //{
    //    if (listViewPositionsClosed.SelectedItems.Count > 0)
    //    {
    //        ListViewItem item = listViewPositionsClosed.SelectedItems[0];
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

    //    //if (GlobalData.ExchangeListName.TryGetValue("Binance", out CryptoSbmScanner.Model.CryptoExchange exchange))
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
    //    //    if (GlobalData.ExchangeListName.TryGetValue("Binance", out CryptoSbmScanner.Model.CryptoExchange exchange))
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

    private void ContextMenuStripPositionsOpenRecalculate_Click(object sender, EventArgs e)
    {
        if (listViewPositionsClosed.SelectedItems.Count > 0)
        {
            ListViewItem item = listViewPositionsClosed.SelectedItems[0];
            CryptoPosition position = (CryptoPosition)item.Tag;

            PositionTools.CalculatePositionViaTrades(databaseMain, position);

            ListViewItem.ListViewSubItem subItem;

            // profit bedrag
            subItem = item.SubItems[11];
            subItem.Text = position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat);
            if (position.Profit > position.Invested)
                subItem.ForeColor = Color.Green;
            else if (position.Profit < position.Invested)
                subItem.ForeColor = Color.Red;

            // profit percentage
            subItem = item.SubItems[12];
            subItem.Text = position.Percentage.ToString("N2");
            if (position.Profit > position.Invested)
                subItem.ForeColor = Color.Green;
            else if (position.Profit < position.Invested)
                subItem.ForeColor = Color.Red;

            listViewPositionsClosed.Invalidate();
        }

    }

    private void DebugDumpToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (listViewPositionsClosed.SelectedItems.Count > 0)
        {
            //ListViewItem item = listViewPositionsClosed.SelectedItems[0];
            //CryptoPosition position = (CryptoPosition)item.Tag;

            //using (CryptoDatabase databaseThread = new())
            //{
            //    databaseThread.Open();
            //    try
            //    {
            //        position.Steps.Clear();
            //        string sql = string.Format("select * from positionstep where PositionId={0} order by Id", position.Id);
            //        foreach (CryptoPositionStep step in databaseThread.Connection.Query<CryptoPositionStep>(sql))
            //        {
            //            if (step.OrderId.HasValue)
            //                position.Orders.Add((long)step.OrderId, step);
            //            if (step.Order2Id.HasValue)
            //                position.Orders.Add((long)step.Order2Id, step);
            //        }

            //        await TradeTools.RefreshTrades(databaseThread, position);


            //        StringBuilder strings = new();
            //        TradeTools.DumpPosition(position, strings);
            //        GlobalData.AddTextToLogTab(strings.ToString());
            //    }
            //    finally
            //    {
            //        databaseThread.Close();
            //    }
            //}
        }
    }

}

