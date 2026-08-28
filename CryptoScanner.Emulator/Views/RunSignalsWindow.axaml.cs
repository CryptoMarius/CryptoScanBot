using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.ViewModels;
using CryptoScanner.Views;

namespace CryptoScanner.Emulator.Views;

public partial class RunSignalsWindow : Window
{
    // The single reused signals window. Static so opening signals for another run reuses this window
    // (its content is swapped) instead of stacking new windows, which got confusing over time.
    // Mirrors ChartWindowLauncher's single-window handling.
    private static RunSignalsWindow? _instance;

    public RunSignalsWindow() : this(new RunRow())
    {
        // Designer-only path: empty constructor for the XAML preview.
    }

    public RunSignalsWindow(RunRow run)
    {
        InitializeComponent();
        DataContext = new RunSignalsViewModel(run);

        // Select the row under the cursor on right-click BEFORE the context menu opens, so
        // "Open chart" always acts on the row the user actually clicked.
        SignalsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel);

        // Double-click a signal row to open its chart (same action as the context menu).
        SignalsGrid.DoubleTapped += OnSignalDoubleTapped;
    }

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(SignalsGrid).Properties.IsRightButtonPressed)
            return;

        if (e.Source is Visual source && source.FindAncestorOfType<DataGridRow>() is DataGridRow gridRow
            && gridRow.DataContext is SignalRow row)
        {
            SignalsGrid.SelectedItem = row;
        }
    }

    private void OnSignalDoubleTapped(object? sender, TappedEventArgs e) => OpenChartForSelectedRow();

    private void OnOpenChartClick(object? sender, RoutedEventArgs e) => OpenChartForSelectedRow();

    private void OpenChartForSelectedRow()
    {
        if (SignalsGrid.SelectedItem is not SignalRow row || string.IsNullOrEmpty(row.Symbol))
            return;
        if (DataContext is not RunSignalsViewModel viewModel)
            return;

        // Resolve the symbol (base/quote) from the run's exchange, loaded in memory. Rows written
        // by a pre-migration run hold the bare pair, so resolve via the pair lookup.
        var exchange = GlobalData.ActiveExchange;
        if (exchange == null || !exchange.TryGetSymbolByPair(row.Symbol, out var symbol))
        {
            viewModel.Status = $"Symbol {row.Symbol} not found in the active exchange.";
            return;
        }

        try
        {
            // Shared launcher: reuses one window. Centre the chart on the signal's candle (no close time,
            // so a window around the signal ± a candle margin). Pass the run id so the chart only draws
            // THIS run's signals/positions.
            ChartWindowLauncher.Show(symbol.Base, symbol.Quote, row.Interval, row.SignalTime, null, viewModel.RunId);
        }
        catch (Exception ex)
        {
            viewModel.Status = $"Failed to open chart: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens the signals window for the given run, reusing the one existing instance. When a window is
    /// already open its content is replaced with the new run and it is brought to the front, so only a
    /// single signals window is ever shown from the emulator. A closed window (IsVisible == false)
    /// causes a fresh one to be created on the next call.
    /// </summary>
    public static void ShowSingle(RunRow run, Window owner)
    {
        if (_instance == null || !_instance.IsVisible)
        {
            _instance = new RunSignalsWindow(run);
            _instance.Show(owner);
        }
        else
        {
            _instance.DataContext = new RunSignalsViewModel(run);
            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }
}
