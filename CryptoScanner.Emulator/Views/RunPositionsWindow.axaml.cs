using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.ViewModels;
using CryptoScanner.Views;

namespace CryptoScanner.Emulator.Views;

public partial class RunPositionsWindow : Window
{
    public RunPositionsWindow() : this(new RunRow())
    {
        // Designer-only path: empty constructor for the XAML preview.
    }

    public RunPositionsWindow(RunRow run)
    {
        InitializeComponent();
        DataContext = new RunPositionsViewModel(run);

        // Select the row under the cursor on right-click BEFORE the context menu opens, so
        // "Open Symbol Chart" always acts on the row the user actually clicked.
        PositionsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel);

        // Double-click a position row to open its chart (same action as the context menu) — quicker
        // than right-click → menu.
        PositionsGrid.DoubleTapped += OnPositionDoubleTapped;
    }

    private void OnPositionDoubleTapped(object? sender, TappedEventArgs e) => OpenChartForSelectedRow();

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(PositionsGrid).Properties.IsRightButtonPressed)
            return;

        if (e.Source is Visual source && source.FindAncestorOfType<DataGridRow>() is DataGridRow gridRow
            && gridRow.DataContext is PositionRow row)
        {
            PositionsGrid.SelectedItem = row;
        }
    }

    private void OnOpenChartClick(object? sender, RoutedEventArgs e) => OpenChartForSelectedRow();

    private void OpenChartForSelectedRow()
    {
        if (PositionsGrid.SelectedItem is not PositionRow row || string.IsNullOrEmpty(row.Symbol))
            return;
        if (DataContext is not RunPositionsViewModel viewModel)
            return;

        // Resolve the symbol (base/quote) from the run's exchange, loaded in memory.
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || !exchange.SymbolListName.TryGetValue(row.Symbol, out var symbol))
        {
            viewModel.Status = $"Symbol {row.Symbol} not found in the active exchange.";
            return;
        }

        try
        {
            // Shared launcher: reuses one window, restores/activates if already open. Show the position's
            // lifetime (CreateTime..CloseTime) ± a candle margin, so the chart opens a bounded window
            // around the trade instead of the whole multi-month run (tens of thousands of candles).
            // Pass the run id so the chart only draws THIS run's signals/positions (not every run's).
            ChartWindowLauncher.Show(symbol.Base, symbol.Quote, row.Interval, row.CreateTime, row.CloseTime, viewModel.RunId);
        }
        catch (Exception ex)
        {
            viewModel.Status = $"Failed to open chart: {ex.Message}";
        }
    }
}
