using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

using CryptoScanner.Core.Services;

using System.ComponentModel;

namespace CryptoScanner.Services;

/// <summary>
/// Avalonia-specific extension methods for ApplicationStateService.
/// These were moved out of Core to keep it framework-agnostic.
/// </summary>
public static class ApplicationStateServiceExtensions
{
    public static void SaveGridState(this ApplicationStateService service, string gridName, DataGrid dataGrid, string? sortColumn, ListSortDirection? sortDirection)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);

        var columns = dataGrid.Columns.Select(col => new GridColumn
        {
            SortMemberPath = col.SortMemberPath ?? string.Empty,
            Width = col.Width.IsAbsolute ? col.Width.Value : -1,  // -1 = Auto
            DisplayIndex = col.DisplayIndex,
            IsVisible = col.IsVisible
        }).ToList();

        service.SaveGridColumnState(gridName, columns);
        service.SaveGridSortState(gridName, sortColumn, sortDirection);
    }

    public static void RestoreGridState(this ApplicationStateService service, string gridName, DataGrid dataGrid, out string sortColumn, out ListSortDirection sortDirection)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);

        service.RestoreGridSortState(gridName, out sortColumn, out sortDirection);

        var columns = service.RestoreGridColumnState(gridName);
        if (columns == null)
            return;

        foreach (var colSetting in columns)
        {
            var column = dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == colSetting.SortMemberPath);

            if (column != null)
            {
                // Restore width
                if (colSetting.Width > 0)
                {
                    column.Width = new DataGridLength(colSetting.Width);
                }

                try
                {
                    // Restore display order (must be in range of available columns)
                    column.DisplayIndex = colSetting.DisplayIndex;
                }
                catch
                {
                    // ignore (wil crash if we reduced the amount of columns)
                }

                // Restore visibility
                column.IsVisible = colSetting.IsVisible;
            }
        }
    }

    public static void SaveWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        service.SaveWindowStateValues(windowName,
            window.Position.X, window.Position.Y,
            window.Width, window.Height,
            window.WindowState.ToString());

        service.FlushToDisk();
        service.FlushWindowStateToDisk();
    }

    public static void RestoreWindowState(this ApplicationStateService service, string windowName, Window window)
    {
        var state = service.GetOrCreateWindowState(windowName);
        if (string.IsNullOrEmpty(state.State))
            return;

        if (Enum.TryParse<Avalonia.Controls.WindowState>(state.State, out var windowState))
        {
            Screen? targetScreen;
            if (IsPositionOnScreen(window, state.X, state.Y, out targetScreen))
            {
                window.Position = new PixelPoint((int)state.X, (int)state.Y);
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                targetScreen = window.Screens.Primary ?? window.Screens.All.FirstOrDefault();
            }

            // Clamp to the working area of the target screen, so a size saved on a large
            // display doesn't end up partially off-screen when restored on a smaller one
            double width = state.Width;
            double height = state.Height;
            if (targetScreen != null)
            {
                double scaling = targetScreen.Scaling > 0 ? targetScreen.Scaling : 1.0;
                width = Math.Min(width, targetScreen.WorkingArea.Width / scaling);
                height = Math.Min(height, targetScreen.WorkingArea.Height / scaling);
            }

            window.Width = width;
            window.Height = height;
            window.WindowState = windowState;
        }
    }

    private static bool IsPositionOnScreen(Window window, double x, double y, out Screen? matchedScreen)
    {
        matchedScreen = null;
        try
        {
            var point = new PixelPoint((int)x, (int)y);
            var screens = window.Screens.All;

            // Check if point is within ANY screen's working area
            foreach (var screen in screens)
            {
                if (screen.WorkingArea.Contains(point))
                {
                    matchedScreen = screen;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // Op sommige Linux window managers kan dit falen
            return false;
        }
    }
}
