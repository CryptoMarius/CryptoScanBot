using Avalonia.Controls;

using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class PositionClosedGridView : UserControlWithGrid<PositionViewModel>
{
    public PositionClosedGridView()
    {
        _gridName = "PositionClosedGrid";
        _targetMenu = TargetMenu.Position;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = PositionClosedGrid;
        if (_dataGrid == null)
            throw new InvalidOperationException("PositionClosedGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<PositionColumnEnum, PositionColumnComparer>("Created", ListSortDirection.Ascending);
    }

}