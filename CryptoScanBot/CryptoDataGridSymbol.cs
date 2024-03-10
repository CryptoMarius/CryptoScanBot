﻿using CryptoScanBot.Commands;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;

namespace CryptoScanBot;

public class CryptoDataGridSymbol<T>(DataGridView grid, List<T> list, SortedList<string, ColumnSetting> columnList) : 
    CryptoDataGrid<T>(grid, list, columnList) where T : CryptoSymbol
{
    private enum ColumnsForGrid
    {
        //Exchange,
        Symbol,
        Volume,
        //Price
    }

    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        AddCommand(menuStrip, this, "Activate trading app", Command.ActivateTradingApp, CommandTools.ExecuteCommandCommandViaTag);
        AddCommand(menuStrip, this, "TradingView internal", Command.ActivateTradingviewIntern, CommandTools.ExecuteCommandCommandViaTag);
        AddCommand(menuStrip, this, "TradingView external", Command.ActivateTradingviewExtern, CommandTools.ExecuteCommandCommandViaTag);
        menuStrip.Items.Add(new ToolStripSeparator());
        AddCommand(menuStrip, this, "Copy symbol name", Command.CopySymbolInformation, CommandTools.ExecuteCommandCommandViaTag);
        AddCommand(menuStrip, this, "Trend information (log)", Command.ShowTrendInformation, CommandTools.ExecuteCommandCommandViaTag);
        AddCommand(menuStrip, this, "Symbol information (Excel)", Command.ExcelSymbolInformation, CommandTools.ExecuteCommandCommandViaTag);
    }


    public override void InitializeHeaders()
    {
        SortOrder = SortOrder.Ascending;
        SortColumn = (int)ColumnsForGrid.Symbol;

        var columns = Enum.GetValues(typeof(ColumnsForGrid));
        foreach (ColumnsForGrid column in columns)
        {
            DataGridViewTextBoxColumn _ = column switch
            {
                ColumnsForGrid.Symbol => CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft, 100),
                ColumnsForGrid.Volume => CreateColumn("Volume", typeof(decimal), "#,##0", DataGridViewContentAlignment.MiddleRight, 75),
                //ColumnsForGrid.Price => CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight, 70),
                _ => throw new NotImplementedException(),
            };
        }

    }


    private int Compare(CryptoSymbol a, CryptoSymbol b)
    {
        int compareResult = (ColumnsForGrid)SortColumn switch
        {
            ColumnsForGrid.Symbol => ObjectCompare.Compare(a.Name, b.Name),
            ColumnsForGrid.Volume => ObjectCompare.Compare(a.Volume, b.Volume),
            //ColumnsForGrid.Price => ObjectCompare.Compare(a.LastPrice, b.LastPrice),
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
        List.Sort(Compare);
    }


    public override void GetTextFunction(object sender, DataGridViewCellValueEventArgs e)
    {
        CryptoSymbol symbol = GetCellObject(e.RowIndex);
        if (symbol != null)
        {           
            e.Value = (ColumnsForGrid)e.ColumnIndex switch
            {
                ColumnsForGrid.Symbol => symbol.Name,
                ColumnsForGrid.Volume => symbol.Volume.ToString("N0"),
                //ColumnsForGrid.Price => symbol.LastPrice?.ToString(symbol.PriceDisplayFormat),
                _ => '?',
            };
        }
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
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
        CryptoSymbol symbol = GetCellObject(e.RowIndex);
        if (symbol != null)
        {
            switch ((ColumnsForGrid)e.ColumnIndex)
            {
                case ColumnsForGrid.Volume:
                    if (symbol.Volume >= symbol.QuoteData.MinimalVolume)
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
