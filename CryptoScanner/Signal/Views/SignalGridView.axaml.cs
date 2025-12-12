using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using CryptoScanner.Signal.ViewModels;

namespace CryptoScanner.Signal.Views;

public partial class SignalGridView : UserControl
{
    private const double HeaderHeight = 30.0;

    public SignalGridView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Handle pointer pressed events on the DataGrid
    /// Right-click on header shows column visibility window
    /// Right-click on row shows context menu
    /// </summary>
    private void OnDataGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            var dataGrid = this.FindControl<DataGrid>("SignalDataGrid");
            if (dataGrid != null)
            {
                var gridPoint = e.GetPosition(dataGrid);

                // Check if click is in header area (Y < HeaderHeight)
                if (gridPoint.Y < HeaderHeight)
                {
                    // Header click
                    ShowHeaderFlyout(dataGrid, e);
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
    /// Show the signal column visibility window as a modal dialog centered on the parent window
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
    }

    /// <summary>
    /// Handle launch external program from context menu
    /// </summary>
    private void OnLaunchExternal(object? sender, RoutedEventArgs e)
    {
        var dataGrid = this.FindControl<DataGrid>("SignalDataGrid");
        if (dataGrid != null && dataGrid.SelectedItem != null)
        {
            if (DataContext is SignalGridViewModel vm)
            {
                vm.OpenExternalProgramCommand.Execute(dataGrid.SelectedItem);
            }
        }
    }

    private void ColumnsAdjust(object? sender, RoutedEventArgs e)
    {
        var dataGrid = this.FindControl<DataGrid>("SignalDataGrid");
        if (dataGrid != null)
        {
            //if (DataContext is SignalGridViewModel vm)
            //{
                ShowColumnVisibilityWindow(dataGrid);
            //}
        }
    }
}
