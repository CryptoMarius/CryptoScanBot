using CryptoScanner.Symbol.Model;

using System.Collections;

namespace CryptoScanner.Symbol.Common;

public enum GridSortDirection
{
    Ascending,
    Descending,
}

public enum GridColumnAlignment
{
    Left,
    Center,
    Right,
}

public enum GridColumn
{
    Id,
    Symbol,
    Volume,
    //Price
    Distance,
    //MarketTrendPrimary, to much cpu needed
}

// Helper class for column configuration
public class GridColumnDefinition
{
    public GridColumn Column { get; set; }
    public string Caption { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(string);
    public GridColumnAlignment Align { get; set; } = GridColumnAlignment.Left;

    // Other attributes
    public int Index { get; set; }
    public int Width { get; set; }
    public bool Visible { get; set; } = true;
    public string Format { get; set; } = string.Empty;
}


public class GridColumnDefinitions
{
    private static readonly CaseInsensitiveComparer ObjectCompare = new();

    public GridColumnDefinition? SortColumn { get; set; }
    public GridSortDirection? SortDirection { get; set; }
    public Dictionary<GridColumn, GridColumnDefinition> Columns { get; set; } = [];


    public GridColumnDefinition CreateColumn(GridColumn column, string caption, Type type, string format, 
        GridColumnAlignment align, int width = 0, bool visible = false)
    {
        GridColumnDefinition c = new()
        {
            Column = column,
            Caption = caption,
            Type = type,
            Align = align,
            Visible = visible,
            Width = width,
            Format = format
        };

        //c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        //if (width > 0)
        //{
        //    c.Width = width;
        //    c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // NotSet; // AllCellsExceptHeader; // AllCells; //
        //}
        //else
        //{
        //    c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader; // AllCells; // NotSet; // AllCellsExceptHeader; // AllCells; //
        //    c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // NotSet; // AllCellsExceptHeader; // AllCells; //
        //}
        //c.DefaultCellStyle.Format = format;
        //c.DefaultCellStyle.Alignment = align;
        Columns.Add(column, c);
        return c;
    }


    /// <summary>
    /// Create the definition for the default Columns
    /// </summary>
    public void DefaultColumnDefinition()
    {
        if (SortDirection == null)
            SortDirection = GridSortDirection.Ascending;
        //if (SortColumn == null || (int)SortDirection > Enum.GetNames<ColumnEnum>().Length)
        //    SortColumn = ColumnEnum.Symbol;

        var columns = Enum.GetValues<GridColumn>();
        foreach (GridColumn column in columns)
        {
            switch (column)
            {
                case GridColumn.Id:
                    CreateColumn(column, "Id", typeof(string), string.Empty, GridColumnAlignment.Center, 50).Visible = false;
                    break;
                case GridColumn.Symbol:
                    CreateColumn(column, "Symbol", typeof(string), string.Empty, GridColumnAlignment.Left, 100, true);
                    break;
                case GridColumn.Volume:
                    CreateColumn(column, "Volume", typeof(decimal), "#,##0", GridColumnAlignment.Right, 75);
                    break;
                case GridColumn.Distance:
                    CreateColumn(column, "Distance", typeof(decimal), "##0.#0", GridColumnAlignment.Right, 75).Visible = false;
                    break;
            }
        }

    }


    //private void SortByColumn(ColumnEnum column, GridSortDirection direction)
    //{
    //    IOrderedEnumerable<SymbolInfo> sorted;

    //    if (direction == GridSortDirection.Ascending)
    //    {
    //        sorted = column switch
    //        {
    //            ColumnEnum.Id => Symbols.OrderBy(s => s.Id),
    //            ColumnEnum.Symbol => Symbols.OrderBy(s => s.Symbol),
    //            ColumnEnum.Volume => Symbols.OrderBy(s => s.Volume),
    //            ColumnEnum.Distance => Symbols.OrderBy(s => s.Distance),
    //            _ => Symbols.OrderBy(s => s.Id)
    //        };
    //    }
    //    else
    //    {
    //        sorted = column switch
    //        {
    //            ColumnEnum.Id => Symbols.OrderByDescending(s => s.Id),
    //            ColumnEnum.Symbol => Symbols.OrderByDescending(s => s.Symbol),
    //            ColumnEnum.Volume => Symbols.OrderByDescending(s => s.Volume),
    //            ColumnEnum.Distance => Symbols.OrderByDescending(s => s.Distance),
    //            _ => Symbols.OrderByDescending(s => s.Id)
    //        };
    //    }

    //    Symbols = new ObservableCollection<SymbolInfo>(sorted);
    //}

    public int Compare(SymbolInfo a, SymbolInfo b)
    {
        if (SortColumn == null || SortDirection == null)
            return 0;

        try
        {
            int compareResult = SortColumn.Column switch
            {
                GridColumn.Id => ObjectCompare.Compare(a.Id, b.Id),
                GridColumn.Symbol => ObjectCompare.Compare(a.Symbol, b.Symbol),
                GridColumn.Volume => ObjectCompare.Compare(a.Volume, b.Volume),
                //ColumnsForGrid.Price => ObjectCompare.Compare(a.LastPrice, b.LastPrice),
                //ColumnEnum.Distance => ObjectCompare.Compare(ZoneTools.ZoneDistance(a), ZoneTools.ZoneDistance(b)),
                //ColumnsForGrid.MarketTrendPrimary => ObjectCompare.Compare(MarketTrendPrimary(a), MarketTrendPrimary(b)),
                _ => 0
            };


            // secondary sort
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);


            // Calculate correct return value based on object comparison
            if (SortDirection == GridSortDirection.Ascending)
                return +compareResult;
            else if (SortDirection == GridSortDirection.Descending)
                return -compareResult;
            else
                return 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
