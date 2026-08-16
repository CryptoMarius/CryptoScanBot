using CryptoScanner.Core.Services;

using System.ComponentModel;

namespace CryptoScanner.UI.ViewModels;

/// <summary>
/// Runtime state for a single column: visibility, width, display order.
/// </summary>
public class ColumnRuntimeState<TEnum> where TEnum : struct, Enum
{
    public TEnum Column { get; init; }
    public string Header { get; init; } = "";
    public string CssClass { get; init; } = "";
    public bool IsVisible { get; set; }
    public double Width { get; set; }
    public int DisplayIndex { get; set; }
}

/// <summary>
/// Manages column state (visibility, width, order) for a grid and persists to ApplicationStateService.
/// Also holds the sort state. One instance per grid.
/// </summary>
public class GridColumnState<TEnum> where TEnum : struct, Enum
{
    private readonly string _gridName;
    private readonly ApplicationStateService _stateService;
    private readonly List<ColumnRuntimeState<TEnum>> _columns = [];

    public GridSortState<TEnum> SortState { get; }
    public IReadOnlyList<ColumnRuntimeState<TEnum>> Columns => _columns;

    public GridColumnState(string gridName, ApplicationStateService stateService,
        IEnumerable<GridColumnDef<TEnum>> definitions,
        TEnum? defaultSortColumn = null, ListSortDirection defaultSortDirection = ListSortDirection.Ascending)
    {
        _gridName = gridName;
        _stateService = stateService;

        foreach (var def in definitions)
        {
            _columns.Add(new ColumnRuntimeState<TEnum>
            {
                Column = def.Column,
                Header = def.Header,
                CssClass = def.CssClass,
                IsVisible = def.DefaultVisible,
                Width = def.DefaultWidth,
                DisplayIndex = def.DefaultDisplayIndex,
            });
        }

        // Restore persisted sort
        _stateService.RestoreGridSortState(gridName, out var sortColumn, out var sortDirection);
        SortState = !string.IsNullOrEmpty(sortColumn)
            ? new GridSortState<TEnum>()
            : defaultSortColumn.HasValue
                ? new GridSortState<TEnum>(defaultSortColumn.Value, defaultSortDirection)
                : new GridSortState<TEnum>();
        SortState.Restore(sortColumn, sortDirection);

        // Restore persisted column state (visibility, width, order)
        RestoreColumnState();
    }

    /// <summary>
    /// Returns columns ordered by DisplayIndex, filtered to visible only.
    /// </summary>
    public IEnumerable<ColumnRuntimeState<TEnum>> VisibleColumns =>
        _columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex);

    public void SetVisibility(TEnum column, bool visible)
    {
        var col = _columns.FirstOrDefault(c => c.Column.Equals(column));
        if (col != null)
            col.IsVisible = visible;
    }

    public void SetAllVisible(bool visible)
    {
        foreach (var col in _columns)
            col.IsVisible = visible;
    }

    public void SaveSort()
    {
        _stateService.SaveGridSortState(_gridName, SortState.SortColumnName, SortState.SortDirection);
    }

    public void SaveColumnState()
    {
        _stateService.SaveGridColumnState(_gridName, _columns.Select(c => new GridColumn
        {
            SortMemberPath = c.Column.ToString()!,
            Width = c.Width,
            DisplayIndex = c.DisplayIndex,
            IsVisible = c.IsVisible,
        }).ToList());
    }

    private void RestoreColumnState()
    {
        var persisted = _stateService.RestoreGridColumnState(_gridName);
        if (persisted == null || persisted.Count == 0)
            return;

        Dictionary<TEnum, int> savedOrder = [];
        foreach (var saved in persisted)
        {
            if (Enum.TryParse<TEnum>(saved.SortMemberPath, out var col))
            {
                var runtime = _columns.FirstOrDefault(c => c.Column.Equals(col));
                if (runtime != null)
                {
                    runtime.IsVisible = saved.IsVisible;
                    if (saved.Width > 0)
                        runtime.Width = saved.Width;
                    savedOrder[col] = saved.DisplayIndex;
                }
            }
        }

        RestoreDisplayOrder(savedOrder);
    }

    /// <summary>
    /// Rebuild the display order from the saved state. A column that is not in the saved state (added
    /// in a newer version) keeps the position it has in the column definitions, directly behind the
    /// column it follows there, instead of taking over the index of an existing column. The indexes
    /// are handed out again afterwards so they stay unique, which the drag and drop reorder relies on.
    /// </summary>
    private void RestoreDisplayOrder(Dictionary<TEnum, int> savedOrder)
    {
        // Sort key per column: its own saved index when we know it, otherwise the saved index of the
        // nearest preceding known column. The definition order is the tie breaker.
        List<(int SavedIndex, int DefinitionIndex, ColumnRuntimeState<TEnum> Column)> order = [];
        int previous = -1;
        for (int i = 0; i < _columns.Count; i++)
        {
            if (savedOrder.TryGetValue(_columns[i].Column, out int savedIndex))
                previous = savedIndex;
            order.Add((previous, i, _columns[i]));
        }

        var sorted = order.OrderBy(o => o.SavedIndex).ThenBy(o => o.DefinitionIndex).ToList();
        for (int i = 0; i < sorted.Count; i++)
            sorted[i].Column.DisplayIndex = i;
    }
}
