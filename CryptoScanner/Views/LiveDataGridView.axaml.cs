using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
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

        // Runtime - get service from App
        _applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        _dataGrid = this.FindControl<DataGrid>("LiveDataDataGrid")
            ?? throw new InvalidOperationException("LiveDataDataGrid not found");

        DataContextChanged += OnDataContextChanged;

        InitializeGrid<LiveDataColumnEnum, LiveDataColumnComparer>("Date", ListSortDirection.Ascending);
    }


    private LiveDataGridViewModel? _currentViewModel;
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnDataContextChanged {_gridName} {_currentSortColumn} {_currentSortDirection}");

        // Unsubscribe old
        if (_currentViewModel != null)
        {
            _currentViewModel.RequestSort -= OnRequestSort;
            _currentViewModel.RequestSortedInsert -= OnRequestSortedInsert;
        }

        // Subscribe new
        if (DataContext is LiveDataGridViewModel vm)
        {
            _currentViewModel = vm;
            vm.RequestSort += OnRequestSort;
            vm.RequestSortedInsert += OnRequestSortedInsert;
        }
    }

}