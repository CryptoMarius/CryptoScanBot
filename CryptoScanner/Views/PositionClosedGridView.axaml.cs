using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
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

        // Runtime - get service from App
        _applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        _dataGrid = this.FindControl<DataGrid>("PositionClosedGrid")
            ?? throw new InvalidOperationException("PositionClosedGrid not found");

        DataContextChanged += OnDataContextChanged;

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<PositionClosedColumnEnum, PositionClosedColumnComparer>("Created", ListSortDirection.Ascending);
    }


    private PositionClosedGridViewModel? _currentViewModel;
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe old
        if (_currentViewModel != null)
        {
            _currentViewModel.RequestSort -= OnRequestSort;
            _currentViewModel.RequestSortedInsert -= OnRequestSortedInsert;
        }

        // Subscribe new
        if (DataContext is PositionClosedGridViewModel vm)
        {
            _currentViewModel = vm;
            vm.RequestSort += OnRequestSort;
            vm.RequestSortedInsert += OnRequestSortedInsert;
        }
    }


}