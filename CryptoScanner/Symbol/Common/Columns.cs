using CryptoScanner.Symbol.Model;

using System.Collections;

namespace CryptoScanner.Symbol.Common;


public enum GridColumn
{
    Id,
    Symbol,
    Volume,
    //Price
    Distance,
    //MarketTrendPrimary, to much cpu needed
}



public class SymbolColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private GridColumn? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public SymbolColumnComparer(GridColumn? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is SymbolInfo a && y is SymbolInfo b)
        {

            try
            {
                int compareResult = SortColumn switch
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


                //// Calculate correct return value based on object comparison
                //if (SortDirection == GridSortDirection.Ascending)
                //    return +compareResult;
                //else if (SortDirection == GridSortDirection.Descending)
                //    return -compareResult;
                //else
                //    return 0;

                return compareResult;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        return 0;
    }
}
