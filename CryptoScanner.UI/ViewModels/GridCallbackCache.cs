using Microsoft.AspNetCore.Components.Web;

using System.Runtime.CompilerServices;

namespace CryptoScanner.UI.ViewModels;

/// <summary>
/// Keeps one set of event handlers per grid row (or per column header) alive for as long as that
/// row exists.
/// <para>
/// Why this exists: writing <c>@onclick="@(e =&gt; OnRowClick(e, vm))"</c> in a loop allocates a NEW
/// closure on every render. Blazor compares the old and the new render tree with Equals, sees two
/// different delegate objects, and concludes the handler changed: it hands out a new handler id for
/// every row and disposes the old one. Nothing on screen changes, yet the diff that goes to the web
/// view is thousands of edits — over 100 KB for a grid of a few hundred rows, several times per
/// minute, and every one of those messages ends up in native memory.
/// </para>
/// <para>
/// Handing the SAME delegate instance to Blazor on every render makes that comparison succeed, so
/// the handler id is kept and no edit is emitted at all. That is all this cache does: create the
/// delegates once per row and return the same ones afterwards.
/// </para>
/// <para>
/// A <see cref="ConditionalWeakTable{TKey, TValue}"/> is used on purpose. The delegates capture the
/// row they belong to, so a normal dictionary would keep every row that ever appeared in the grid
/// alive. The weak table drops the entry as soon as the row itself is gone, cycle and all.
/// </para>
/// </summary>
public sealed class GridCallbackCache<TRow, TCallbacks>
    where TRow : class
    where TCallbacks : class
{
    private readonly ConditionalWeakTable<TRow, TCallbacks> _table = [];
    private readonly ConditionalWeakTable<TRow, TCallbacks>.CreateValueCallback _create;

    public GridCallbackCache(Func<TRow, TCallbacks> factory)
    {
        // Stored once instead of per call, so asking for the handlers of a row allocates nothing.
        _create = new ConditionalWeakTable<TRow, TCallbacks>.CreateValueCallback(factory);
    }

    public TCallbacks Get(TRow row) => _table.GetValue(row, _create);
}


/// <summary>
/// The three handlers every data grid puts on a row. Grids that do not use all three simply leave
/// the rest at their do-nothing default.
/// </summary>
public sealed class RowCallbacks
{
    public Action<MouseEventArgs> Click { get; init; } = _ => { };
    public Action DoubleClick { get; init; } = () => { };
    public Action<MouseEventArgs> ContextMenu { get; init; } = _ => { };
}


/// <summary>
/// The four handlers every data grid puts on a column header. DragEnd is not in here: that one is
/// already a method group in the markup, so it never changes between renders.
/// </summary>
public sealed class HeaderCallbacks
{
    public Action Sort { get; init; } = () => { };
    public Action DragStart { get; init; } = () => { };
    public Action DragOver { get; init; } = () => { };
    public Action Drop { get; init; } = () => { };
}
