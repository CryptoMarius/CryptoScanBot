using Avalonia.Controls;

using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class PositionOpenGridView : UserControlWithGrid<PositionViewModel>
{
    public PositionOpenGridView()
    {
        _gridName = "PositionOpenGrid";
        _targetMenu = TargetMenu.Position;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = PositionOpenGrid;
        if (_dataGrid == null)
            throw new InvalidOperationException("PositionOpenGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<PositionColumnEnum, PositionColumnComparer>("UpdateTime", ListSortDirection.Descending);
    }
}