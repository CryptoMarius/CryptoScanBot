using Avalonia.Controls;
using Avalonia.Data;

using System.Collections;
using System.Reflection;
using System.Text;

namespace CryptoScanner.Commands;

/// <summary>
/// Copies the visible cells of the DataGrid to the clipboard as TSV (tab-separated values,
/// with the column headers on the first line). Pastes cleanly into Excel / Sheets.
///
/// Generic: works for any DataGrid that uses bound columns (DataGridTextColumn etc.). When
/// the user has selected one or more rows those are exported; with no selection ALL items
/// of the grid are exported. Only columns whose IsVisible is true are included, in the
/// same order they appear on screen.
///
/// Template columns (DataGridTemplateColumn) have no binding path so their cell value is
/// emitted as an empty string — the column still appears in the header so the layout
/// stays aligned with the visible grid.
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

        // Visible columns, in display order. DataGrid.Columns is already in display order;
        // we only filter on IsVisible. Hidden columns are skipped entirely (header + data).
        var columns = grid.Columns.Where(c => c.IsVisible).ToList();
        if (columns.Count == 0)
            return;

        // Row set: prefer the selection so the user can copy a few rows; fall back to the
        // entire grid when nothing is selected.
        List<object> rows = [];
        if (grid.SelectedItems != null && grid.SelectedItems.Count > 0)
        {
            foreach (var item in grid.SelectedItems)
            {
                if (item != null)
                    rows.Add(item);
            }
        }
        else if (grid.ItemsSource is IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item != null)
                    rows.Add(item);
            }
        }

        if (rows.Count == 0)
            return;

        var sb = new StringBuilder();

        // Header line
        sb.AppendLine(string.Join("\t", columns.Select(c => EscapeForTsv(c.Header?.ToString() ?? ""))));

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join("\t", columns.Select(c => EscapeForTsv(GetCellText(c, row)))));
        }

        var clipboard = dto.parentWindow.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(sb.ToString());
    }


    /// <summary>
    /// Get the displayed value of <paramref name="column"/> for the given <paramref name="row"/>.
    /// Uses the column's binding path (dotted, e.g. "Symbol.Name") with reflection.
    /// Returns "" when the column is not a bound column or the path cannot be resolved.
    /// </summary>
    private static string GetCellText(DataGridColumn column, object row)
    {
        if (column is DataGridBoundColumn bound && bound.Binding is Binding binding && !string.IsNullOrEmpty(binding.Path))
        {
            var value = GetPathValue(row, binding.Path);
            return value?.ToString() ?? "";
        }

        // Template / unbound columns: no straightforward way to extract the displayed value
        // without rendering the template. Leave blank rather than guessing.
        return "";
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
    /// break row/column alignment when pasted into a spreadsheet. Replace them with spaces.
    /// </summary>
    private static string EscapeForTsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }
}
