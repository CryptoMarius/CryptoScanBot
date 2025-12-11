using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using System.ComponentModel;

using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Signal.Common;
using Avalonia.Controls.Templates;

namespace CryptoScanner.Signal.Views
{
    public partial class SignalGridView : UserControl
    {
        private const double HeaderHeight = 30.0;
        private DataGrid? _dataGrid;
        private bool _isSorting = false;

        public SignalGridView()
        {
            InitializeComponent();

            // Voeg sorting event handler toe
            _dataGrid = this.FindControl<DataGrid>("SignalsDataGrid");
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
            // Herstel sort indicator na laden DataContext is SignalGridViewModel vm && 
            if (_dataGrid != null)
            {
                // Werkt niet (tot zover de beperkte input van AI)
                //UpdateSortIndicators(vm);
                if (SignalShared.Columns.Columns.Count == 0)
                    SignalShared.Columns.DefaultColumnDefinition();

                foreach (var column in SignalShared.Columns.Columns.Values)
                {
                    // Maak de Volume kolom aan
                    var dgColumn = new DataGridTemplateColumn
                    {
                        Header = new TextBlock
                        {
                            Text = column.Caption,
                            HorizontalAlignment = HorizontalAlignment.Right
                        },
                        SortMemberPath = column.Column.ToString(),
                        Width = DataGridLength.Auto,
                        IsVisible = true, // Of bind aan ViewModel property

                        CellTemplate = new FuncDataTemplate<object>((value, namescope) =>
                        {
                            var textBlock = new TextBlock
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = column.Align,
                            };

                            // Bind Text met StringFormat
                            textBlock.Bind(TextBlock.TextProperty, new Binding(column.Column.ToString())
                            {
                                //StringFormat = "{0:N0}"
                            });

                            //// Bind Foreground met Converter (bind het hele object)
                            //textBlock.Bind(TextBlock.ForegroundProperty, new Binding(".")
                            //{
                            //    Converter = converter,
                            //});

                            return textBlock;
                        })
                    };
                    _dataGrid.Columns.Add(dgColumn);
                }
            }
        }

        private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
        {
            if (_isSorting)
                return;

            _isSorting = true;

            try
            {
                if (DataContext is SignalGridViewModel vm)
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

        private void UpdateSortIndicators(SignalGridViewModel vm)
        {
            if (_dataGrid == null)
                return;

            var config = vm.GetColumnConfig(ColumnEnum.Id).Visible; // Dummy call om Columns te krijgen

            // We hebben de Columns nodig - voeg publieke property toe aan ViewModel
            // Voor nu: haal sort info op uit de kolom configuratie

            foreach (var column in _dataGrid.Columns)
            {
                var sortMemberPath = column is DataGridBoundColumn bc
                    ? bc.SortMemberPath
                    : (column as DataGridTemplateColumn)?.SortMemberPath;

                if (!string.IsNullOrEmpty(sortMemberPath) &&
                    Enum.TryParse<ColumnEnum>(sortMemberPath, true, out ColumnEnum gridColumn))
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
                var dataGrid = this.FindControl<DataGrid>("SignalsDataGrid");
                if (dataGrid != null)
                {
                    var gridPoint = e.GetPosition(dataGrid);
                    if (gridPoint.Y < HeaderHeight)
                    {
                        if (DataContext is SignalGridViewModel vm)
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
            var dataGrid = this.FindControl<DataGrid>("SignalsDataGrid");
            if (dataGrid != null && dataGrid.SelectedItem != null)
            {
                if (DataContext is SignalGridViewModel vm)
                    vm.LaunchExternal();
            }
        }
    }
}