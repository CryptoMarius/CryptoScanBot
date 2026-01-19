using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class PositionOpenGridView : UserControlWithGrid<PositionViewModel>
{
    public PositionOpenGridView()
    {
        _gridName = "PositionOpenGrid";
        _targetMenu = TargetMenu.Symbol;
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

        _dataGrid = this.FindControl<DataGrid>("PositionOpenGrid")
            ?? throw new InvalidOperationException("PositionOpenGrid not found");

        DataContextChanged += OnDataContextChanged;

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<PositionOpenColumnEnum, PositionOpenColumnComparer>("Created", ListSortDirection.Ascending);
    }


    private PositionOpenGridViewModel? _currentViewModel;
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe old
        if (_currentViewModel != null)
        {
            _currentViewModel.RequestSort -= OnRequestSort;
            _currentViewModel.RequestSortedInsert -= OnRequestSortedInsert;
        }

        // Subscribe new
        if (DataContext is PositionOpenGridViewModel vm)
        {
            _currentViewModel = vm;
            vm.RequestSort += OnRequestSort;
            vm.RequestSortedInsert += OnRequestSortedInsert;
        }
    }


}