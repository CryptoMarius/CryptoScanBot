using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

namespace CryptoScanner.ViewModels;

/// <summary>
/// ViewModel for managing column visibility in the Object Grid
/// </summary>
public partial class ColumnWindowViewModel : ObservableObject
{
    /// <summary>
    /// Collection of column visibility items
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SignalColumnVisibilityItem> _columns = [];

    /// <summary>
    /// Constructor that takes DataGrid columns and creates view models
    /// </summary>
    public ColumnWindowViewModel(ObservableCollection<DataGridColumn> columns)
    {
        foreach (var column in columns)
        {
            var item = new SignalColumnVisibilityItem(column);
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

    /// <summary>
    /// Close command (handled by the view)
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // The actual closing is handled in the code-behind
    }
}

/// <summary>
/// Represents a single signal grid column's visibility state
/// </summary>
public partial class SignalColumnVisibilityItem : ObservableObject
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
    public SignalColumnVisibilityItem(DataGridColumn column)
    {
        _column = column;
        _header = column.Header?.ToString() ?? "Unknown";
        _isVisible = column.IsVisible;
    }

    /// <summary>
    /// When IsVisible changes, update the actual column
    /// </summary>
    partial void OnIsVisibleChanged(bool value)
    {
        _column.IsVisible = value;
    }
}
