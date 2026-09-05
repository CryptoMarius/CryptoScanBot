using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

namespace CryptoScanner.Emulator.ViewModels;

/// <summary>
/// ViewModel for managing column visibility in the runs grid. A copy of the scanner's
/// ColumnWindowViewModel: the emulator cannot reference the scanner project, and the dialog
/// should work the same way in both.
/// </summary>
public partial class ColumnWindowViewModel : ObservableObject
{
    /// <summary>
    /// Collection of column visibility items
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ColumnVisibilityItem> _columns = [];

    /// <summary>
    /// Constructor that takes DataGrid columns and creates view models
    /// </summary>
    public ColumnWindowViewModel(ObservableCollection<DataGridColumn> columns)
    {
        foreach (var column in columns)
        {
            var item = new ColumnVisibilityItem(column);
            _columns.Add(item);
        }
    }

    /// <summary>
    /// Select all columns to be visible
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var column in Columns)
        {
            column.IsVisible = true;
        }
    }

    /// <summary>
    /// Deselect all columns to be hidden
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var column in Columns)
        {
            column.IsVisible = false;
        }
    }
}


/// <summary>
/// Represents a single grid column's visibility state
/// </summary>
public partial class ColumnVisibilityItem : ObservableObject
{
    private readonly DataGridColumn _column;

    /// <summary>
    /// Header text of the column
    /// </summary>
    [ObservableProperty]
    private string _header;

    /// <summary>
    /// Visibility state of the column
    /// When changed, updates the actual DataGrid column
    /// </summary>
    [ObservableProperty]
    private bool _isVisible;

    /// <summary>
    /// Constructor
    /// </summary>
    public ColumnVisibilityItem(DataGridColumn column)
    {
        _column = column;
        _header = GetHeaderText(column);
        _isVisible = column.IsVisible;
    }

    /// <summary>
    /// The text to show for a column. A header is not always a plain string: a column can carry a
    /// control as its header (a TextBlock, for instance, to align the header text), and calling
    /// ToString() on that yields the type name ("Avalonia.Controls.TextBlock") instead of the text.
    /// </summary>
    private static string GetHeaderText(DataGridColumn column)
    {
        string? text = ExtractText(column.Header);
        if (string.IsNullOrWhiteSpace(text))
            text = column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(text))
            return "Unknown";
        return text;
    }

    /// <summary>
    /// Dig the text out of a header, whatever it was built from. Anything else that is a control
    /// returns null, so the caller can fall back to the sort member path.
    /// </summary>
    private static string? ExtractText(object? header)
    {
        return header switch
        {
            null => null,
            string text => text,
            TextBlock textBlock => textBlock.Text,
            ContentControl contentControl => ExtractText(contentControl.Content),
            Control => null,
            _ => header.ToString(),
        };
    }

    /// <summary>
    /// When IsVisible changes, update the actual column
    /// </summary>
    partial void OnIsVisibleChanged(bool value)
    {
        _column.IsVisible = value;
    }
}
