using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.UI.ViewModels;


public class SymbolColumnComparer : IComparer<SymbolViewModel>
{
    private readonly SymbolColumnEnum _sortColumn;

    public SymbolColumnComparer(SymbolColumnEnum sortColumn)
    {
        _sortColumn = sortColumn;
    }

    public int Compare(SymbolViewModel? x, SymbolViewModel? y)
    {
        if (x == null || y == null)
            return 0;

        try
        {
            int compareResult = _sortColumn switch
            {
                SymbolColumnEnum.Id => string.Compare(x.Id, y.Id, StringComparison.OrdinalIgnoreCase),
                SymbolColumnEnum.Symbol => string.Compare(x.Symbol, y.Symbol, StringComparison.OrdinalIgnoreCase),
                SymbolColumnEnum.ExchangeName => string.Compare(x.ExchangeName, y.ExchangeName, StringComparison.OrdinalIgnoreCase),
                SymbolColumnEnum.Volume => x.Object.Volume.CompareTo(y.Object.Volume),
                SymbolColumnEnum.Distance => Nullable.Compare(
                    ZoneTools.ZoneDistance(x.Object),
                    ZoneTools.ZoneDistance(y.Object)),
                _ => 0
            };

            // Secondary sort on Symbol name (same as Avalonia SymbolColumnComparer)
            if (compareResult == 0 && _sortColumn != SymbolColumnEnum.Symbol)
                compareResult = string.Compare(x.Symbol, y.Symbol, StringComparison.OrdinalIgnoreCase);

            return compareResult;
        }
        catch
        {
            return 0;
        }
    }
}
