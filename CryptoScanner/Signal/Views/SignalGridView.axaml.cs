using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using CryptoScanner.Services;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.ViewModels;
using CryptoScanner.Views;

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

        _dataGrid.AddHandler(PointerPressedEvent, OnDataGridPointerPressed, RoutingStrategies.Tunnel);
    
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
                ShowHeaderContextMenu(_dataGrid!, e);
                e.Handled = true;
            }
            else
            {
                // Row click
                ShowRowContextMenu(_dataGrid!, e);
                e.Handled = true;
            }
        }
    }


    /// <summary>
    /// Show context menu for header (column management)
    /// </summary>
    private void ShowHeaderContextMenu(DataGrid dataGrid, PointerPressedEventArgs e)
    {
        var flyout = new MenuFlyout();

        var adjustColumnsItem = new MenuItem { Header = "Adjust Columns..." };
        adjustColumnsItem.Click += (s, args) => ShowColumnVisibilityWindow(dataGrid);
        flyout.Items.Add(adjustColumnsItem);

        flyout.Items.Add(new Separator());

        var resetColumnsItem = new MenuItem { Header = "Reset Columns" };
        resetColumnsItem.Click += (s, args) => ResetColumns(dataGrid);
        flyout.Items.Add(resetColumnsItem);

        flyout.ShowAt(dataGrid, true);
    }


    /// <summary>
    /// Show context menu for rows (signal actions)
    /// </summary>
    private void ShowRowContextMenu(DataGrid dataGrid, PointerPressedEventArgs e)
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
    private void ResetColumns(DataGrid dataGrid)
    {
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
