using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using CryptoScanner.Commands;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Services;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

using System.Collections;
using System.ComponentModel;

namespace CryptoScanner.Model;


public abstract partial class UserControlWithGrid<T> : UserControl where T : class
{
    protected enum TargetMenu
    {
        Log,
        Symbol,
        Position,
        Signal,
        LiveData,
    };

    protected TargetMenu _targetMenu;
    protected string _gridName = string.Empty;

    protected DataGrid _dataGrid { get; set; } = null!;

    internal bool _onDataGridSortingSkipFirst = true;
    internal string _currentSortColumn = string.Empty;
    internal ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

    internal ApplicationStateService _applicationStateService { get; set; } = null!;


    //internal void InitializeComponent()
    //{
    //    AvaloniaXamlLoader.Load(this);
    //}


    internal void DataGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        // Call this method only once..
        _dataGrid.Loaded -= DataGrid_Loaded;

        System.Diagnostics.Debug.WriteLine($"DataGrid_Loaded {_gridName} {_currentSortColumn} {_currentSortDirection}");

        // The grid needs to be fully loaded to have any effect
        var column = _dataGrid.Columns.First(c => c.SortMemberPath == _currentSortColumn);
        column?.Sort(_currentSortDirection);

        _dataGrid.Sorting += OnDataGridSorting; // to early, sorting gets messed up.. --> introduced _onDataGridSortingSkipFirst
        _dataGrid.DoubleTapped += OnDataGridDoubleTapped;
        _dataGrid.AddHandler(PointerPressedEvent, OnDataGridPointerPressed, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Sort the rows that are already in the grid again, with the values they have at this moment,
    /// and put the selection back afterwards. The grid sorts a row once, when it is added or when a
    /// column header is clicked, so a column whose value keeps moving (profit, profit percentage,
    /// duration) drifts out of order while the rows stay where they are.
    /// <para>
    /// Re-sorting under the eyes of the user would make the rows jump around, so this is called
    /// when a tab comes back into view - see the resortWhenShown parameter of InitializeGrid.
    /// </para>
    /// </summary>
    internal void ReapplySort()
    {
        if (_dataGrid?.CollectionView is not { } collectionView || collectionView.SortDescriptions.Count == 0)
            return;

        // Refreshing is not free: it throws every row away and builds it again, which puts the grid
        // back at the top of the list. So first look whether the rows are still in the order the
        // sort asks for - which they are for a column that does not move, the date of a signal for
        // instance, and then there is nothing to do.
        List<object> items = collectionView.Cast<object>().ToList();
        bool inOrder = true;
        for (int i = 1; i < items.Count && inOrder; i++)
            inOrder = CompareBySortDescriptions(collectionView, items[i - 1], items[i]) <= 0;
        if (inOrder)
            return;

        // Refreshing throws the rows away and builds them again, and the selection goes with them.
        List<object> selectedItems = _dataGrid.SelectedItems.Cast<object>().ToList();
        object? selectedItem = _dataGrid.SelectedItem;

        try
        {
            collectionView.Refresh();
        }
        catch (Exception error)
        {
            // The DataGrid can trip over its own index bookkeeping while it rebuilds (the log grid
            // has the same story). The rows themselves are fine, so it is not worth an exception.
            ScannerLog.Logger.Error(error, "");
            return;
        }

        // SelectedItem first: assigning it makes that row the whole selection, so the rest of the
        // selected rows have to be added after it.
        if (selectedItem != null && collectionView.Contains(selectedItem))
            _dataGrid.SelectedItem = selectedItem;
        foreach (object item in selectedItems)
        {
            if (collectionView.Contains(item) && !_dataGrid.SelectedItems.Contains(item))
                _dataGrid.SelectedItems.Add(item);
        }
    }

    /// <summary>
    /// Compare two rows the way the grid is sorted at this moment: the first sort description that
    /// sees a difference decides. The direction is part of the comparer of a description, so a
    /// negative result means the rows are in the right order as the grid shows them.
    /// </summary>
    private static int CompareBySortDescriptions(IDataGridCollectionView collectionView,
        object first, object second)
    {
        foreach (var sortDescription in collectionView.SortDescriptions)
        {
            int result = sortDescription.Comparer.Compare(first, second);
            if (result != 0)
                return result;
        }
        return 0;
    }

    internal void SaveGridState()
    {
        _applicationStateService.SaveGridState(_gridName, _dataGrid, _currentSortColumn, _currentSortDirection);
    }

    internal void RestoreGridState()
    {
        // Access the service via App.ApplicationStateService
        _applicationStateService.RestoreGridState(_gridName, _dataGrid, out _currentSortColumn, out _currentSortDirection);
    }

    internal void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        string text = $"OnDataGridSorting {_gridName} {_currentSortColumn} {_currentSortDirection}";
        if (_onDataGridSortingSkipFirst)
        {
            _onDataGridSortingSkipFirst = false;
            System.Diagnostics.Debug.WriteLine($"{text} direction is {_currentSortDirection} (skipped sorting)");
        }
        else
        {
            if (e.Column.SortMemberPath != null)
            {
                var direction = (_currentSortColumn == e.Column.SortMemberPath &&
                                _currentSortDirection == ListSortDirection.Ascending)
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                _currentSortColumn = e.Column.SortMemberPath;
                _currentSortDirection = direction;
                System.Diagnostics.Debug.WriteLine($"{text} direction is now {_currentSortDirection}");
                SaveGridState();
            }
        }
    }

    /// <summary>
    /// Show the signal column visibility window as a modal dialog
    /// </summary>
    internal async void ShowColumnVisibilityWindow(DataGrid dataGrid)
    {
        if (TopLevel.GetTopLevel(this) is not Window parentWindow)
            return;

        var columnVisibilityWindow = new ColumnWindow
        {
            Title = "Select Visible Columns",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = new ColumnWindowViewModel(dataGrid.Columns)
        };

        await columnVisibilityWindow.ShowDialog(parentWindow);

        // Save settings after user closes the dialog
        SaveGridState();
    }


    //protected void OnRequestSortedInsert(object? sender, T newItem)
    //{
    //    if (!string.IsNullOrEmpty(_currentSortColumn))
    //    {
    //        System.Diagnostics.Debug.WriteLine($"OnRequestSortedInsert {_gridName} {_currentSortColumn} {_currentSortDirection}");

    //        if (_dataGrid.ItemsSource is ObservableCollection<T> collection)
    //        {
    //            var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == _currentSortColumn);
    //            if (column != null)
    //            {
    //                collection.AddItem(newItem, column.CustomSortComparer, _currentSortDirection);
    //            }
    //        }
    //    }
    //}


    //protected void OnRequestSort(object? sender, EventArgs e)
    //{
    //    // Save selected item
    //    var selectedItem = _dataGrid.SelectedItem;

    //    // Re-sort using saved sort column/direction
    //    ApplySortToCollection(_currentSortColumn, _currentSortDirection);

    //    // Restore selected item
    //    if (selectedItem != null)
    //    {
    //        _dataGrid.SelectedItem = selectedItem;
    //        _dataGrid.ScrollIntoView(selectedItem, null);
    //    }
    //}


    //internal void ApplySortToCollection(string sortMemberPath, ListSortDirection sortDirection)
    //{
    //    System.Diagnostics.Debug.WriteLine($"ApplySortToCollection {_gridName} {sortMemberPath} {sortDirection}");

    //    if (!string.IsNullOrEmpty(sortMemberPath))
    //    {
    //        _currentSortColumn = sortMemberPath;
    //        _currentSortDirection = sortDirection;

    //        // Problem: GEEN indicator tot eerste click
    //        if (_dataGrid.ItemsSource is ObservableCollection<T> collection)
    //        {
    //            var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == sortMemberPath);
    //            if (column != null)
    //            {
    //                var sorted = collection.ToArray();
    //                Array.Sort(sorted, column.CustomSortComparer);
    //                if (_currentSortDirection == ListSortDirection.Descending)
    //                    Array.Reverse(sorted);
    //                collection.Replace(sorted);

    //            }
    //        }
    //    }
    //}

    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_dataGrid.SelectedItem != null)
        {
            var parentWindow = this.FindAncestorOfType<Window>();
            if (GlobalData.Settings.General.DoubleClickAction == CryptoDoubleClickAction.ActivateChartForm)
            {
                var command = new CommandShowChart();
                command.Execute((_dataGrid, _dataGrid.SelectedItem, parentWindow));
                e.Handled = true;
            }
            else
            {
                var command = new CommandLaunchTradingAppStandard();
                command.Execute((_dataGrid, _dataGrid.SelectedItem, parentWindow));
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// Handle pointer pressed events on the DataGrid
    /// Right-click on header shows column visibility window
    /// </summary>
    internal void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            var gridPoint = e.GetPosition(_dataGrid);

            // Check if click is in header area (Y < HeaderHeight)
            double HeaderHeight = 30.0;
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


    internal void AddStandardGridHeaderCommands(MenuFlyout flyout)
    {
        var adjustColumnsItem = new MenuItem { Header = "Adjust Columns..." };
        adjustColumnsItem.Click += (s, args) => ShowColumnVisibilityWindow(_dataGrid);
        flyout.Items.Add(adjustColumnsItem);

        //var resetColumnsItem = new MenuItem { Header = "Reset Column width" };
        //resetColumnsItem.Click += (s, args) => ResetColumns();
        //flyout.Items.Add(resetColumnsItem);
    }

    ///// <summary>
    ///// Reset columns to default settings
    ///// </summary>
    //private void ResetColumns()
    //{
    //    _dataGrid.Columns.Clear();
    //    // is dat wel genoeg? Daar krijg je echt de originele index niet mee terug
    //    //try
    //    //{
    //    //    // Delete settings file
    //    //    var settingsPath = GetSettingsFilePath();
    //    //    if (File.Exists(settingsPath))
    //    //        File.Delete(settingsPath);

    //    //    // Reload default settings (you might need to refresh the view)
    //    //    System.Diagnostics.Debug.WriteLine("Column settings reset to defaults");
    //    //}
    //    //catch (Exception ex)
    //    //{
    //    //    System.Diagnostics.Debug.WriteLine($"Error resetting columns: {ex.Message}");
    //    //}
    //}


    /// <summary>
    /// Show context menu for header (column management)
    /// </summary>
    internal virtual void ShowHeaderContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();
        AddStandardGridHeaderCommands(flyout);
        flyout.ShowAt(dataGrid, true);
    }

    internal virtual void ShowRowContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();
        AddStandardGridRowCommands(flyout);
        flyout.ShowAt(dataGrid, true);
    }

    internal void AddStandardGridRowCommands(MenuFlyout flyout)
    {
        var parentWindow = this.FindAncestorOfType<Window>();
        var parameter = (_dataGrid, _dataGrid.SelectedItem, parentWindow);

        if (_targetMenu == TargetMenu.Log)
        {
            // The Log grid has no symbol/position/signal context, so we don't show any of
            // the open/export/calculate items below. Only the Copy-text option is relevant
            // (puts the LogViewModel.Text of the selected row on the clipboard).
            flyout.Items.Add(new MenuItem { Header = "Copy text", Command = new CommandLogCopyText(), CommandParameter = parameter });
            return;
        }

        flyout.Items.Add(new MenuItem { Header = "Open symbol Chart", Command = new CommandShowChart(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open trading app", Command = new CommandLaunchTradingAppStandard(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open Tradingview internal", Command = new CommandLaunchTradingViewInternal(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open Tradingview External", Command = new CommandLaunchTradingViewExternal(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open the exchange", Command = new CommandLaunchExchange(), CommandParameter = parameter });

        if (_targetMenu == TargetMenu.Position)
        {
            flyout.Items.Add(new MenuItem { Header = "-" });
            flyout.Items.Add(new MenuItem { Header = "Position recalculate", Command = new CommandPositionCalculate(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Position delete from database", Command = new CommandPositionDelete(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Position add additional DCA", Command = new CommandPositionCreateAdditionalDca(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Position cancel open DCA", Command = new CommandPositionRemoveAdditionalDca(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Export position information to Excel", Command = new CommandExcelPositionInformation(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Export all position information to Excel", Command = new CommandExcelPositionsInformation(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Delete all positions", Command = new CommandPositionDeleteAll(), CommandParameter = parameter });
        }


        flyout.Items.Add(new MenuItem { Header = "-" });
        flyout.Items.Add(new MenuItem { Header = "Copy symbol name", Command = new CommandCopySymbolName(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Copy all data cells", Command = new CommandCopyDataCells(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Calculate liquidity zones", Command = new CommandCalculateDlzForSymbol(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "-" });
        flyout.Items.Add(new MenuItem { Header = "Export trend information to log", Command = new CommandShowTrendInformation(), CommandParameter = parameter });
        if (_targetMenu == TargetMenu.Signal)
            flyout.Items.Add(new MenuItem { Header = "Export signal information to Excel", Command = new CommandExcelSignalInformation(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Export symbol information to Excel", Command = new CommandExcelSymbolInformation(), CommandParameter = parameter });
        if (_targetMenu == TargetMenu.Signal)
            flyout.Items.Add(new MenuItem { Header = "Export all signal information to Excel", Command = new CommandExcelSignalsInformation(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Export barometer information to Excel", Command = new CommandExcelBarometerInformation(), CommandParameter = parameter });
        if (_targetMenu == TargetMenu.Signal)
            flyout.Items.Add(new MenuItem { Header = "Delete all signals", Command = new CommandSignalDeleteAll(), CommandParameter = parameter });

        flyout.Items.Add(new MenuItem { Header = "-" });
        flyout.Items.Add(new MenuItem { Header = "Hide grid selection", Command = new CommandDatagridHideSelection(), CommandParameter = parameter });
    }


    /// <param name="resortWhenShown">
    /// For grids with columns whose value keeps moving (profit, price change, volume): sort the rows
    /// again every time the tab comes back into view. See <see cref="ReapplySort"/>.
    /// </param>
    internal void InitializeGrid<TEnum, TComparer>(string defaultSortColumn = "",
        ListSortDirection defaultsortDirection = ListSortDirection.Ascending,
        bool resortWhenShown = false) where TEnum : struct, Enum where TComparer : IComparer
    {
        // Runtime - get service from App
        _applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        _dataGrid.Loaded += DataGrid_Loaded;

        // Add a custom onCompare method to each column
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<TEnum>(column.SortMemberPath, out TEnum columnEnum))
            {
                var comparer = (IComparer)Activator.CreateInstance(typeof(TComparer), columnEnum)!;
                column.CustomSortComparer = comparer;
            }
            else
            {
                throw new Exception($"Column comparer for {_gridName} {column.SortMemberPath} not set");
            }
        }

        // Restore grid state from the service (width, column index, sort order etc)
        RestoreGridState();

        // Apply defaults if not saved from the previous session
        if (string.IsNullOrEmpty(_currentSortColumn))
        {
            _currentSortColumn = defaultSortColumn;
            _currentSortDirection = defaultsortDirection;
        }

        // A row is sorted into place when it is added and never again, so a column whose value
        // keeps moving leaves the grid out of order. Sort again when the tab is opened, which
        // happens before it is drawn - the rows are never seen jumping into place, and the user
        // does not get them moving under the mouse while looking at the grid.
        if (resortWhenShown)
            _dataGrid.AttachedToVisualTree += (_, _) => ReapplySort();

        // There is no event for registering the changed widths of the columns.
        // Lets not overcomplicate things, save the columns when shutting down.
        DetachedFromVisualTree += (_, __) =>
        {
            SaveGridState();
        };
    }

}