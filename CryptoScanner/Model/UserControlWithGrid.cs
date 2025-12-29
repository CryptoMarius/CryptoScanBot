using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using CryptoScanner.Services;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

using System.ComponentModel;

namespace CryptoScanner.Model;

public abstract partial class UserControlWithGrid<T> : UserControl
{
    internal const double HeaderHeight = 30.0;
    internal string _gridName = string.Empty;
    internal DataGrid _dataGrid { get; set; } = null!;

    internal string? _currentSortColumn;
    internal ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

    internal ApplicationStateService _applicationStateService { get; set;  }


    internal abstract void ShowRowContextMenu(DataGrid dataGrid);
    internal abstract void ShowHeaderContextMenu(DataGrid dataGrid);


    internal void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    internal void DataGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"DataGrid_Loaded {_gridName} {_currentSortColumn} {_currentSortDirection}");

        _dataGrid.Sorting += OnDataGridSorting;
        _dataGrid.ColumnReordered += OnColumnReordered;
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

}
