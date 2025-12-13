using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CryptoScanner.Services;
using CryptoScanner.Signal.ViewModels;

namespace CryptoScanner.Signal.Views;

public partial class SignalGridView : UserControl
{
    private const double HeaderHeight = 30.0;
    private const string SettingsFileName = "signal-grid-columns.json";

    private readonly DataGrid? _dataGrid;
    private readonly IPlatformService? _platformService;
    private readonly IDataGridColumnsService? _datagridService;


    public SignalGridView()
    {
        InitializeComponent();

        // Get services from DI container
        _platformService = App.GetService<IPlatformService>()
            ?? throw new InvalidOperationException("IPlatformService not registered");
        _datagridService = App.GetService<IDataGridColumnsService>()
            ?? throw new InvalidOperationException("IDataGridColumnsService not registered");
        _dataGrid = this.FindControl<DataGrid>("SignalDataGrid")
            ?? throw new InvalidOperationException("SignalDataGrid not found");

        LoadColumnSettings();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Get the full path to the settings file
    /// </summary>
    private string GetSettingsFileName()
    {
        string dataDir = _platformService!.GetDataDirectory();

        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        return Path.Combine(dataDir, SettingsFileName);
    }

    /// <summary>
    /// Load saved column settings (width, order, visibility) from JSON file
    /// </summary>
    private void LoadColumnSettings()
    {
        var settingsFileName = GetSettingsFileName();
        _datagridService!.LoadColumnSettings(_dataGrid!, settingsFileName);
    }

    /// <summary>
    /// Save column settings to JSON file
    /// </summary>
    private void SaveColumnSettings()
    {
        var settingsFileName = GetSettingsFileName();
        _datagridService!.SaveColumnSettings(_dataGrid!, settingsFileName);
    }

    /// <summary>
    /// Handle column reordering
    /// </summary>
    private void OnColumnReordered(object? sender, DataGridColumnEventArgs e)
    {
        SaveColumnSettings();
    }

    /// <summary>
    /// Handle column display index changes
    /// </summary>
    private void OnColumnDisplayIndexChanged(object? sender, DataGridColumnEventArgs e)
    {
        SaveColumnSettings();
    }

    /// <summary>
    /// Handle pointer pressed events on the DataGrid
    /// Right-click on header shows column visibility window
    /// </summary>
    private void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            var gridPoint = e.GetPosition(_dataGrid!);

            // Check if click is in header area (Y < HeaderHeight)
            if (gridPoint.Y < HeaderHeight)
            {
                // Header click
                //ShowHeaderFlyout(dataGrid, e);
                // Header click - show column visibility window
                ShowColumnVisibilityWindow(_dataGrid!);
                e.Handled = true;
            }
            //else
            //{
            //    // Row click
            //    ShowGridFlyout(dataGrid, e);
            //    e.Handled = true;
            //}
        }
    }


    private void ShowHeaderFlyout(DataGrid dataGrid, PointerPressedEventArgs e)
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem { Header = "Adjust Columns..." });
        flyout.Items.Add(new MenuItem { Header = "---" });
        flyout.Items.Add(new MenuItem { Header = "Reset Columns" });

        flyout.ShowAt(dataGrid, true);
        //// Header click - show column visibility window
        //if (DataContext is SignalGridViewModel vm)
        //{
        //    ShowColumnVisibilityWindow(dataGrid);
        //    e.Handled = true; // Prevent context menu
        //}
            //< !--Context menu for rows only -->
            //< DataGrid.ContextFlyout >

            //    < MenuFlyout >
            //        < MenuItem Header = "Adjust columns" Click = "ColumnsAdjust" Margin = "2" />
            //        < MenuItem Header = "-" />
            //        < MenuItem Header = "Open in External Program" Click = "OnLaunchExternal" />
            //        < MenuItem Header = "-" />
            //    </ MenuFlyout >
            //</ DataGrid.ContextFlyout >

    }

    private void ShowGridFlyout(DataGrid dataGrid, PointerPressedEventArgs e)
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem { Header = "Copy name..." });
        flyout.Items.Add(new MenuItem { Header = "---" });
        flyout.Items.Add(new MenuItem { Header = "BlaBla" });

        flyout.ShowAt(dataGrid, true);
    }
    /// <summary>
    /// Show the signal column visibility window as a modal dialog
    /// </summary>
    private async void ShowColumnVisibilityWindow(DataGrid dataGrid)
    {
        if (this.GetVisualRoot() is not Window parentWindow)
            return;

        var columnVisibilityWindow = new SignalColumnVisibilityWindow
        {
            Title = "Select Visible Columns",
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Width = 300,
            Height = 500,
            DataContext = new SignalColumnVisibilityViewModel(dataGrid.Columns)
        };

        await columnVisibilityWindow.ShowDialog(parentWindow);

        // Save settings after user closes the dialog
        SaveColumnSettings();
    }

    /// <summary>
    /// Handle launch external program from context menu
    /// </summary>
    private void OnLaunchExternal(object? sender, RoutedEventArgs e)
    {
        if (_dataGrid!.SelectedItem != null)
        {
            if (DataContext is SignalGridViewModel vm)
            {
                vm.OpenExternalProgramCommand.Execute(_dataGrid.SelectedItem);
            }
        }
    }

    private void ColumnsAdjust(object? sender, RoutedEventArgs e)
    {
        ShowColumnVisibilityWindow(_dataGrid);
    }
}
