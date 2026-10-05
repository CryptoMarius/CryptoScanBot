using Microsoft.JSInterop;

namespace CryptoScanner.UI.Services;

/// <summary>
/// The keyboard navigation the grids share: the arrow keys, page up and down, home and end move the
/// active row, the way a DataGrid does. The Avalonia grids get this from the control itself; here
/// the rows are plain table rows, so without this the browser only scrolls the container and the
/// selection stays where it was and slides out of view.
/// </summary>
public static class GridNavigation
{
    /// <summary>
    /// The row a navigation key moves to, or -1 when the key is not a navigation key or there is
    /// nothing to move to. A page is however many rows actually fit, asked of the browser through
    /// the row that is current now.
    /// </summary>
    public static async Task<int> TargetIndexAsync(IJSRuntime js, string? key, int current, int count, Func<int, string> rowId)
    {
        if (count == 0 || string.IsNullOrEmpty(key))
            return -1;

        int pageSize = 15;
        if (key == "PageDown" || key == "PageUp")
        {
            try
            {
                int rows = await js.InvokeAsync<int>("GridInterop.visibleRowCount", rowId(Math.Max(current, 0)));
                if (rows > 1)
                    pageSize = rows - 1; // one row of overlap, so you keep your bearings
            }
            catch (JSException) { }
        }

        return key switch
        {
            "ArrowDown" => current < 0 ? 0 : Math.Min(current + 1, count - 1),
            "ArrowUp" => current <= 0 ? 0 : current - 1,
            "PageDown" => current < 0 ? 0 : Math.Min(current + pageSize, count - 1),
            "PageUp" => current < 0 ? 0 : Math.Max(current - pageSize, 0),
            "Home" => 0,
            "End" => count - 1,
            _ => -1,
        };
    }

    /// <summary>
    /// Bring the row into view, no further than needed.
    /// </summary>
    public static async Task ScrollRowIntoViewAsync(IJSRuntime js, string rowId)
    {
        try
        {
            await js.InvokeVoidAsync("GridInterop.scrollRowIntoView", rowId);
        }
        catch (JSException) { }
    }
}
