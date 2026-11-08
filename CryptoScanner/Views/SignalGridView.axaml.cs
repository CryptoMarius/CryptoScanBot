using Avalonia.Controls;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Services;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class SignalGridView : UserControlWithGrid<SignalViewModel>
{
    public SignalGridView()
    {
        _gridName = GridNames.Signal;
        _targetMenu = TargetMenu.Signal;

        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = this.FindControl<DataGrid>("SignalDataGrid")
            ?? throw new InvalidOperationException("SignalDataGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath.
        // Price change and minimum entry follow the last price, hence resortWhenShown.
        InitializeGrid<SignalColumnEnum, SignalColumnComparer>("Date", ListSortDirection.Descending,
            resortWhenShown: true);

        _dataGrid.SelectionChanged += OnDataGridSelectionChanged;

        // Note: hover suppression is handled via App.axaml styles (DataGridRow:pointerover),
        // so no PointerMovedEvent handler is needed here. Adding one would block the
        // DataGridColumnHeaderGripper from receiving PointerMoved, breaking column resize.
    }

    /// <summary>
    /// An open chart follows the selected signal, on the symbol and interval of that signal.
    /// </summary>
    private void OnDataGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_dataGrid.SelectedItem is SignalViewModel row)
            ChartWindowLauncher.FollowSelection(row.Object.Symbol.Base, row.Object.Symbol.Quote, row.Object.ChartInterval.Name);
    }

}