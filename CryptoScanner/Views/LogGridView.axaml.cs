using Avalonia.Controls;

using CryptoScanner.Model;
using CryptoScanner.ViewModels;

using System.ComponentModel;


namespace CryptoScanner.Views;

public partial class LogGridView : UserControlWithGrid<LogViewModel>
{
    public LogGridView()
    {
        _gridName = "LogGrid";
        _targetMenu = TargetMenu.Log;
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            // Designer mode
            _dataGrid = null!;
            _applicationStateService = null!;
            return;
        }

        _dataGrid = this.FindControl<DataGrid>("LogDataGrid")
            ?? throw new InvalidOperationException("LogDataGrid not found");

        // Register a custom comparer for each column based on its SortMemberPath
        InitializeGrid<LogColumnEnum, LogColumnComparer>("Date", ListSortDirection.Ascending);

    }
}
