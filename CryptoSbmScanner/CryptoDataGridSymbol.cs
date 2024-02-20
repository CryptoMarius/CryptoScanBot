using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner;

public class CryptoDataGridSymbol<T>(DataGridView grid, List<T> list) : CryptoDataGrid<T>(grid, list) where T : CryptoSymbol
{
    public override void InitializeCommands(ContextMenuStrip menuStrip)
    {
        AddStandardSymbolCommands(menuStrip, false);
        Grid.Tag = Command.ActivateTradingApp;
        Grid.DoubleClick += Commands.ExecuteCommandCommandViaTag;
    }


    public override void InitializeHeaders()
    {
        CreateColumn("Symbol", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleLeft);
        CreateColumn("Volume", typeof(decimal), "#,##0.##", DataGridViewContentAlignment.MiddleRight);
        CreateColumn("Price", typeof(string), string.Empty, DataGridViewContentAlignment.MiddleRight);
    }


    private int Compare(CryptoSymbol a, CryptoSymbol b)
    {
        int compareResult = SortColumn switch
        {
            00 => ObjectCompare.Compare(a.Name, b.Name),
            01 => ObjectCompare.Compare(a.Volume, b.Volume),
            02 => ObjectCompare.Compare(a.LastPrice, b.LastPrice),
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
        CryptoSymbol symbol = List[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => symbol.Name,
            1 => symbol.Volume,
            2 => symbol.LastPrice?.ToString(symbol.PriceDisplayFormat),
            _ => '?',
        };
    }

    public override void RowSetDefaultColor(object sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex % 2 == 0)
        {
            Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = VeryLightGray;
        }
    }


    public override void CellFormattingEvent(object sender, DataGridViewCellFormattingEventArgs e)
    {
        // done by column so it happens once per row
        //if (e.ColumnIndex == Grid.Columns["Interval"].Index)
        //{
        //    //Grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Red;
        //    Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Style = styleRed;
        //}
    }


}
