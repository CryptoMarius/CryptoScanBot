using Avalonia.Controls;
using Avalonia.Interactivity;

using CryptoScanner.Model;
using CryptoScanner.Services;
using CryptoScanner.Signal.Common;
using CryptoScanner.Signal.Model;
using CryptoScanner.Signal.ViewModels;

namespace CryptoScanner.Signal.Views;

public partial class SignalGridView : UserControlWithGrid<SignalInfo>
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
        _applicationStateService = App.GetService<ApplicationStateService>()
            ?? throw new InvalidOperationException("ApplicationStateService not registered");

        _dataGrid = this.FindControl<DataGrid>("SignalDataGrid")
            ?? throw new InvalidOperationException("SignalDataGrid not found");

        DataContextChanged += OnDataContextChanged;
        _dataGrid.Loaded += DataGrid_Loaded; // Sorting stuff

        // Kind of Hacky, needs work...
        Loaded += (s, e) =>
        {
            if (DataContext is SignalGridViewModel vm)
            {
                var owner = TopLevel.GetTopLevel(this) as Window
                    ?? throw new InvalidOperationException("No parent window available");
                vm.SetOwner(owner);
                //vm.RequestSort += OnRequestSort;
                //vm.RequestSortedInsert += OnRequestSortedInsert;
            }
        };

        //// Kind of Hacky, needs work... (is it really needed?)
        //Unloaded += (s, e) =>
        //{
        //    if (DataContext is SignalGridViewModel vm)
        //    {
        //        vm.RequestSort -= OnRequestSort;
        //        vm.RequestSortedInsert -= OnRequestSortedInsert;
        //    }
        //};

        // Register a custom comparer for each column based on its SortMemberPath
        foreach (var column in _dataGrid.Columns)
        {
            if (Enum.TryParse<SignalColumnEnum>(column.SortMemberPath, out SignalColumnEnum a))
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


    /// <summary>
    /// Show context menu for header (column management)
    /// </summary>
    internal override void ShowHeaderContextMenu(DataGrid dataGrid)
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
    internal override void ShowRowContextMenu(DataGrid dataGrid)
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


}