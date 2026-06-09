using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

using CryptoScanner.Emulator.ViewModels;

using System.Collections.Generic;
using System.Linq;

namespace CryptoScanner.Emulator.Views;

public partial class RunResultsView : UserControl
{
    public RunResultsView()
    {
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


    private async void OnRunDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not RunRow row)
            return;
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var positions = new RunPositionsWindow(row);
        await positions.ShowDialog(owner);
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
