using Avalonia.Controls;

using CryptoScanner.Core.Services;

namespace CryptoScanner.Emulator.Services;

/// <summary>
/// Saves and restores the layout of a DataGrid (which columns are shown, how wide they are and in
/// what order) through <see cref="ApplicationStateService"/>, which writes it to
/// CryptoScanBot-user.json in the data folder - the same file and the same shape the scanner uses
/// for its grids. A copy of the grid half of the scanner's ApplicationStateServiceExtensions,
/// because the emulator cannot reference the scanner project. The sort order is not in here: the
/// runs grid keeps that in the run configuration, where it already was.
/// </summary>
public static class GridStateExtensions
{
    public static void SaveGridLayout(this ApplicationStateService service, string gridName, DataGrid dataGrid)
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
    }


    public static void RestoreGridLayout(this ApplicationStateService service, string gridName, DataGrid dataGrid)
    {
        ArgumentNullException.ThrowIfNull(dataGrid);

        var columns = service.RestoreGridColumnState(gridName);
        if (columns == null || columns.Count == 0)
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

                // Restore visibility
                column.IsVisible = colSetting.IsVisible;
            }
        }

        RestoreDisplayOrder(dataGrid, columns);
    }


    /// <summary>
    /// Rebuild the display order from the saved state.
    /// Assigning DisplayIndex one column at a time renumbers the other columns, so a column that is
    /// not in the saved state (added in a newer version) gets pushed one place to the right by every
    /// following assignment and ends up at the far right of the grid. Instead the target order is
    /// determined first - a new column stays where it sits in the xaml, directly behind the column it
    /// follows there - and the indexes are then handed out in ascending order, which leaves the
    /// already positioned columns untouched.
    /// </summary>
    private static void RestoreDisplayOrder(DataGrid dataGrid, List<GridColumn> savedColumns)
    {
        Dictionary<string, int> savedOrder = [];
        foreach (var colSetting in savedColumns)
        {
            if (!string.IsNullOrEmpty(colSetting.SortMemberPath))
                savedOrder[colSetting.SortMemberPath] = colSetting.DisplayIndex;
        }

        // Sort key per column: its own saved index when we know it, otherwise the saved index of the
        // nearest preceding known column. The xaml position is the tie breaker, so an unknown column
        // sorts after the column it follows in the xaml but before the next known column.
        List<(int SavedIndex, int XamlIndex, DataGridColumn Column)> order = [];
        int previous = -1;
        for (int i = 0; i < dataGrid.Columns.Count; i++)
        {
            var column = dataGrid.Columns[i];
            if (column.SortMemberPath != null && savedOrder.TryGetValue(column.SortMemberPath, out int savedIndex))
                previous = savedIndex;
            order.Add((previous, i, column));
        }

        var sorted = order.OrderBy(o => o.SavedIndex).ThenBy(o => o.XamlIndex).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            try
            {
                sorted[i].Column.DisplayIndex = i;
            }
            catch
            {
                // ignore (will crash if we reduced the amount of columns)
            }
        }
    }
}
