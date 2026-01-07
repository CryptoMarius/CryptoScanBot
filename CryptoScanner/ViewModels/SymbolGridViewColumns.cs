using CryptoScanner.Core.Zones;

using System.Collections;

namespace CryptoScanner.ViewModels;


public enum SymbolColumnEnum
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
    private SymbolColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public SymbolColumnComparer(SymbolColumnEnum? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is SymbolViewModel a && y is SymbolViewModel b)
        {

            try
            {
                int compareResult = SortColumn switch
                {
                    SymbolColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                    SymbolColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol, b.Symbol),
                    SymbolColumnEnum.Volume => ObjectCompare.Compare(a.Volume, b.Volume),
                    //SymbolColumnEnum.Price => ObjectCompare.Compare(a.LastPrice, b.LastPrice),
                    SymbolColumnEnum.Distance => ObjectCompare.Compare(ZoneTools.ZoneDistance(a.Object), ZoneTools.ZoneDistance(b.Object)),
                    //SymbolColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(MarketTrendPrimary(a), MarketTrendPrimary(b)),
                    _ => 0
                };


                // secondary sort
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);


                //// Calculate correct return value based on object comparison
                //if (SortDirection == ListSortDirection.Ascending)
                //    return +compareResult;
                //else if (SortDirection == ListSortDirection.Descending)
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
