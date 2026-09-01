namespace CryptoScanner.UI.ViewModels;

/// <summary>
/// The selection of one grid, held outside the page. The tabs of this UI are routes: leaving a tab
/// disposes the page and every field on it, so a selection kept in the page itself is gone the
/// moment the user looks at another tab. The services live for the whole session, so the selection
/// is kept there instead - the same way the Avalonia grids keep theirs, where the view stays alive
/// while another tab is on screen.
/// <para>
/// The rows are matched on a key of the underlying object instead of on the identity of the view
/// model, because a reload throws the view models away and builds new ones for the same positions
/// or signals. See <see cref="Rebind"/>.
/// </para>
/// <para>
/// The methods here are called from the scanner threads while the renderer reads the selection, so
/// they do not empty and refill the set in place: they build a new one and swap it in. A reader
/// then sees either the old set or the new one, never one that is halfway.
/// </para>
/// </summary>
public class GridSelectionState<T>(Func<T, object> keySelector) where T : class
{
    /// <summary>
    /// The row the last click landed on. It is the anchor a shift-click measures its range from.
    /// </summary>
    public T? Current { get; set; }

    /// <summary>
    /// Every selected row - the grids allow an extended (ctrl/shift) selection. The page adds to and
    /// removes from this set directly, which is safe because those are clicks on the render thread.
    /// </summary>
    public HashSet<T> Selected { get; private set; } = [];

    public void Clear()
    {
        Current = null;
        Selected = [];
    }

    /// <summary>
    /// Drop a row that no longer exists (deleted, closed, filtered away).
    /// </summary>
    public void Remove(T row)
    {
        if (Selected.Contains(row))
        {
            HashSet<T> reduced = new(Selected);
            reduced.Remove(row);
            Selected = reduced;
        }
        if (ReferenceEquals(Current, row))
            Current = null;
    }

    /// <summary>
    /// Move the selection over to a freshly built list of rows. Without this a reload - the hourly
    /// symbol refresh, an exchange switch - silently empties the selection, because the new view
    /// models are different objects even though they show the same positions.
    /// </summary>
    public void Rebind(IEnumerable<T> rows)
    {
        if (Selected.Count == 0 && Current == null)
            return;

        var selectedKeys = Selected.Select(keySelector).ToHashSet();
        object? currentKey = Current != null ? keySelector(Current) : null;

        HashSet<T> rebound = [];
        T? current = null;
        foreach (T row in rows)
        {
            object key = keySelector(row);
            if (selectedKeys.Contains(key))
                rebound.Add(row);
            if (currentKey != null && key.Equals(currentKey))
                current = row;
        }

        Selected = rebound;
        Current = current;
    }
}
