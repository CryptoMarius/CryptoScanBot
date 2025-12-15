using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

using CryptoScanner.Signal.Common;
using CryptoScanner.Signal.Model;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.Signal.Views;

public partial class SignalGridView : UserControl
{
    private const double HeaderHeight = 30.0;

    private readonly DataGrid _dataGrid;

    private string? _currentSortColumn;
    private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

    public SignalGridView()
    {
        InitializeComponent();

        _dataGrid = this.FindControl<DataGrid>("SignalDataGrid")
            ?? throw new InvalidOperationException("SignalDataGrid not found");

        _dataGrid.Loaded += DataGrid_Loaded; // - restore layout and sort

        // Register a custom comparer for each column based on its SortMemberPath
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<ColumnEnum>(column.SortMemberPath, out ColumnEnum a))
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
        _dataGrid.Sorting += OnDataGridSorting;
        _dataGrid.ColumnReordered += OnColumnReordered;
        _dataGrid.ColumnDisplayIndexChanged += OnColumnDisplayIndexChanged;
        _dataGrid.AddHandler(PointerPressedEvent, OnDataGridPointerPressed, RoutingStrategies.Tunnel);
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }



    private void SaveGridState()
    {
        // Access the service via App.GridStateService
        App.GridStateService.SaveGridState("SignalGrid", _dataGrid, _currentSortColumn, _currentSortDirection);
    }

    private void RestoreGridState()
    {
        // Access the service via App.GridStateService
        App.GridStateService.RestoreGridState("SignalGrid", _dataGrid, out _currentSortColumn, out _currentSortDirection);

        // Apply the sort to the collection
        if (!string.IsNullOrEmpty(_currentSortColumn))
        {
            ApplySortToCollection(_currentSortColumn, _currentSortDirection);
        }

        MarkSortedColumn();
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


    private void MarkSortedColumn()
    {
        // this does not work unfortunately
        //return;
        foreach (var c in _dataGrid.Columns)
        {
            //string headerText = c.Header?.ToString() ?? "";

            //c.HeaderTemplate.FontWeight = FontWeight.Bold;

            if (c.SortMemberPath == _currentSortColumn)
            {
                if (c.Header is DataGridColumnHeader column2)
                {
                    //DataGridColumnHeader HeaderCell
                    //c.HeaderCell.FontWeight = FontWeight.Bold;
                    //column2.FontWeight = FontWeight.Bold;
                    //column2.Header.
                    //column2.Header = new TextBlock
                    //{
                    //    Text = headerText,
                    //    FontWeight = FontWeight.Bold,
                    //    TextAlignment = TextAlignment.Center
                    //};
                }
            }
            else
            {
                if (c.Header is DataGridColumnHeader column2)
                {
                    column2.FontWeight = FontWeight.Normal;
                    //column2.Header = new TextBlock
                    //{
                    //    Text = headerText,
                    //    FontWeight = FontWeight.Normal,
                    //    TextAlignment = TextAlignment.Center
                    //};
                }
            }
        }
    }

    private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (e.Column.SortMemberPath != null)
        {
            var direction = (_currentSortColumn == e.Column.SortMemberPath &&
                            _currentSortDirection == ListSortDirection.Ascending)
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            _currentSortColumn = e.Column.SortMemberPath;
            _currentSortDirection = direction;
            MarkSortedColumn();
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

        //var source = e.Source as Control;
        //var header = source?.FindAncestorOfType<DataGridColumnHeader>();
        //if (header?.DataContext is DataGridColumn column)
        //{
        //    // Komt nooit tot hier...
        //    if (column.SortMemberPath != null)
        //    {
        //        // Toggle direction
        //        var direction = (_currentSortColumn == column.SortMemberPath &&
        //                       _currentSortDirection == ListSortDirection.Ascending)
        //            ? ListSortDirection.Descending
        //            : ListSortDirection.Ascending;

        //        _currentSortColumn = column.SortMemberPath;
        //        _currentSortDirection = direction;
        //        MarkSortedColumn();
        //    }
        //}

        //var source = e.Source as Control;
        //DataGridColumnHeader header = source?.FindAncestorOfType<DataGridColumnHeader>();
        //if (header?.Column != null)
        //{
        //    var column = header.Column;

        //    // ***1 Komt nu wel hier!
        //    if (column.SortMemberPath != null)
        //    {
        //        var direction = (_currentSortColumn == column.SortMemberPath &&
        //                       _currentSortDirection == ListSortDirection.Ascending)
        //            ? ListSortDirection.Descending
        //            : ListSortDirection.Ascending;
        //        _currentSortColumn = column.SortMemberPath;
        //        _currentSortDirection = direction;
        //        MarkSortedColumn();
        //    }
        //}

        //var source = e.Source as Control;
        //var header = source?.FindAncestorOfType<DataGridColumnHeader>();
        //if (header != null)
        //{
        //    var columnIndex = _dataGrid.Columns.IndexOf(_dataGrid.Columns.FirstOrDefault(c => c.Header == header.Content));
        //    if (columnIndex >= 0)
        //    {
        //        var column = _dataGrid.Columns[columnIndex];
        //        if (column.SortMemberPath != null)
        //        {
        //            var direction = (_currentSortColumn == column.SortMemberPath &&
        //                           _currentSortDirection == ListSortDirection.Ascending)
        //                ? ListSortDirection.Descending
        //                : ListSortDirection.Ascending;
        //            _currentSortColumn = column.SortMemberPath;
        //            _currentSortDirection = direction;
        //            MarkSortedColumn();
        //        }
        //    }
        //}
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

        flyout.Items.Add(new Separator());

        var resetColumnsItem = new MenuItem { Header = "Reset Columns" };
        resetColumnsItem.Click += (s, args) => ResetColumns();
        flyout.Items.Add(resetColumnsItem);

        flyout.ShowAt(dataGrid, true);
    }


    /// <summary>
    /// Show context menu for rows (signal actions)
    /// </summary>
    private void ShowRowContextMenu(DataGrid dataGrid)
    {
        var flyout = new MenuFlyout();

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


    private void ApplySortToCollection(string? sortMemberPath, ListSortDirection sortDirection)
    {
        if (!string.IsNullOrEmpty(sortMemberPath))
        {
            _currentSortColumn = sortMemberPath;
            _currentSortDirection = sortDirection;

            // Problem: GEEN indicator tot eerste click
            if (_dataGrid.ItemsSource is ObservableCollection<SignalInfo> collection)
            {
                var column = _dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == sortMemberPath);
                if (column != null)
                {
                    var sorted = collection.ToArray(); // Naar array

                    Array.Sort(sorted, column.CustomSortComparer);

                    // Reverse als descending
                    if (_currentSortDirection == ListSortDirection.Descending)
                        Array.Reverse(sorted);

                    collection.Clear();
                    foreach (var item in sorted)
                        collection.Add(item);
                }
            }
        }
    }
}

