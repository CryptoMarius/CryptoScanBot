using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.ViewModels;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CryptoScanner.Emulator.Views;

public partial class RunResultsView : UserControl
{
    public RunResultsView()
    {
        // TEMP diagnostic: time the actual render of the Results tab.
        // IMPORTANT: this view is constructed once at startup (the TabControl builds it eagerly), NOT
        // when the tab is selected. So a stopwatch started in the constructor would measure the time
        // until the user happens to click the tab — pure idle time, which is exactly what made an
        // earlier log read "17.81s" while the data itself loads in ~8 ms. Instead we start a fresh
        // stopwatch on AttachedToVisualTree (fires when the tab is actually shown) and post at
        // ContextIdle (runs only after the UI thread finishes all layout + render and goes idle); the
        // delta is the true render cost of showing the tab. Remove once confirmed fast.
        AttachedToVisualTree += (_, _) =>
        {
            var renderWatch = Stopwatch.StartNew();
            int rowCount = (DataContext as RunResultsViewModel)?.Runs.Count ?? 0;
            Dispatcher.UIThread.Post(() =>
            {
                GlobalData.AddTextToLogTab(
                    $"Results tab: render settled {renderWatch.Elapsed.TotalSeconds:N2}s after shown " +
                    $"({rowCount} run row(s))");
            }, DispatcherPriority.ContextIdle);
        };

        InitializeComponent();

        // Wire the double-click drill-down. Done in code-behind because the handler needs the
        // owner Window (to root the modal positions dialog) and the selected row — both easier
        // here than via an MVVM binding. The owner is resolved at click-time from the visual
        // tree because this control lives inside MainWindow's TabControl, not its own Window.
        RunsGrid.DoubleTapped += OnRunDoubleTapped;

        // Select the row under the cursor on right-click BEFORE the context menu opens. The
        // DataGrid only updates its selection on a LEFT click, so without this a right-click on a
        // different row would leave SelectedItem pointing at the previously selected run and the
        // Delete action would hit the wrong one. Tunnelling so we see the press before the
        // ContextMenu's own handling consumes it.
        RunsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel);
    }


    private void OnRunDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not RunRow row)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        // Non-modal (Show, not ShowDialog): the chart opened from here renders correctly only when it
        // is NOT spawned from within a modal dialog's nested loop. Non-modal also lets results +
        // positions + chart stay open together.
        new RunPositionsWindow(row).Show(owner);
    }


    private void OnShowSignalsClick(object? sender, RoutedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not RunRow row)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        new RunSignalsWindow(row).Show(owner);
    }


    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(RunsGrid).Properties.IsRightButtonPressed)
            return;

        // Walk up from the clicked element to its DataGridRow so we can select the run the user
        // actually right-clicked. A click on empty space below the rows finds no row and leaves
        // the current selection untouched.
        if (e.Source is Visual source && source.FindAncestorOfType<DataGridRow>() is DataGridRow gridRow
            && gridRow.DataContext is RunRow runRow)
        {
            // Keep an existing MULTI-selection if the right-clicked row is part of it, so the context
            // menu acts on all selected runs. Only collapse to this single row when the user
            // right-clicks a row that isn't currently selected (standard list behaviour).
            if (!RunsGrid.SelectedItems.Contains(runRow))
                RunsGrid.SelectedItem = runRow;
        }
    }


    private async void OnShowJsonClick(object? sender, RoutedEventArgs e)
    {
        // Single-row action: only meaningful for one run's JSON at a time.
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        if (rows.Count != 1)
        {
            viewModel.Status = "Select a single run to view its JSON.";
            return;
        }

        RunRow row = rows[0];
        string? json = viewModel.GetSettingsJsonForDisplay(row.Id);
        if (string.IsNullOrWhiteSpace(json))
        {
            viewModel.Status = $"Run #{row.Id} has no stored settings JSON.";
            return;
        }

        string label = string.IsNullOrWhiteSpace(row.Label) ? "" : $" — {row.Label}";
        var window = new RunJsonWindow($"Run #{row.Id} settings JSON{label}", json);
        await window.ShowDialog(owner);
    }


    private void OnExportSettingsClick(object? sender, RoutedEventArgs e)
    {
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        if (rows.Count == 0)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        // Non-destructive (just writes files), so no confirmation dialog — the ViewModel reports
        // the result via Status.
        viewModel.ExportSettings(rows);
    }


    private void OnRecalculateClick(object? sender, RoutedEventArgs e)
    {
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        if (rows.Count == 0)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        // Non-destructive recompute from existing positions — no confirmation needed.
        viewModel.RecalculateRuns(rows);
    }


    private async void OnDeleteRunClick(object? sender, RoutedEventArgs e)
    {
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        if (rows.Count == 0)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        string message;
        if (rows.Count == 1)
        {
            string label = string.IsNullOrWhiteSpace(rows[0].Label) ? "" : $" \"{rows[0].Label}\"";
            message = $"Delete run #{rows[0].Id}{label} and all of its signals and positions?\n\nThis cannot be undone.";
        }
        else
        {
            message = $"Delete {rows.Count} runs and all of their signals and positions?\n\nThis cannot be undone.";
        }

        bool confirmed = await ConfirmAsync(owner, "Delete runs", message);
        if (!confirmed)
            return;

        viewModel.DeleteRuns(rows);
    }


    /// <summary>
    /// Minimal modal yes/no confirmation built in code so the emulator needs no extra message-box
    /// dependency. Returns true only when the user explicitly confirms; closing the window or
    /// pressing Escape (the Cancel button) returns false.
    /// </summary>
    private static async Task<bool> ConfirmAsync(Window owner, string title, string message)
    {
        bool result = false;

        var deleteButton = new Button { Content = "Delete", MinWidth = 80 };
        var cancelButton = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true };

        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 14,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        MaxWidth = 380,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { deleteButton, cancelButton },
                    },
                },
            },
        };

        deleteButton.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        cancelButton.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        await dialog.ShowDialog(owner);
        return result;
    }
}
