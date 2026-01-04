using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Model;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;


namespace CryptoScanner.Views;

public partial class PositionClosedGridView : UserControlWithGrid<PositionViewModel>
{
    public PositionClosedGridView()
    {
        _gridName = "PositionClosedGrid";
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

        _dataGrid = this.FindControl<DataGrid>("SymbolDataGrid")
            ?? throw new InvalidOperationException("SymbolDataGrid not found");

        DataContextChanged += OnDataContextChanged;
        _dataGrid.Loaded += DataGrid_Loaded; // - restore layout and sort

        //// Kind of Hacky, needs work... (is it really needed?)
        //Unloaded += (s, e) =>
        //{
        //    if (DataContext is PositionClosedGridViewModel vm)
        //    {
        //        vm.RequestSort -= OnRequestSort;
        //        vm.RequestSortedInsert -= OnRequestSortedInsert;
        //    }
        //};

        // Register a custom comparer for each column based on its SortMemberPath
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<PositionClosedColumnEnum>(column.SortMemberPath, out PositionClosedColumnEnum a))
            {
                var comparer = new PositionClosedColumnComparer(a);
                column.CustomSortComparer = comparer;
            }
            else
                System.Diagnostics.Debug.WriteLine($"Column comparer for {_gridName} {column} {column.SortMemberPath} not set");
        }

        // Restore grid state from the service
        RestoreGridState();
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


    internal override void ShowRowContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();
        AddStandardGridRowCommands(flyout, TargetViewModel.Position);
        flyout.ShowAt(dataGrid, true);
    }

}