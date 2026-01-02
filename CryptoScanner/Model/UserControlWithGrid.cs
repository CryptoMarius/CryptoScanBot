using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using CryptoScanner.Commands;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

using System.ComponentModel;
using System.Windows.Input;

namespace CryptoScanner.Model;

public abstract partial class UserControlWithGrid<T> : UserControl
{
    internal const double HeaderHeight = 30.0;
    internal string _gridName = string.Empty;
    internal DataGrid _dataGrid { get; set; } = null!;

    internal string? _currentSortColumn;
    internal ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

    internal ApplicationStateService _applicationStateService { get; set; }


    internal abstract void ShowRowContextMenu(DataGrid dataGrid);


    internal void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    internal void DataGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"DataGrid_Loaded {_gridName} {_currentSortColumn} {_currentSortDirection}");

        _dataGrid.Sorting += OnDataGridSorting;
        _dataGrid.ColumnReordered += OnColumnReordered;
        _dataGrid.DoubleTapped += OnDataGridDoubleTapped;
        _dataGrid.ColumnDisplayIndexChanged += OnColumnDisplayIndexChanged;
        _dataGrid.AddHandler(PointerPressedEvent, OnDataGridPointerPressed, RoutingStrategies.Tunnel);
    }

    internal void SaveGridState()
    {
        // Access the service via App.ApplicationStateService
        _applicationStateService.SaveGridState(_gridName, _dataGrid, _currentSortColumn, _currentSortDirection);
    }

    internal void RestoreGridState()
    {
        // Access the service via App.ApplicationStateService
        _applicationStateService.RestoreGridState(_gridName, _dataGrid, out _currentSortColumn, out _currentSortDirection);
    }

    /// <summary>
    /// Handle column reordering
    /// </summary>
    internal void OnColumnReordered(object? sender, DataGridColumnEventArgs e)
    {
        SaveGridState();
    }

    /// <summary>
    /// Handle column display index changes
    /// </summary>
    internal void OnColumnDisplayIndexChanged(object? sender, DataGridColumnEventArgs e)
    {
        SaveGridState();
    }

    internal void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"OnDataGridSorting {_gridName} {_gridName} {_currentSortColumn} {_currentSortDirection}");
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
    /// Show the signal column visibility window as a modal dialog
    /// </summary>
    internal async void ShowColumnVisibilityWindow(DataGrid dataGrid)
    {
        if (this.GetVisualRoot() is not Window parentWindow)
            return;

        var columnVisibilityWindow = new ColumnWindow
        {
            CanResize = false,
            Title = "Select Visible Columns",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            DataContext = new ColumnWindowViewModel(dataGrid.Columns)
        };

        await columnVisibilityWindow.ShowDialog(parentWindow);

        // Save settings after user closes the dialog
        SaveGridState();
    }


    internal void OnRequestSort(object? sender, EventArgs e)
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


    internal void ApplySortToCollection(string? sortMemberPath, ListSortDirection sortDirection)
    {
        System.Diagnostics.Debug.WriteLine($"ApplySortToCollection {_gridName} {sortMemberPath} {sortDirection}");

        if (!string.IsNullOrEmpty(sortMemberPath))
        {
            _currentSortColumn = sortMemberPath;
            _currentSortDirection = sortDirection;

            // Problem: GEEN indicator tot eerste click
            if (_dataGrid.ItemsSource is ObservableRangeCollection<T> collection)
            {
                var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == sortMemberPath);
                if (column != null)
                {
                    var sorted = collection.ToArray();
                    Array.Sort(sorted, column.CustomSortComparer);
                    if (_currentSortDirection == ListSortDirection.Descending)
                        Array.Reverse(sorted);
                    collection.Replace(sorted);
                }
            }
        }
    }

    internal void OnRequestSortedInsert(object? sender, T newSignal)
    {
        if (!string.IsNullOrEmpty(_currentSortColumn))
        {
            System.Diagnostics.Debug.WriteLine($"OnRequestSortedInsert {_gridName} {_currentSortColumn} {_currentSortDirection}");

            if (_dataGrid.ItemsSource is ObservableRangeCollection<T> collection)
            {
                var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == _currentSortColumn);
                if (column != null)
                {
                    collection.AddItem(newSignal, column.CustomSortComparer, _currentSortDirection);
                }
            }
        }
    }

    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_dataGrid.SelectedItem != null)
        {
            var parentWindow = this.FindAncestorOfType<Window>();
            var command = new CommandLaunchTradingAppStandard();
            command.Execute((_dataGrid, _dataGrid.SelectedItem, parentWindow));



            e.Handled = true;
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

    internal enum TargetViewModel
    {
        Symbol,
        Position,
        Signal,
        LiveData,
    };

    internal void AddStandardGridRowCommands(MenuFlyout flyout, TargetViewModel target)
    {
        var parentWindow = this.FindAncestorOfType<Window>();
        var parameter = (_dataGrid, _dataGrid.SelectedItem, parentWindow);

        flyout.Items.Add(new MenuItem { Header = "Open symbol Chart", Command = new CommandShowGraph(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open trading app", Command = new CommandLaunchTradingAppStandard(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open Tradingview internal", Command = new CommandLaunchTradingViewInternal(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open Tradingview External", Command = new CommandLaunchTradingViewExternal(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Open the exchange", Command = new CommandLaunchTradingViewExternal(), CommandParameter = parameter });

        if (target == TargetViewModel.Position)
        {
            flyout.Items.Add(new MenuItem { Header = "-"});
            flyout.Items.Add(new MenuItem { Header = "Position recalculate", Command = new CommandPositionCalculate(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Position delete from database", Command = new CommandPositionDelete(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Position add additional DCA", Command = new CommandPositionCreateAdditionalDca(), CommandParameter = parameter }); 
            flyout.Items.Add(new MenuItem { Header = "Position cancel open DCA", Command = new CommandPositionRemoveAdditionalDca(), CommandParameter = parameter }); 
            flyout.Items.Add(new MenuItem { Header = "Export position information to Excel", Command = new CommandExcelPositionInformation(), CommandParameter = parameter });
            flyout.Items.Add(new MenuItem { Header = "Export all position information to Excel", Command = new CommandExcelPositionsInformation(), CommandParameter = parameter });
        }

        flyout.Items.Add(new MenuItem { Header = "-"});
        flyout.Items.Add(new MenuItem { Header = "Copy symbol name", Command = new CommandCopySymbolName(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Copy all data cells", Command = new CommandCopyDataCells(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Calculate liquidity zones", Command = new CommandCalculateDlzForSymbol(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "-"});
        flyout.Items.Add(new MenuItem { Header = "Export trend information to log", Command = new CommandShowTrendInformation(), CommandParameter = parameter });
        if (target == TargetViewModel.Signal)
            flyout.Items.Add(new MenuItem { Header = "Export signal information to Excel", Command = new CommandExcelSignalInformation(), CommandParameter = parameter });
        flyout.Items.Add(new MenuItem { Header = "Export symbol information to Excel", Command = new CommandExcelSymbolInformation(), CommandParameter = parameter });
        if (target == TargetViewModel.Signal)
            flyout.Items.Add(new MenuItem { Header = "Export all signal information to Excel", Command = new CommandExcelSignalsInformation(), CommandParameter = parameter });

        flyout.Items.Add(new MenuItem { Header = "-"});
        flyout.Items.Add(new MenuItem { Header = "Hide grid selection", Command = new CommandDatagridHideSelection(), CommandParameter = parameter });
    }


}