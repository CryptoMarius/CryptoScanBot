using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

public interface IGridComparer<TItem, TColumnEnum> : IComparer<TItem>
    where TItem : class
    where TColumnEnum : struct, Enum
{
    TColumnEnum SortColumn { get; set; }
    ListSortDirection SortDirection { get; set; }
}

public abstract partial class BaseGridViewModel<TItem, TColumnEnum, TComparer> : ObservableObject
    where TItem : class
    where TColumnEnum : struct, Enum
    where TComparer : IGridComparer<TItem, TColumnEnum>, new() // <-- Added new() constraint
{
    // Common fields
    internal readonly object _lock = new();
    internal TColumnEnum SortColumn;
    internal ListSortDirection SortDirection = ListSortDirection.Descending;

    // All loaded objects (not necessarily all visible)
    protected List<TItem> _allObjects = [];

    // The visible objects (part of the _allObjects)
    [ObservableProperty]
    private AvaloniaList<TItem> _visibleObjects = [];

    [ObservableProperty]
    protected TItem? _selectedObject = null;

    [ObservableProperty]
    protected ObservableCollection<GridLength> _columnWidths = [];

    [ObservableProperty]
    protected ObservableCollection<GridColumnDefinition<TColumnEnum>> _columns = [];
    internal IEnumerable<GridColumnDefinition<TColumnEnum>>? _columnsVisible;

    // Common properties
    public IEnumerable<GridColumnDefinition<TColumnEnum>> VisibleColumns
    {
        get
        {
            if (_columnsVisible == null)
            {
                _columnsVisible = Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToList();
            }
            return _columnsVisible;
        }
    }

    partial void OnColumnsChanged(ObservableCollection<GridColumnDefinition<TColumnEnum>> value)
    {
        _columnsVisible = null;
        OnPropertyChanged(nameof(VisibleColumns));
    }

    public int TotalCount
    {
        get
        {
            lock (_lock)
            {
                return _allObjects.Count;
            }
        }
    }

    public void SortByColumn(TColumnEnum columnEnum)
    {
        if (SortColumn.Equals(columnEnum))
        {
            SortDirection = SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            SortColumn = columnEnum;
            SortDirection = ListSortDirection.Ascending;
        }

        lock (_lock)
        {
            ApplySort(SortColumn);
        }

        RefreshVisibleItems();
    }

    internal void ApplySort(TColumnEnum sortColumn)
    {
        var comparer = new TComparer
        {
            SortColumn = sortColumn,
            SortDirection = this.SortDirection,
        };
        System.Diagnostics.Debug.WriteLine($"ApplySort({nameof(TColumnEnum)} {SortColumn}, {SortDirection})");
        _allObjects.Sort(comparer);
        //System.Diagnostics.Debug.WriteLine($"First 3: {string.Join(", ", _allObjects.Take(3).Select(x => x.Name))}");
    }

    protected abstract void RefreshVisibleItems();

    // Common column width methods
    public void UpdateColumnWidths(string widthsString)
    {
        var parts = widthsString.Split(',');
        var newWidths = new ObservableCollection<GridLength>();

        foreach (var part in parts)
        {
            if (part.Trim() == "*")
                newWidths.Add(new GridLength(1, GridUnitType.Star));
            else if (double.TryParse(part.Trim(), out var value))
                newWidths.Add(new GridLength(value));
        }

        if (newWidths.Count > 0)
            ColumnWidths = newWidths;
    }

    public string GetColumnWidthsString()
    {
        var parts = ColumnWidths.Select(w =>
        {
            if (w.IsStar)
                return "*";
            return w.Value.ToString("F0");
        });

        return string.Join(",", parts);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _allObjects.Clear();
        }
        RefreshVisibleItems();
    }


    public static ObservableCollection<GridLength> GetWidths(IEnumerable<GridColumnDefinition<TColumnEnum>> columns)
    {
        // Build ColumnWidths from Columns
        var columnWidths = new ObservableCollection<GridLength>();
        foreach (var column in columns.OrderBy(c => c.DisplayIndex))
        {
            if (column.IsVisible)
                columnWidths.Add(new GridLength(column.Width));
            else
                columnWidths.Add(new GridLength(0));

            // Add splitter width (except after last column)
            if (column != columns.Last())
            {
                if (column.IsVisible)
                    columnWidths.Add(new GridLength(5));
                else
                    columnWidths.Add(new GridLength(0));
            }
        }
        if (columnWidths.Count > 0)
            columnWidths[^1] = new GridLength(1, GridUnitType.Star); // Last column is star-sized
        return columnWidths;
    }
}