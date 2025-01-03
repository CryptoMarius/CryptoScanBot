﻿using CryptoScanBot.Commands;
using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.ZoneVisualisation;

namespace CryptoScanBot;

public class CryptoDataGridSymbol<T>() : CryptoDataGrid<T>() where T : CryptoSymbol
{
    private enum ColumnsForGrid
    {
        Id,
        //Exchange,
        Symbol,
        Volume,
        //Price
        Distance,
        //MarketTrend, to much cpu needed
    }

    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        InitializeStandardCommands(menuStrip);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Copy symbol name", Command.CopySymbolInformation);
        menuStrip.AddCommand(this, "Calculate liquidity zones", Command.CalculateSymbolLiquidityZones);

        menuStrip.AddCommand(this, "Export trend information to log", Command.ShowTrendInformation);
        menuStrip.AddCommand(this, "Export symbol information to Excel", Command.ExcelSymbolInformation);
        //menuStrip.AddCommand(this, "Create Position", Command.None, CreatePosition);

        menuStrip.AddSeperator();
        menuStrip.AddCommand(this, "Hide selection", Command.None, ClearSelection);
    }


    public override void InitializeHeaders()
    {
        SortOrder = SortOrder.Ascending;
        SortColumn = (int)ColumnsForGrid.Symbol;

        var columns = Enum.GetValues(typeof(ColumnsForGrid));
        foreach (ColumnsForGrid column in columns)
        {
            switch (column)
            {
                case ColumnsForGrid.Id:
                    CreateColumn("Id", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleCenter, 50).Visible = false;
                    break;
                case ColumnsForGrid.Symbol:
                    CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100, true);
                    break;
                case ColumnsForGrid.Volume:
                    CreateColumn("Volume", typeof(decimal), "#,##0", DataGridViewContentAlignment.MiddleRight, 75);
                    break;
                //case ColumnsForGrid.Price :
                //CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70);
                //break;
                //_ => throw new NotImplementedException(),
                case ColumnsForGrid.Distance:
                    CreateColumn("Distance", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 75).Visible = false;
                    break;
                    //case ColumnsForGrid.MarketTrend:
                    //    CreateColumn("M.Trend%", typeof(decimal), "##0.#0", DataGridViewContentAlignment.MiddleRight, 75);
                    //    break;
            }
        }

    }


    private int Compare(CryptoSymbol a, CryptoSymbol b)
    {
        int compareResult = (ColumnsForGrid)SortColumn switch
        {
            ColumnsForGrid.Id => ObjectCompare.Compare(a.Id, b.Id),
            ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Name, b.Name),
            ColumnsForGrid.Volume => ObjectCompare.Compare(a.Volume, b.Volume),
            //ColumnsForGrid.Price => ObjectCompare.Compare(a.LastPrice, b.LastPrice),
            ColumnsForGrid.Distance => ObjectCompare.Compare(ZoneDistance(a), ZoneDistance(b)),
            //ColumnsForGrid.MarketTrend => ObjectCompare.Compare(MarketTrend(a), MarketTrend(b)),

            _ => 0
        };


        // extend if still the same
        if (compareResult == 0 && SortColumn > 0)
        {
            compareResult = ObjectCompare.Compare(a.Name, b.Name);
        }


        // Calculate correct return value based on object comparison
        if (SortOrder == SortOrder.Ascending)
            return +compareResult;
        else if (SortOrder == SortOrder.Descending)
            return -compareResult;
        else
            return 0;
    }


    public override void SortFunction()
    {
        Monitor.Enter(List);
        try
        {
            List.Sort(Compare);
        }
        finally
        {
            Monitor.Exit(List);
        }
    }


    public override void GetTextFunction(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        CryptoSymbol? symbol = GetCellObject(e.RowIndex);
        if (symbol != null)
        {
            e.Value = (ColumnsForGrid)e.ColumnIndex switch
            {
                ColumnsForGrid.Id => symbol.Id,
                ColumnsForGrid.Symbol => symbol.Name,
                ColumnsForGrid.Volume => symbol.Volume.ToString("N0"),
                //ColumnsForGrid.Price => symbol.LastPrice?.ToString(symbol.PriceDisplayFormat),
                ColumnsForGrid.Distance => ZoneDistance(symbol),
                //ColumnsForGrid.MarketTrend => MarketTrend(symbol),                
                _ => '?',
            };
        }
    }

    private static decimal? ZoneDistance(CryptoSymbol symbol)
    {
        // Set the date of the last swing point for the automatic zone calculation
        AccountSymbolData symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
        AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);

        if (symbolIntervalData.BestLongZone == null && symbolIntervalData.BestShortZone == null)
            return null;
        if (symbolIntervalData.BestLongZone == null)
            return symbolIntervalData.BestShortZone;
        if (symbolIntervalData.BestShortZone == null)
            return symbolIntervalData.BestLongZone;

        return Math.Min(symbolIntervalData.BestLongZone.Value, symbolIntervalData.BestShortZone.Value);
    }


    //private static float? MarketTrend(CryptoSymbol symbol)
    //{
    //    // Set the date of the last swing point for the automatic zone calculation
    //    AccountSymbolData symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
    //    AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);
    //    return symbolData.MarketTrendPercentage;
    //}

    public override void CellFormattingEvent(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (GlobalData.ApplicationIsClosing)
            return;

        // Standard background for the cell (with alternating line color)
        Color backColor;
        if (e.RowIndex % 2 == 0)
        {
            if (GlobalData.Settings.General.BlackTheming)
                backColor = VeryLightGray2;
            else
                backColor = VeryLightGray1;
        }
        else
            backColor = Grid.DefaultCellStyle.BackColor;

        Color foreColor = Color.Black;
        CryptoSymbol? symbol = GetCellObject(e.RowIndex);
        if (symbol != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Volume:
                    if (symbol.Volume >= symbol.QuoteData!.MinimalVolume)
                        foreColor = Color.Green;
                    else
                        foreColor = Color.Red;
                    break;
            }
        }

        DataGridViewCell cell = Grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.Style.BackColor = backColor;
        cell.Style.ForeColor = foreColor;
    }

}
