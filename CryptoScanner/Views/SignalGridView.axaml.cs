using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class SignalGridView : UserControlWithGrid<SignalViewModel>
{
    public SignalGridView()
    {
        _gridName = "SignalGrid";
        _targetMenu = TargetMenu.Signal;

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

        _dataGrid = this.FindControl<DataGrid>("SignalDataGrid")
            ?? throw new InvalidOperationException("SignalDataGrid not found");

        DataContextChanged += OnDataContextChanged;

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<SignalColumnEnum, SignalColumnComparer>("Date", ListSortDirection.Descending);
    }


    private SignalGridViewModel? _currentViewModel;
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnDataContextChanged {_gridName}");

        // Unsubscribe old
        if (_currentViewModel != null)
        {
            _currentViewModel.RequestSort -= OnRequestSort;
            _currentViewModel.RequestSortedInsert -= OnRequestSortedInsert;
        }

        // Subscribe new
        if (DataContext is SignalGridViewModel vm)
        {
            _currentViewModel = vm;
            vm.RequestSort += OnRequestSort;
            vm.RequestSortedInsert += OnRequestSortedInsert;
        }
    }

}