using Avalonia.Controls;

using CryptoScanner.Core.Core;
using CryptoScanner.Model;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

public partial class LiveDataGridView : UserControlWithGrid<LiveDataViewModel>
{

    public LiveDataGridView()
    {
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
        _dataGrid.Loaded += DataGrid_Loaded; // - restore layout and sort

        // Register a custom comparer for each column based on its SortMemberPath
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<LiveDataColumnEnum>(column.SortMemberPath, out LiveDataColumnEnum a))
            {
                var comparer = new LiveDataColumnComparer(a);
                column.CustomSortComparer = comparer;
            }
            else
                System.Diagnostics.Debug.WriteLine($"Column comparer for {_gridName} {column} {column.SortMemberPath} not set");
        }

        // Restore grid state from the service
        RestoreGridState();
    }




    private LiveDataGridViewModel? _currentViewModel;
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        //System.Diagnostics.Debug.WriteLine($"OnDataContextChanged {_gridName} {_currentSortColumn} {_currentSortDirection}");

        //// Unsubscribe old
        //if (_currentViewModel != null)
        //{
        //    _currentViewModel.RequestSort -= OnRequestSort;
        //    _currentViewModel.RequestSortedInsert -= OnRequestSortedInsert;
        //}

        //// Subscribe new
        //if (DataContext is LiveDataGridViewModel vm)
        //{
        //    _currentViewModel = vm;
        //    vm.RequestSort += OnRequestSort;
        //    vm.RequestSortedInsert += OnRequestSortedInsert;
        //}
    }


    internal override void ShowRowContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();
        AddStandardGridRowCommands(flyout, TargetViewModel.LiveData);
        flyout.ShowAt(dataGrid, true);
    }
}