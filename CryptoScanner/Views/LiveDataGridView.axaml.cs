using Avalonia.Controls;

using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.Views;

public partial class LiveDataGridView : UserControlWithGrid<LiveDataViewModel>
{

    public LiveDataGridView()
    {
        _targetMenu = TargetMenu.LiveData;
        _gridName = "LiveDataGrid";
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = LiveDataDataGrid;
        if (_dataGrid == null)
            throw new InvalidOperationException("LiveDataDataGrid not found");

        InitializeGrid<LiveDataColumnEnum, LiveDataColumnComparer>("Date", ListSortDirection.Descending);
    }

}