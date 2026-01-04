using Avalonia.Controls;
using Avalonia.Threading;

using CryptoScanner.Core.Core;
using CryptoScanner.Model;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

public partial class SignalGridView : UserControlWithGrid<SignalViewModel>
{
    public SignalGridView()
    {
        _gridName = "SignalGrid";
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
        _dataGrid.Loaded += DataGrid_Loaded; // Sorting stuff


        // Register a custom comparer for each column based on its SortMemberPath
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<SignalColumnEnum>(column.SortMemberPath, out SignalColumnEnum a))
            {
                var comparer = new SignalColumnComparer(a);
                column.CustomSortComparer = comparer;
            }
            else
                System.Diagnostics.Debug.WriteLine($"Column comparer for {_gridName} {column} {column.SortMemberPath} not set");
        }

        // Restore grid state from the service
        RestoreGridState();
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

        // Post de sort-actie async
        //Dispatcher.UIThread.InvokeAsync(() =>
        {
            System.Diagnostics.Debug.WriteLine($"{_gridName} restore sortindicator {_currentSortColumn} {_currentSortDirection}");

            var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath.ToString() == _currentSortColumn);
            if (column != null)
                column.Sort(_currentSortDirection);
        }; //, DispatcherPriority.Background); // Background zorgt voor delay na render

    }

    /// <summary>
    /// Show context menu for rows (signal actions)
    /// </summary>
    internal override void ShowRowContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();
        AddStandardGridRowCommands(flyout, TargetViewModel.Signal);
        flyout.ShowAt(dataGrid, true);
    }

}