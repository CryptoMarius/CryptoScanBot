using Avalonia.Controls;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

/// <summary>
/// Single entry point for opening the shared <see cref="ChartWindow"/>. Holds the one reusable
/// window instance and the "already open" handling (update the symbol, restore if minimized, bring
/// to front) so both the scanner's CommandShowChart and the emulator's position grid behave
/// identically — and so that logic lives in exactly one place instead of being re-implemented
/// (and drifting) per caller.
/// </summary>
public static class ChartWindowLauncher
{
    private const string DefaultInterval = "15m";

    // The single reused window. Static so a second "open chart" reuses the same window instead of
    // stacking new ones (matches the original CommandShowChart behaviour).
    private static ChartWindow? _chartWindow;

    /// <param name="windowStart">Optional window start. Pass a position's CreateTime when opening from
    /// the emulator so the chart shows that position's lifetime ± a candle margin (a multi-month run has
    /// tens of thousands of candles); null (the scanner) follows the clock / "now".</param>
    /// <param name="windowEnd">Optional window end (the position's CloseTime). Null for a still-open
    /// position → falls back to windowStart.</param>
    /// <param name="emulatorRunId">Optional emulator run. Set (opening from a run's position grid) →
    /// the chart shows only that run's signals/positions; null (the scanner) → only live ones.</param>
    public static void Show(string symbolBase, string symbolQuote, string? intervalName,
        DateTime? windowStart = null, DateTime? windowEnd = null, int? emulatorRunId = null)
    {
        if (_chartWindow == null || !_chartWindow.IsVisible)
        {
            _chartWindow = new ChartWindow
            {
                CanResize = true,
                Title = "Chart form",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            ApplySymbol(_chartWindow, symbolBase, symbolQuote, intervalName, windowStart, windowEnd, emulatorRunId);
            _chartWindow.Show();
        }
        else
        {
            ApplySymbol(_chartWindow, symbolBase, symbolQuote, intervalName, windowStart, windowEnd, emulatorRunId);

            // Force a refresh even when SelectedBase/Quote/Interval above didn't actually change
            // value (e.g. picking a different position on the same symbol+interval, common when
            // browsing a run's position grid). Those properties only raise PropertyChanged on a
            // real change, so OnSymbolChanged would otherwise never fire and the WindowStart/
            // WindowEnd/WindowEmulatorRunId just set in ApplySymbol would silently never be picked
            // up — leaving the previous position's candles on screen.
            if (_chartWindow.DataContext is ChartWindowViewModel vm)
                vm.RequestRefresh();

            // Restore if minimized, then bring to the front.
            if (_chartWindow.WindowState == WindowState.Minimized)
                _chartWindow.WindowState = WindowState.Normal;
            _chartWindow.Activate();
        }
    }

    private static void ApplySymbol(ChartWindow chartWindow, string symbolBase, string symbolQuote,
        string? intervalName, DateTime? windowStart, DateTime? windowEnd, int? emulatorRunId)
    {
        if (chartWindow.DataContext is ChartWindowViewModel vm)
        {
            // Set the window BEFORE the symbol so the (re)load picks it up straight away.
            vm.WindowStart = windowStart;
            vm.WindowEnd = windowEnd;
            vm.WindowEmulatorRunId = emulatorRunId;
            vm.HideAnnototionCursor();
            vm.SymbolSelector.SelectedBase = symbolBase;
            vm.SymbolSelector.SelectedQuote = symbolQuote;
            vm.SymbolSelector.SelectedInterval = string.IsNullOrEmpty(intervalName) ? DefaultInterval : intervalName;
        }
    }
}
