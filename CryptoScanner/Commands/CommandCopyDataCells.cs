using Avalonia.Controls;
using Avalonia.Data;

using System.Reflection;
using System.Text;

namespace CryptoScanner.Commands;

/// <summary>
/// Copies the cells of the ACTIVE row of the DataGrid to the clipboard as TSV
/// (tab-separated values). Pastes cleanly into Excel / Sheets.
///
/// Generic: works for any DataGrid that uses bound columns. Only columns whose
/// <see cref="DataGridColumn.IsVisible"/> is true are included, in display order.
/// No header row — just the cell values of the selected row.
///
/// Template columns (DataGridTemplateColumn) without a binding produce an empty
/// string for that cell so the column position stays aligned.
/// </summary>
public class CommandCopyDataCells : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (!GetObjectInformation(parameter, out ParameterObjects dto) || dto.datagrid == null || dto.parentWindow == null)
            return;

        var grid = dto.datagrid;

        // Visible columns, in display order.
        var columns = grid.Columns.Where(c => c.IsVisible).ToList();
        if (columns.Count == 0)
            return;

        // Active row only — the row the user right-clicked on. Avalonia's DataGrid auto-selects
        // the row under the cursor when the context menu opens, so SelectedItem is the active one.
        var row = grid.SelectedItem;
        if (row == null)
            return;

        var line = string.Join("\t", columns.Select(c => EscapeForTsv(GetCellText(c, row))));

        var clipboard = dto.parentWindow.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(line);
    }


    /// <summary>
    /// Get the displayed value of <paramref name="column"/> for the given <paramref name="row"/>.
    /// Resolves the property path in priority order:
    ///   1. <see cref="DataGridColumn.SortMemberPath"/> — explicitly set on virtually every column
    ///      in this codebase, and works regardless of binding kind.
    ///   2. The column's binding Path — handles both the classic <see cref="Binding"/> and the
    ///      compiled-binding case via reflection on a "Path" property.
    /// Returns "" when no path can be resolved or the value walk yields null.
    /// </summary>
    private static string GetCellText(DataGridColumn column, object row)
    {
        string? path = ResolvePropertyPath(column);
        if (string.IsNullOrEmpty(path))
            return "";

        var value = GetPathValue(row, path);
        return value?.ToString() ?? "";
    }


    private static string? ResolvePropertyPath(DataGridColumn column)
    {
        // Preferred: SortMemberPath. Set explicitly on every grid column in this app.
        if (!string.IsNullOrEmpty(column.SortMemberPath))
            return column.SortMemberPath;

        // Fallback: read the binding's Path. CompiledBindingExtension is the runtime type when
        // x:DataType is in effect (true throughout this codebase), so the classic 'is Binding'
        // cast does NOT match and we'd otherwise always return null. Reflect on the binding
        // object's "Path" property which both Binding (string) and CompiledBindingExtension
        // (CompiledBindingPath whose ToString() returns the dotted path) expose.
        if (column is DataGridBoundColumn bound && bound.Binding is { } iBinding)
        {
            if (iBinding is Binding plain && !string.IsNullOrEmpty(plain.Path))
                return plain.Path;

            var pathProp = iBinding.GetType().GetProperty("Path", BindingFlags.Instance | BindingFlags.Public);
            var pathObj = pathProp?.GetValue(iBinding);
            if (pathObj is string s && !string.IsNullOrEmpty(s))
                return s;
            // CompiledBindingPath.ToString() returns the property chain like "Date" / "Symbol.Name"
            var asText = pathObj?.ToString();
            if (!string.IsNullOrEmpty(asText))
                return asText;
        }

        return null;
    }


    /// <summary>
    /// Walk a dotted property path through <paramref name="root"/> using reflection.
    /// e.g. ("Symbol.Name") returns root.Symbol.Name, with null-propagation along the way.
    /// </summary>
    private static object? GetPathValue(object? root, string path)
    {
        var obj = root;
        foreach (var part in path.Split('.'))
        {
            if (obj == null)
                return null;
            var prop = obj.GetType().GetProperty(part, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
                return null;
            obj = prop.GetValue(obj);
        }
        return obj;
    }


    /// <summary>
    /// Escape a cell value for TSV: tabs/newlines/carriage-returns inside a value would
    /// break column alignment when pasted into a spreadsheet. Replace them with spaces.
    /// </summary>
    private static string EscapeForTsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }
}
