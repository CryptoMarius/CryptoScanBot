using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;

using CryptoScanner.Commands;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

using System.ComponentModel;
using System.Globalization;

namespace CryptoScanner.Model;

public abstract class UserControlWithListBox<TItem, TColumnEnum, TViewModel, TComparer> : UserControl
    where TItem : class
    where TColumnEnum : struct, Enum
    where TViewModel : BaseGridViewModel<TItem, TColumnEnum, TComparer>
    where TComparer : IGridComparer<TItem, TColumnEnum>, new()
{

    internal enum TargetMenu
    {
        Log,
        Symbol,
        Position,
        Signal,
        LiveData,
    };

    internal TargetMenu _targetMenu;
    internal string _gridName = string.Empty;

    protected TViewModel _viewModel = null!;
    internal ListBox _listBox { get; set; } = null!;
    internal ScrollViewer _headerScroller = null!;
    internal ScrollViewer _dataScroller = null!;

    protected ApplicationStateService _applicationStateService { get; set; } = null!;

    internal void ListBox_Loaded(object? sender, RoutedEventArgs e)
    {
        // Only once
        Loaded -= ListBox_Loaded;

        _applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        if (DataContext is TViewModel vm)
            _viewModel = vm;
        else
            throw new Exception($"DataContext is not a {_gridName}");

        //_listBox.Loaded -= ListBox_Loaded;
        System.Diagnostics.Debug.WriteLine($"ListBox_Loaded {_gridName}");

        _listBox = this.FindControl<ListBox>("DataListBox")
            ?? throw new InvalidOperationException("DataListBox not found");

        _headerScroller = this.FindControl<ScrollViewer>("HeaderScroller")
            ?? throw new Exception($"HeaderScroller not found in {_gridName}");
        // right click on header
        _headerScroller.AddHandler(PointerPressedEvent, OnHeaderPointerPressed, RoutingStrategies.Tunnel);

        // right click on rows
        _listBox.AddHandler(PointerPressedEvent, OnListBoxPointerPressed, RoutingStrategies.Tunnel);

        _dataScroller = _listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()
            ?? throw new Exception($"DataListBox ScrollViewer not found in {_gridName}");
        _dataScroller.ScrollChanged += OnDataScrollChanged;

        BuildAxamlColumnsAtRuntime();

        RestoreColumnWidths();
        UpdateSortIndicators(vm.SortColumn, vm.SortDirection);
    }

    internal void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            // Header click
            ShowHeaderContextMenu(_listBox);
            e.Handled = true;
        }
    }

    internal void OnListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            // Row click
            ShowRowContextMenu(_listBox);
            e.Handled = true;
        }
    }

    internal void AddStandardGridHeaderCommands(MenuFlyout flyout)
    {
        var adjustColumnsItem = new MenuItem { Header = "Adjust Columns..." };
        adjustColumnsItem.Click += (s, args) => ShowColumnVisibilityWindow(_listBox);
        flyout.Items.Add(adjustColumnsItem);
    }

    internal async void ShowColumnVisibilityWindow(ListBox listBox)
    {
        if (this.GetVisualRoot() is not Window parentWindow)
            return;

        // todo, needs some extra work because of the listBox -> listbox migration
        // TODO: We can use the Columns and the ActualWidth property!
        //var columnVisibilityWindow = new ColumnWindow
        //{
        //    Title = "Select Visible Columns",
        //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //    DataContext = new ColumnWindowViewModel(listBox.Columns)
        //};

        //await columnVisibilityWindow.ShowDialog(parentWindow);

        //// Save settings after user closes the dialog
        //SaveGridState();
    }


    internal virtual void ShowHeaderContextMenu(ListBox listBox)
    {
        var flyout = new MenuFlyout();
        AddStandardGridHeaderCommands(flyout);
        flyout.ShowAt(listBox, true);
    }

    internal virtual void ShowRowContextMenu(ListBox listBox)
    {
        var flyout = new MenuFlyout();
        AddStandardGridRowCommands(flyout);
        flyout.ShowAt(listBox, true);
    }

    internal void AddStandardGridRowCommands(MenuFlyout flyout)
    {
        var parentWindow = this.FindAncestorOfType<Window>();
        var parameter = (_listBox, _listBox.SelectedItem, _viewModel, parentWindow);

        if (_targetMenu == TargetMenu.Log)
        {
            // Clear..?
            return;
        }

        flyout.Items.Add(new MenuItem { Header = "Open symbol Chart", Command = new CommandShowGraph(), CommandParameter = parameter });
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

        flyout.Items.Add(new MenuItem { Header = "-" });
        flyout.Items.Add(new MenuItem { Header = "Hide grid selection", Command = new CommandDatagridHideSelection(), CommandParameter = parameter });
    }

    internal void InitializeListBox()
    {
        // Runtime - get service from App
        _applicationStateService = GlobalData.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        _listBox.Loaded += ListBox_Loaded;
    }


    private void OnDataScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_headerScroller != null && _dataScroller != null)
        {
            _headerScroller.Offset = new Avalonia.Vector(_dataScroller.Offset.X, 0);
        }
    }

    internal void SaveGridState()
    {
        // todo, needs some extra work because of the listBox -> listbox migration
        //_applicationStateService.SaveGridState(_gridName, _dataGrid, _currentSortColumn, _currentSortDirection);
    }

    internal void RestoreColumnWidths()
    {
        var saved = _applicationStateService.GetColumnWidths(_gridName);
        if (!string.IsNullOrEmpty(saved))
        {
            _viewModel.UpdateColumnWidths(saved);
        }
    }

    internal void RestoreGridState()
    {
        // todo, needs some extra work because of the listBox -> listbox migration
        // Access the service via App.ApplicationStateService
        //_applicationStateService.RestoreGridState(_gridName, _dataGrid, out _currentSortColumn, out _currentSortDirection);
    }

    internal void UpdateSortIndicators(TColumnEnum sortedColumn, ListSortDirection direction)
    {
        // Find header grid
        var headerScroller = this.FindControl<ScrollViewer>("HeaderScroller");
        if (headerScroller?.Content is Grid headerGrid)
        {
            foreach (var child in headerGrid.Children)
            {
                if (child is Button button && button.Tag != null)
                {
                    if (button.Tag is not GridColumnDefinition<TColumnEnum> column)
                        continue;

                    // Update button content with sort indicator
                    var isSorted = column.ColumnEnum.Equals(sortedColumn);
                    if (isSorted)
                    {
                        var arrow = direction == ListSortDirection.Ascending ? " ▲" : " ▼";
                        var originalText = button.Content?.ToString()?.Replace(" ▲", "").Replace(" ▼", "") ?? "";
                        button.Content = originalText + arrow;
                    }
                    else
                    {
                        var originalText = button.Content?.ToString()?.Replace(" ▲", "").Replace(" ▼", "") ?? "";
                        button.Content = originalText;
                    }
                }
            }
        }
    }

    private static int _gridsBuildCount = 0;

    internal void BuildAxamlColumnsAtRuntime()
    {
        // Headers
        var headerGrid = this.FindControl<Grid>("HeaderGrid");
        if (headerGrid != null && _viewModel != null)
        {
            // Clear existing definitions
            headerGrid.Children.Clear();
            headerGrid.ColumnDefinitions.Clear();

            int colIndex = 0;
            foreach (var column in _viewModel.Columns.OrderBy(c => c.DisplayIndex))
            {
                //if (!column.IsVisible)
                //    continue;

                // Bind the columdef to the actualwidth (and do the same for the cell)
                var columnDefinition = new ColumnDefinition();
                var binding = new Binding { Source = column, Path = "ActualWidth", Mode = BindingMode.TwoWay, };
                columnDefinition.Bind(ColumnDefinition.WidthProperty, binding);
                headerGrid.ColumnDefinitions.Add(columnDefinition);

                // Header button
                //< Button Grid.Column = "0" Tag = "Date" Content = "Date" Classes = "SortableHeader" Click = "OnHeaderClick" />
                var button = new Button { Content = column.Header, Classes = { "SortableHeader" }, Tag = column, };
                button.Click += OnHeaderClick;
                Grid.SetColumn(button, colIndex++);
                headerGrid.Children.Add(button);

                // Splitter (except the last)
                if (column != _viewModel.Columns.Last())
                {
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition(column.IsVisible ? new GridLength(1) : new GridLength(0)));

                    var splitter = new GridSplitter { Width = 1, ResizeDirection = GridResizeDirection.Columns, };
                    splitter.DragCompleted += OnSplitterDragCompleted;
                    Grid.SetColumn(splitter, colIndex++);
                    headerGrid.Children.Add(splitter);
                }
            }
        }

        // back to axaml because code below vialates virtualisation and causes performance issues, we only 
        // need to do this once for the header, the rows will be done via datatemplate and virtualization
        if (_targetMenu == TargetMenu.Signal)
            return;


        // DataTemplate
        var listBox = this.FindControl<ListBox>("DataListBox");
        if (listBox != null && _viewModel != null)
        {
            var template = new FuncDataTemplate<TItem>((item, scope) =>
            {
                // Count how many times this gets called
                int count = Interlocked.Increment(ref _gridsBuildCount);
                System.Diagnostics.Debug.WriteLine($"{_gridName}: Grid build count = {count}");


                var grid = new Grid { Height = 22, Background = Brushes.Transparent };

                int colIndex = 0;
                foreach (var column in _viewModel.Columns.OrderBy(c => c.DisplayIndex))
                {
                    if (!column.IsVisible)
                        continue;

                    // Bind the columdef to the actualwidth (like we also did for the header)
                    var columnDefinition = new ColumnDefinition();
                    var binding = new Binding { Source = column, Path = "ActualWidth", Mode = BindingMode.TwoWay, };
                    columnDefinition.Bind(ColumnDefinition.WidthProperty, binding);
                    grid.ColumnDefinitions.Add(columnDefinition);

                    var border = new Border();
                    var textBlock = new TextBlock
                    {
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = column.Alignment,
                        IsVisible = column.IsVisible,
                        Padding = new Thickness(5, 0, 0, 0),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                    };

                    // Always: bind Line
                    if (typeof(TItem).GetProperty($"{column.ColumnEnum}Text") != null)
                        textBlock.Bind(TextBlock.TextProperty, new Binding($"{column.ColumnEnum}Text") { Mode = BindingMode.OneWay });
                    else
                        System.Diagnostics.Debug.WriteLine($"{_gridName} {column.ColumnEnum}Text does not exist"); // Debug

                    // Optional: bind Foreground
                    if (typeof(TItem).GetProperty($"{column.ColumnEnum}Foreground") != null)
                        textBlock.Bind(TextBlock.ForegroundProperty, new Binding($"{column.ColumnEnum}Foreground") { Mode = BindingMode.OneWay });

                    // Optional: bind Background
                    if (typeof(TItem).GetProperty($"{column.ColumnEnum}Background") != null)
                        border.Bind(Border.BackgroundProperty, new Binding($"{column.ColumnEnum}Background") { Mode = BindingMode.OneWay });

                    border.Child = textBlock;
                    Grid.SetColumn(border, colIndex++);
                    grid.Children.Add(border);

                    // Add splitter column (just like the header)
                    if (column != _viewModel.Columns.Last())
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition(column.IsVisible ? new GridLength(1) : new GridLength(0)));
                        colIndex++; // Skip splitter column
                    }
                }

                return grid;
            });

            listBox.ItemTemplate = template;
        }
    }

    public void OnSplitterDragCompleted(object? sender, VectorEventArgs e)
    {
        // TODO: Old bullshit code below, we only need to save the columns and specially the Actualwidth!!!

        //if (sender is not GridSplitter splitter)
        //    return;

        //var headerGrid = this.FindControl<Grid>("HeaderGrid");
        //if (headerGrid == null)
        //    return;

        //// Find which column the splitter is in
        //var splitterColumn = Grid.GetColumn(splitter);

        //// The column before the splitter is the one that was resized
        //var resizedColumnIndex = splitterColumn - 1;

        //if (resizedColumnIndex < 0 || resizedColumnIndex >= headerGrid.ColumnDefinitions.Count)
        //    return;

        //// Get the new width of the resized column
        //var newWidth = headerGrid.ColumnDefinitions[resizedColumnIndex].ActualWidth;

        //// Build the complete width string from all columns
        //var widthParts = new List<string>();
        //for (int i = 0; i < headerGrid.ColumnDefinitions.Count; i++)
        //{
        //    var colDef = headerGrid.ColumnDefinitions[i];
        //    if (colDef.Width.IsStar)
        //        widthParts.Add("*");
        //    else
        //        widthParts.Add(colDef.ActualWidth.ToString("F0"));
        //}

        //var widthString = string.Join(",", widthParts);

        //_viewModel.UpdateColumnWidths(widthString);
        //_applicationStateService.SaveColumnWidths(_gridName, widthString);
    }

    public void OnHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not GridColumnDefinition<TColumnEnum> column)
            return;

        System.Diagnostics.Debug.WriteLine($"Header clicked: Tag={column.Header}");

        _viewModel.SortByColumn(column.ColumnEnum);
        UpdateSortIndicators(column.ColumnEnum, _viewModel.SortDirection);
    }
}

