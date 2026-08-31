using Avalonia.Controls;
using Avalonia.Input;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Services;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.Collections;
using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class SymbolGridView : UserControlWithGrid<SymbolViewModel>
{
    public SymbolGridView()
    {
        _gridName = GridNames.Symbol;
        _targetMenu = TargetMenu.Symbol;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = SymbolDataGrid;
        if (_dataGrid == null)
            throw new InvalidOperationException("SymbolDataGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<SymbolColumnEnum, SymbolColumnComparer>("Symbol", ListSortDirection.Ascending);

        _dataGrid.KeyDown += OnDataGridKeyDown;
    }

    private void OnDataGridKeyDown(object? sender, KeyEventArgs e)
    {
        // Only handle printable single-character keys (letters/digits)
        if (e.Key == Key.None)
            return;

        char ch = e.Key switch
        {
            >= Key.A and <= Key.Z => (char)('A' + (e.Key - Key.A)),
            >= Key.D0 and <= Key.D9 => (char)('0' + (e.Key - Key.D0)),
            _ => '\0'
        };

        if (ch == '\0')
            return;

        string prefix = ch.ToString();

        if (_dataGrid.ItemsSource is not IEnumerable items)
            return;

        // Find first item whose Symbol starts with the pressed character (case-insensitive)
        SymbolViewModel? match = null;
        foreach (var item in items)
        {
            if (item is SymbolViewModel vm &&
                vm.Symbol.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                match = vm;
                break;
            }
        }

        if (match == null)
            return;

        _dataGrid.SelectedItem = match;
        _dataGrid.ScrollIntoView(match, null);
        e.Handled = true;
    }

}