using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.ComponentModel;

using CryptoScanner.Symbol.ViewModels;
using CryptoScanner.Symbol.Common;

namespace CryptoScanner.Symbol.Views
{
    public partial class SymbolGridView : UserControl
    {
        private const double HeaderHeight = 30.0;
        private readonly DataGrid? _dataGrid;
        private bool _isSorting = false;

        public SymbolGridView()
        {
            InitializeComponent();

            // Voeg sorting event handler toe
            _dataGrid = this.FindControl<DataGrid>("SymbolsDataGrid");
            if (_dataGrid != null)
            {
                _dataGrid.Sorting += OnDataGridSorting;
                _dataGrid.Loaded += OnDataGridLoaded;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataGridLoaded(object? sender, RoutedEventArgs e)
        {
            // Herstel sort indicator na laden
            if (DataContext is SymbolGridViewModel vm && _dataGrid != null)
            {
                UpdateSortIndicators(vm);
            }
        }

        private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
        {
            if (_isSorting)
                return;

            _isSorting = true;

            try
            {
                if (DataContext is SymbolGridViewModel vm)
                {
                    // Laat ViewModel de sort afhandelen
                    vm.OnSorting(sender, e);

                    // Update sort indicators na de sort
                    UpdateSortIndicators(vm);
                }
            }
            finally
            {
                _isSorting = false;
            }
        }

        private void UpdateSortIndicators(SymbolGridViewModel vm)
        {
            if (_dataGrid == null)
                return;

            var config = vm.GetColumnConfig(GridColumn.Id).Visible; // Dummy call om Columns te krijgen

            // We hebben de Columns nodig - voeg publieke property toe aan ViewModel
            // Voor nu: haal sort info op uit de kolom configuratie

            foreach (var column in _dataGrid.Columns)
            {
                var sortMemberPath = column is DataGridBoundColumn bc
                    ? bc.SortMemberPath
                    : (column as DataGridTemplateColumn)?.SortMemberPath;

                if (!string.IsNullOrEmpty(sortMemberPath) &&
                    Enum.TryParse<GridColumn>(sortMemberPath, true, out GridColumn gridColumn))
                {
                    var colConfig = vm.GetColumnConfig(gridColumn);

                    // Check of dit de gesorteerde kolom is
                    var sortedColumn = vm.GetSortColumn();
                    var sortDirection = vm.GetSortDirection();

                    if (sortedColumn != null && sortedColumn.Column == gridColumn && sortDirection != null)
                    {
                        var avaloniaDirection = sortDirection == GridSortDirection.Ascending
                            ? ListSortDirection.Ascending
                            : ListSortDirection.Descending;

                        // Gebruik reflection om SortDirection te zetten zonder event te triggeren
                        var prop = column.GetType().GetProperty("SortDirection");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(column, avaloniaDirection);
                        }
                    }
                    else
                    {
                        // Clear sort direction
                        var prop = column.GetType().GetProperty("SortDirection");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(column, null);
                        }
                    }
                }
            }
        }

        private void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                var dataGrid = this.FindControl<DataGrid>("SymbolsDataGrid");
                if (dataGrid != null)
                {
                    var gridPoint = e.GetPosition(dataGrid);
                    if (gridPoint.Y < HeaderHeight)
                    {
                        if (DataContext is SymbolGridViewModel vm)
                        {
                            //var window = new ColumnVisibilityWindow { DataContext = vm };
                            //window.Show();
                        }
                    }
                    else
                    {
                        e.Handled = false; // Laat ContextFlyout voor rij
                    }
                }
            }
        }

        private void OnLaunchExternal(object? sender, RoutedEventArgs e)
        {
            var dataGrid = this.FindControl<DataGrid>("SymbolsDataGrid");
            if (dataGrid != null && dataGrid.SelectedItem != null)
            {
                if (DataContext is SymbolGridViewModel vm)
                    vm.LaunchExternal();
            }
        }
    }
}