using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

using CryptoScanner.Commands;
using CryptoScanner.Core.Enums;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class PositionClosedGridView : UserControlWithGrid<PositionViewModel>
{
    public PositionClosedGridView()
    {
        _gridName = "PositionClosedGrid";
        _targetMenu = TargetMenu.Position;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = PositionClosedGrid;
        if (_dataGrid == null)
            throw new InvalidOperationException("PositionClosedGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<PositionColumnEnum, PositionColumnComparer>("CloseTime", ListSortDirection.Descending);

        // Delete key removes the selected position from the database (same as the "Position delete from database" context menu item)
        _dataGrid.AddHandler(KeyDownEvent, OnDataGridKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void OnDataGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None && _dataGrid.SelectedItem != null)
        {
            var parentWindow = this.FindAncestorOfType<Window>();
            var command = new CommandPositionDelete();
            command.Execute((_dataGrid, _dataGrid.SelectedItem, parentWindow));
            e.Handled = true;
        }
    }
}