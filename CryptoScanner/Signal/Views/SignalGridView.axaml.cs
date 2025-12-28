using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using CryptoScanner.Model;
using CryptoScanner.Services;
using CryptoScanner.Signal.Common;
using CryptoScanner.Signal.Model;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;
using CryptoScanner.Visualisation.ViewModels;
using CryptoScanner.Visualisation.Views;

using System.ComponentModel;

namespace CryptoScanner.Signal.Views;

public partial class SignalGridView : UserControl
{
    private const double HeaderHeight = 30.0;
    private readonly DataGrid _dataGrid;
    private readonly ApplicationStateService _applicationStateService;

    private string? _currentSortColumn;
    private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;


    public SignalGridView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        // Runtime - get service from App
        _applicationStateService = App.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        _dataGrid = this.FindControl<DataGrid>("SignalDataGrid")
            ?? throw new InvalidOperationException("SignalDataGrid not found");

        DataContextChanged += OnDataContextChanged;
        _dataGrid.Loaded += DataGrid_Loaded; // - restore layout and sort

        // Kind of Hacky, needs work...
        Loaded += (s, e) =>
        {
            if (DataContext is SignalGridViewModel vm)
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                vm.SetOwner(owner);
            }
        };

        // Register a custom comparer for each column based on its SortMemberPath
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<GridColumnEnum>(column.SortMemberPath, out GridColumnEnum a))
            {
                var comparer = new SignalColumnComparer(a);
                column.CustomSortComparer = comparer;
            }
            else
                System.Diagnostics.Debug.WriteLine($"Column comparer for {column} not set");
        }
        // Restore grid state from the service
        RestoreGridState();
    }

    private void DataGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"DataGrid_Loaded {_currentSortColumn} {_currentSortDirection}");

        _dataGrid.Sorting += OnDataGridSorting;
        _dataGrid.ColumnReordered += OnColumnReordered;
        _dataGrid.ColumnDisplayIndexChanged += OnColumnDisplayIndexChanged;
        _dataGrid.AddHandler(PointerPressedEvent, OnDataGridPointerPressed, RoutingStrategies.Tunnel);
    }


    private SignalGridViewModel? _currentViewModel;
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
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

    private void OnRequestSort(object? sender, EventArgs e)
    {
        // Bewaar selectie
        var selectedItem = _dataGrid.SelectedItem;

        // Re-sort met huidige sort column/direction
        ApplySortToCollection(_currentSortColumn, _currentSortDirection);

        // Herstel selectie + scroll
        if (selectedItem != null)
        {
            _dataGrid.SelectedItem = selectedItem;
            _dataGrid.ScrollIntoView(selectedItem, null);
        }
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }



    private void SaveGridState()
    {
        // Access the service via App.ApplicationStateService
        _applicationStateService.SaveGridState("SignalGrid", _dataGrid, _currentSortColumn, _currentSortDirection);
    }

    private void RestoreGridState()
    {
        // Access the service via App.ApplicationStateService
        _applicationStateService.RestoreGridState("SignalGrid", _dataGrid, out _currentSortColumn, out _currentSortDirection);
    }


    /// <summary>
    /// Handle column reordering
    /// </summary>
    private void OnColumnReordered(object? sender, DataGridColumnEventArgs e)
    {
        SaveGridState();
    }

    /// <summary>
    /// Handle column display index changes
    /// </summary>
    private void OnColumnDisplayIndexChanged(object? sender, DataGridColumnEventArgs e)
    {
        SaveGridState();
    }


    private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnDataGridSorting {_currentSortColumn} {_currentSortDirection}");
        if (e.Column.SortMemberPath != null)
        {
            var direction = (_currentSortColumn == e.Column.SortMemberPath &&
                            _currentSortDirection == ListSortDirection.Ascending)
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            _currentSortColumn = e.Column.SortMemberPath;
            _currentSortDirection = direction;
            SaveGridState();
        }
    }


    /// <summary>
    /// Handle pointer pressed events on the DataGrid
    /// Right-click on header shows column visibility window
    /// </summary>
    private void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            var gridPoint = e.GetPosition(_dataGrid);

            // Check if click is in header area (Y < HeaderHeight)
            if (gridPoint.Y < HeaderHeight)
            {
                // Header click
                ShowHeaderContextMenu(_dataGrid);
                e.Handled = true;
                return;
            }
            else
            {
                // Row click
                ShowRowContextMenu(_dataGrid);
                e.Handled = true;
                return;
            }
        }
    }



    /// <summary>
    /// Show context menu for header (column management)
    /// </summary>
    private void ShowHeaderContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();

        var adjustColumnsItem = new MenuItem { Header = "Adjust Columns..." };
        adjustColumnsItem.Click += (s, args) => ShowColumnVisibilityWindow(dataGrid);
        flyout.Items.Add(adjustColumnsItem);

        var resetColumnsItem = new MenuItem { Header = "Reset Column width" };
        //resetColumnsItem.Click += (s, args) => ResetColumns();
        flyout.Items.Add(resetColumnsItem);

        flyout.ShowAt(dataGrid, true);
    }


    /// <summary>
    /// Show context menu for rows (signal actions)
    /// </summary>
    private void ShowRowContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();

        MenuItem menuItem;

        menuItem = new MenuItem { Header = "Symbol Chart" };
        menuItem.Command = ((SignalGridViewModel)DataContext!).OpenChartCommand;
        menuItem.CommandParameter = dataGrid.SelectedItem; //menuItem.Tag;
        //menuItem.Click += OnLaunchTradingChartAsync;
        flyout.Items.Add(menuItem);

        menuItem = new MenuItem { Header = "Altrady Binance Futures" };
        menuItem.CommandParameter = dataGrid.SelectedItem; //menuItem.Tag;
        menuItem.Click += OnLaunchTradingApp;
        flyout.Items.Add(menuItem);

        menuItem = new MenuItem { Header = "Tradingview internal" };
        menuItem.CommandParameter = dataGrid.SelectedItem; //menuItem.Tag;
        menuItem.Click += OnLaunchTradingViewInternal;
        flyout.Items.Add(menuItem);

        menuItem = new MenuItem { Header = "Tradingview External" };
        menuItem.CommandParameter = dataGrid.SelectedItem; //menuItem.Tag;
        menuItem.Click += OnLaunchTradingViewExternal;
        flyout.Items.Add(menuItem);

        flyout.Items.Add(new MenuItem { Header = "Goto the exchange" });
        flyout.Items.Add(new MenuItem { Header = "TV + Altrady" });
        flyout.Items.Add(new MenuItem { Header = "-" });
        flyout.Items.Add(new MenuItem { Header = "Copy symbol name" });
        flyout.Items.Add(new MenuItem { Header = "Copy data cells" });
        flyout.Items.Add(new MenuItem { Header = "Calculate  liquidity zones" });
        flyout.Items.Add(new MenuItem { Header = "-" });

        var openExternalItem = new MenuItem { Header = "Open in External Program" };
        openExternalItem.Click += OnLaunchExternal;
        flyout.Items.Add(openExternalItem);

        flyout.Items.Add(new Separator());

        var copyItem = new MenuItem { Header = "Copy Signal" };
        copyItem.Click += (s, args) =>
        {
            if (DataContext is SignalGridViewModel vm && dataGrid.SelectedItem != null)
                vm.CopySignalCommand.Execute(dataGrid.SelectedItem);
        };
        flyout.Items.Add(copyItem);

        flyout.ShowAt(dataGrid, true);
    }



    /// <summary>
    /// Reset columns to default settings
    /// </summary>
    private void ResetColumns()
    {
        _dataGrid.Columns.Clear();
        // is dat wel genoeg? Daar krijg je echt de originele index niet mee terug
        //try
        //{
        //    // Delete settings file
        //    var settingsPath = GetSettingsFilePath();
        //    if (File.Exists(settingsPath))
        //        File.Delete(settingsPath);

        //    // Reload default settings (you might need to refresh the view)
        //    System.Diagnostics.Debug.WriteLine("Column settings reset to defaults");
        //}
        //catch (Exception ex)
        //{
        //    System.Diagnostics.Debug.WriteLine($"Error resetting columns: {ex.Message}");
        //}
    }


    /// <summary>
    /// Show the signal column visibility window as a modal dialog
    /// </summary>
    private async void ShowColumnVisibilityWindow(DataGrid dataGrid)
    {
        if (this.GetVisualRoot() is not Window parentWindow)
            return;

        var columnVisibilityWindow = new ColumnVisibilityWindow
        {
            CanResize = false,
            Title = "Select Visible Columns",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = new ColumnVisibilityViewModel(dataGrid.Columns)
        };

        await columnVisibilityWindow.ShowDialog(parentWindow);

        // Save settings after user closes the dialog
        SaveGridState();
    }

    /// <summary>
    /// Handle launch external program from context menu
    /// </summary>
    private void OnLaunchExternal(object? sender, RoutedEventArgs e)
    {
        if (_dataGrid.SelectedItem != null)
        {
            if (DataContext is SignalGridViewModel vm)
            {
                vm.OpenExternalProgramCommand.Execute(_dataGrid.SelectedItem);
            }
        }
    }

    /// <summary>
    /// Open Altrady or Hypertrader application
    /// </summary>
    private void OnLaunchTradingApp(object? sender, RoutedEventArgs e)
    {
        if (_dataGrid.SelectedItem != null)
        {
            if (DataContext is SignalGridViewModel vm)
            {
                vm.LaunchTradingAppCommand.Execute(_dataGrid.SelectedItem);
            }
        }
    }


    //private async Task OnLaunchTradingChartAsync(object? sender, RoutedEventArgs e)
    //{
    //    if (_dataGrid.SelectedItem != null)
    //    {
    //        if (this.GetVisualRoot() is not Window parentWindow)
    //            return;


    //        if (DataContext is SignalGridViewModel vm)
    //        {
    //            vm.LaunchTradingChartCommand.Execute(_dataGrid.SelectedItem);
    //        }


    //        var visualisationWindow = new VisualisationWindow
    //        {
    //            CanResize = false,
    //            Title = "Chart form",
    //            WindowStartupLocation = WindowStartupLocation.CenterOwner,
    //        };

    //        if (visualisationWindow.DataContext is VisualisationViewModel frm)
    //        {
    //            frm.Initialize(_dataGrid.SelectedItem as SignalInfo);
    //        }

    //        await visualisationWindow.ShowDialog(parentWindow);
    //    }
    //}
    

    private void OnLaunchTradingViewInternal(object? sender, RoutedEventArgs e)
    {
        if (_dataGrid.SelectedItem != null)
        {
            if (DataContext is SignalGridViewModel vm)
            {
                vm.LaunchTradingViewInternalCommand.Execute(_dataGrid.SelectedItem);
            }
        }
    }

    private void OnLaunchTradingViewExternal(object? sender, RoutedEventArgs e)
    {
        if (_dataGrid.SelectedItem != null)
        {
            if (DataContext is SignalGridViewModel vm)
            {
                vm.LaunchTradingViewExternalCommand.Execute(_dataGrid.SelectedItem);
            }
        }
    }

    private void ApplySortToCollection(string? sortMemberPath, ListSortDirection sortDirection)
    {
        System.Diagnostics.Debug.WriteLine($"ApplySortToCollection {sortMemberPath} {sortDirection}");

        if (!string.IsNullOrEmpty(sortMemberPath))
        {
            _currentSortColumn = sortMemberPath;
            _currentSortDirection = sortDirection;

            // Problem: GEEN indicator tot eerste click
            if (_dataGrid.ItemsSource is ObservableRangeCollection<SignalInfo> collection)
            {
                var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == sortMemberPath);
                if (column != null)
                {
                    var sorted = collection.ToArray();

                    Array.Sort(sorted, column.CustomSortComparer);

                    //}

                    if (_currentSortDirection == ListSortDirection.Descending)
                        Array.Reverse(sorted);

                    collection.Replace(sorted);
                }
            }
        }
    }


    private void OnRequestSortedInsert(object? sender, SignalInfo newSignal)
    {
        if (!string.IsNullOrEmpty(_currentSortColumn))
        {
            System.Diagnostics.Debug.WriteLine($"OnRequestSortedInsert {_currentSortColumn} {_currentSortDirection}");

            if (_dataGrid.ItemsSource is ObservableRangeCollection<SignalInfo> collection)
            {
                var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == _currentSortColumn);
                if (column != null)
                {
                    collection.AddItem(newSignal, column.CustomSortComparer, _currentSortDirection);
                }
            }
        }
    }

}