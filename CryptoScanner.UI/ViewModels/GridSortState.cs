using System.ComponentModel;

namespace CryptoScanner.UI.ViewModels;

/// <summary>
/// Reusable sort state for any grid. Tracks current sort column (as enum) and direction.
/// Matches the Avalonia UserControlWithGrid sort behavior: click toggles direction,
/// click different column resets to ascending.
/// </summary>
public class GridSortState<TEnum> where TEnum : struct, Enum
{
    public TEnum? SortColumn { get; private set; }
    public ListSortDirection SortDirection { get; private set; } = ListSortDirection.Ascending;

    public GridSortState()
    {
    }

    public GridSortState(TEnum defaultColumn, ListSortDirection defaultDirection = ListSortDirection.Ascending)
    {
        SortColumn = defaultColumn;
        SortDirection = defaultDirection;
    }

    public void ToggleSort(TEnum column)
    {
        if (SortColumn?.Equals(column) == true && SortDirection == ListSortDirection.Ascending)
            SortDirection = ListSortDirection.Descending;
        else
        {
            SortColumn = column;
            SortDirection = ListSortDirection.Ascending;
        }
    }

    public string GetSortIndicator(TEnum column)
    {
        if (SortColumn == null || !SortColumn.Value.Equals(column))
            return "";
        return SortDirection == ListSortDirection.Ascending ? " ▲" : " ▼";
    }

    public bool IsAscending => SortDirection == ListSortDirection.Ascending;

    /// <summary>
    /// Restore sort state from persisted strings (e.g. ApplicationStateService).
    /// </summary>
    public void Restore(string? sortColumnName, ListSortDirection? direction)
    {
        if (!string.IsNullOrEmpty(sortColumnName) && Enum.TryParse<TEnum>(sortColumnName, out var col))
        {
            SortColumn = col;
            SortDirection = direction ?? ListSortDirection.Ascending;
        }
    }

    public string? SortColumnName => SortColumn?.ToString();
}
