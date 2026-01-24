using Avalonia.Controls;

using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class SymbolGridView : UserControlWithGrid<SymbolViewModel>
{
    public SymbolGridView()
    {
        _gridName = "SymbolGrid";
        _targetMenu = TargetMenu.Symbol;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = SymbolDataGrid;
        if (_dataGrid == null)
            throw new InvalidOperationException("SymbolDataGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<SymbolColumnEnum, SymbolColumnComparer>("Symbol", ListSortDirection.Ascending);
    }

}