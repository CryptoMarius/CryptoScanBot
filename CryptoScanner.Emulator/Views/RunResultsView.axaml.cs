using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CryptoScanner.Config.Views;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.Services;
using CryptoScanner.Emulator.ViewModels;

using System.ComponentModel;
using System.Diagnostics;

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

        // Which columns are shown, how wide and in what order, as the user left them last time. Read
        // from CryptoScanBot-user.json in the data folder, the same way the scanner does for its
        // grids. Saved again when the column dialog closes and when the window closes.
        RestoreGridLayout();

        // Wire the double-click drill-down. Done in code-behind because the handler needs the
        // owner Window (to root the modal positions dialog) and the selected row — both easier
        // here than via an MVVM binding. The owner is resolved at click-time from the visual
        // tree because this control lives inside MainWindow's TabControl, not its own Window.
        RunsGrid.DoubleTapped += OnRunDoubleTapped;

        // Capture the user's sort choice and persist it in the emulator config file.
        RunsGrid.Sorting += OnGridSorting;

        // Restore the column sort indicator from the saved preference once the grid is shown.
        AttachedToVisualTree += (_, _) => RestoreSortIndicator();

        // There is no event for a changed column width or a dragged column, so - like the scanner -
        // the layout is written once more when the window goes.
        AttachedToVisualTree += (_, _) =>
        {
            if (_closingHooked || TopLevel.GetTopLevel(this) is not Window owner)
                return;
            _closingHooked = true;
            owner.Closing += (_, _) => SaveGridLayout();
        };

        // Select the row under the cursor on right-click BEFORE the context menu opens. The
        // DataGrid only updates its selection on a LEFT click, so without this a right-click on a
        // different row would leave SelectedItem pointing at the previously selected run and the
        // Delete action would hit the wrong one. Tunnelling so we see the press before the
        // ContextMenu's own handling consumes it.
        RunsGrid.AddHandler(PointerPressedEvent, OnGridPointerPressed, RoutingStrategies.Tunnel);
    }


    /// <summary>
    /// Captures the column + direction the user just sorted by and persists it in the emulator
    /// config file so the next Refresh (and next app launch) restores the same order.
    /// Uses <see cref="DataGridColumn.SortMemberPath"/> as the stable key (Header text is
    /// localisation-fragile; SortMemberPath maps 1:1 to the RunRow property).
    /// </summary>
    private void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        string? sortPath = e.Column.SortMemberPath;
        if (string.IsNullOrEmpty(sortPath))
            return;

        var direction = (_currentSortColumn == sortPath && _currentSortDirection == ListSortDirection.Ascending)
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        _currentSortColumn = sortPath;
        _currentSortDirection = direction;

        try
        {
            EmulatorRunConfig config = RunConfigFile.Load();
            config.SortColumn = sortPath;
            config.SortDescending = direction == ListSortDirection.Descending;
            RunConfigFile.Save(config);
        }
        catch
        {
            // Non-fatal: losing the sort preference is cosmetic.
        }
    }

    private string? _currentSortColumn;
    private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;

    /// <summary>The key the runs grid is stored under in CryptoScanBot-user.json.</summary>
    private const string GridName = "EmulatorRuns";
    private bool _closingHooked;


    private static ApplicationStateService? StateService => GlobalData.GetService<ApplicationStateService>();


    private void RestoreGridLayout()
    {
        try
        {
            StateService?.RestoreGridLayout(GridName, RunsGrid);
        }
        catch (Exception ex)
        {
            // Non-fatal: the grid then shows its default layout.
            GlobalData.AddTextToLogTab($"Results grid: restoring the column layout failed — {ex.Message}");
        }
    }


    private void SaveGridLayout()
    {
        try
        {
            StateService?.SaveGridLayout(GridName, RunsGrid);
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"Results grid: saving the column layout failed — {ex.Message}");
        }
    }


    /// <summary>
    /// The column dialog the scanner has as well: a check box per column. The extra columns on the
    /// right (peak capital, return on the start capital, best and worst case) can be brought in
    /// and the ones not needed put away, which is also the way to make the grid fit the window.
    /// </summary>
    private async void OnAdjustColumnsClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var window = new ColumnWindow
        {
            DataContext = new ColumnWindowViewModel(RunsGrid.Columns),
        };
        await window.ShowDialog(owner);

        SaveGridLayout();
    }


    /// <summary>
    /// Programmatically sorts the grid by the persisted column + direction. Uses
    /// <see cref="DataGridColumn.Sort"/> — the same mechanism the scanner's grids use.
    /// Called once when the grid is first shown; subsequent user clicks are handled by the
    /// DataGrid itself (and captured by <see cref="OnGridSorting"/>).
    /// </summary>
    private void RestoreSortIndicator()
    {
        try
        {
            EmulatorRunConfig config = RunConfigFile.Load();
            if (string.IsNullOrEmpty(config.SortColumn))
                return;

            _currentSortColumn = config.SortColumn;
            _currentSortDirection = config.SortDescending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            var column = RunsGrid.Columns.FirstOrDefault(c => c.SortMemberPath == config.SortColumn);
            column?.Sort(_currentSortDirection);
        }
        catch
        {
            // Non-fatal.
        }
    }


    private void OnRunDoubleTapped(object? sender, TappedEventArgs e) => ShowPositionsForSelectedRun();


    private void OnShowPositionsClick(object? sender, RoutedEventArgs e) => ShowPositionsForSelectedRun();


    /// <summary>
    /// Opens the positions window for the currently selected run. Shared by the double-click and by
    /// the "Show positions…" context-menu entry so both do exactly the same thing.
    /// </summary>
    private void ShowPositionsForSelectedRun()
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

        // Reuse the single signals window instead of opening a new one each time.
        RunSignalsWindow.ShowSingle(row, owner);
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


    private async void OnEditLabelClick(object? sender, RoutedEventArgs e)
    {
        // Single-row action: a label belongs to one run.
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        if (rows.Count != 1)
        {
            viewModel.Status = "Select a single run to edit its label.";
            return;
        }

        RunRow row = rows[0];
        string? newLabel = await PromptTextAsync(owner, $"Edit label — run #{row.Id}", "Label (remark):", row.Label);
        if (newLabel is null)   // cancelled / closed
            return;

        viewModel.UpdateLabel(row.Id, newLabel.Trim());
    }


    private async void OnShowSettingsGuiClick(object? sender, RoutedEventArgs e)
    {
        // Single-run action: show one run's settings.
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        if (rows.Count != 1)
        {
            viewModel.Status = "Select a single run to view its settings.";
            return;
        }

        RunRow row = rows[0];
        SettingsBasic? runSettings = RunResultsViewModel.GetRunSettings(row.Id);
        if (runSettings == null)
        {
            viewModel.Status = $"Run #{row.Id} has no stored settings.";
            return;
        }

        // The dialog reads the run's settings directly — including the plugin tabs, which take their
        // values from its AnalyzerSettings blocks. GlobalData.Settings and the live plugin settings
        // stay untouched, so this also works while a replay is running.
        var window = new ConfigurationWindow(runSettings, readOnly: true)
        {
            Title = $"Settings of run #{row.Id} (view only — changes are NOT saved)",
        };
        await window.ShowDialog<bool>(owner);
        viewModel.Status = $"Viewed settings of run #{row.Id} (no changes saved).";
    }


    private async void OnShowEmulatorConfigClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        string path = RunConfigFile.FilePath;
        if (!System.IO.File.Exists(path))
        {
            if (DataContext is RunResultsViewModel vm)
                vm.Status = $"{RunConfigFile.FileName} not found.";
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        var window = new RunJsonWindow(RunConfigFile.FileName, json);
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


    private void OnCopyRowsClick(object? sender, RoutedEventArgs e)
    {
        List<RunRow> rows = RunsGrid.SelectedItems.OfType<RunRow>().ToList();
        CopyRowsToClipboard(rows);
    }


    private void OnCopyAllRowsClick(object? sender, RoutedEventArgs e)
    {
        List<RunRow> rows = RunsGrid.ItemsSource?.OfType<RunRow>().ToList() ?? [];
        CopyRowsToClipboard(rows);
    }


    private void CopyRowsToClipboard(List<RunRow> rows)
    {
        if (DataContext is not RunResultsViewModel viewModel)
            return;
        if (rows.Count == 0)
        {
            viewModel.Status = "Nothing selected to copy.";
            return;
        }

        var sb = new System.Text.StringBuilder();

        // Header row — same order and names as the grid columns.
        sb.AppendLine("Id\tLabel\tPeriod\tStarted\tFinished\tDuration\tResult\tSignals\tPositions\tOpen\tWon\tLost\tTimeout\tWin%\tProfit\tProfit%\tInvested\t" +
            "Peak cap.\tPeak pos\tPeak %\tStart cap.\tEnd cap.\tReturn %\tLong\tProfit long\tShort\tProfit short\tBest case\tWorst case\tAvg dur.\tMin dur.\tMax dur.");

        foreach (RunRow r in rows)
        {
            sb.Append(r.Id).Append('\t');
            sb.Append(r.Label).Append('\t');
            sb.Append(r.Period).Append('\t');
            sb.Append(r.StartedLocal).Append('\t');
            sb.Append(r.FinishedLocal).Append('\t');
            sb.Append(r.Duration).Append('\t');
            sb.Append(r.Result).Append('\t');
            sb.Append(r.SignalCount).Append('\t');
            sb.Append(r.PositionCount).Append('\t');
            sb.Append(r.PositionsOpen).Append('\t');
            sb.Append(r.PositionsWon).Append('\t');
            sb.Append(r.PositionsLost).Append('\t');
            sb.Append(r.PositionsTimeout).Append('\t');
            sb.Append(r.WinPercentage).Append('\t');
            sb.Append(r.Profit).Append('\t');
            sb.Append(r.ProfitPercentage).Append('\t');
            sb.Append(r.Invested).Append('\t');
            sb.Append(r.PeakInvested).Append('\t');
            sb.Append(r.PeakPositions).Append('\t');
            sb.Append(r.PeakProfitPercentage).Append('\t');
            sb.Append(r.StartCapitalText).Append('\t');
            sb.Append(r.EndCapitalText).Append('\t');
            sb.Append(r.CapitalReturnPercentageText).Append('\t');
            sb.Append(r.PositionsLong).Append('\t');
            sb.Append(r.ProfitLong).Append('\t');
            sb.Append(r.PositionsShort).Append('\t');
            sb.Append(r.ProfitShort).Append('\t');
            sb.Append(r.BestCase).Append('\t');
            sb.Append(r.WorstCase).Append('\t');
            sb.Append(r.AvgDurationText).Append('\t');
            sb.Append(r.MinDurationText).Append('\t');
            sb.AppendLine(r.MaxDurationText);
        }

        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            // Fire-and-forget: clipboard SetTextAsync is async but we don't need to await it
            // in a UI event handler — the data is already built, the call just hands it to the OS.
            _ = clipboard.SetTextAsync(sb.ToString());
            viewModel.Status = $"{rows.Count} row(s) copied to clipboard — paste directly into Excel.";
        }
        else
        {
            viewModel.Status = "Clipboard not available.";
        }
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


    private async void OnDeleteAllRunsClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;
        if (DataContext is not RunResultsViewModel viewModel)
            return;

        int count = viewModel.Runs.Count;
        if (count == 0)
            return;

        bool confirmed = await ConfirmAsync(owner, "Delete all runs",
            $"Delete ALL {count} run(s) and all of their signals and positions?\n\nThis cannot be undone.");
        if (!confirmed)
            return;

        viewModel.DeleteAllRuns();
    }


    /// <summary>
    /// Minimal modal single-line text-input dialog, built in code like <see cref="ConfirmAsync"/> so the
    /// emulator needs no extra dialog dependency. Returns the entered text on OK (Enter), or null when
    /// cancelled / closed / Escape. The box is pre-filled with <paramref name="initialText"/> and selected
    /// so the user can immediately replace or extend it.
    /// </summary>
    private static async Task<string?> PromptTextAsync(Window owner, string title, string prompt, string initialText)
    {
        string? result = null;

        var textBox = new TextBox { Text = initialText, MinWidth = 360, AcceptsReturn = false };
        var okButton = new Button { Content = "OK", MinWidth = 80, IsDefault = true };
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
                    new TextBlock { Text = prompt },
                    textBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { okButton, cancelButton },
                    },
                },
            },
        };

        okButton.Click += (_, _) =>
        {
            result = textBox.Text ?? "";
            dialog.Close();
        };
        cancelButton.Click += (_, _) =>
        {
            result = null;
            dialog.Close();
        };

        // Pressing Enter in the text box confirms (IsDefault button isn't triggered from inside a TextBox).
        textBox.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                result = textBox.Text ?? "";
                dialog.Close();
            }
        };

        // Focus + select-all once shown so typing immediately replaces the current label.
        dialog.Opened += (_, _) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(owner);
        return result;
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
