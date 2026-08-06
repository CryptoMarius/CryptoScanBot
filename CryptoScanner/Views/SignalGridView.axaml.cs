using Avalonia.Controls;

using CryptoScanner.Core.Enums;
using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class SignalGridView : UserControlWithGrid<SignalViewModel>
{
    public SignalGridView()
    {
        _gridName = "SignalGrid";
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

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<SignalColumnEnum, SignalColumnComparer>("Date", ListSortDirection.Descending);

        // Note: hover suppression is handled via App.axaml styles (DataGridRow:pointerover),
        // so no PointerMovedEvent handler is needed here. Adding one would block the
        // DataGridColumnHeaderGripper from receiving PointerMoved, breaking column resize.
    }

}