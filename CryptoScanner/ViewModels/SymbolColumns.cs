using Avalonia.Controls;
using Avalonia.Layout;

using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

public class SymbolColumnComparer : IGridComparer<CryptoSymbol, SymbolColumnEnum>
{
    public SymbolColumnEnum SortColumn { get; set; }
    public ListSortDirection SortDirection { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public int Compare(CryptoSymbol? a, CryptoSymbol? b)
    {
        if (a == null || b == null)
            return 0;
        
        try
        {
            int compareResult = SortColumn switch
            {
                SymbolColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                SymbolColumnEnum.Symbol => ObjectCompare.Compare(a.Name, b.Name),
                SymbolColumnEnum.Volume => ObjectCompare.Compare(a.Volume, b.Volume),
                //SymbolColumnEnum.Price => ObjectCompare.Compare(a.LastPrice, b.LastPrice),
                SymbolColumnEnum.Distance => ObjectCompare.Compare(ZoneTools.ZoneDistance(a), ZoneTools.ZoneDistance(b)),
                //SymbolColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(MarketTrendPrimary(a), MarketTrendPrimary(b)),
                _ => 0
            };


            // secondary sort
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Name, b.Name);

            if (SortDirection == ListSortDirection.Descending)
                return -compareResult;
            else
                return compareResult;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}


public static class SymbolColumns
{
    public static ObservableCollection<GridColumnDefinition<SymbolColumnEnum>> GetColumns()
    {
        var columns = new ObservableCollection<GridColumnDefinition<SymbolColumnEnum>>
        {
            new() { ColumnEnum = SymbolColumnEnum.Id, Header = "Id", Width = 50, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = SymbolColumnEnum.Symbol, Header = "Symbol", Width = 80, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SymbolColumnEnum.Volume, Header = "Volume", Width = 100, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SymbolColumnEnum.Distance, Header = "Distance", Width = 70, Alignment = HorizontalAlignment.Left},
        };

        // Initialize DisplayIndex
        int index = 0;
        foreach (var column in columns)
        {
            column.ActualWidth = new GridLength(column.Width);
            column.DisplayIndex = index;
            index++;
        }
        return columns;
    }

}