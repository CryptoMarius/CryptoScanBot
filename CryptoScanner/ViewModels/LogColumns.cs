using Avalonia.Controls;
using Avalonia.Layout;

using CryptoScanner.Core.Model;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

public enum LogColumnEnum
{
    Date,
    Line,
}

public class LogColumnComparer : IGridComparer<CryptoLog, LogColumnEnum>
{
    public LogColumnEnum SortColumn { get; set; }
    public ListSortDirection SortDirection { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public int Compare(CryptoLog? a, CryptoLog? b)
    {
        if (a == null || b == null)
            return 0;

        try
        {
            int compareResult = SortColumn switch
            {
                LogColumnEnum.Date => ObjectCompare.Compare(a.Date, b.Date),
                LogColumnEnum.Line => ObjectCompare.Compare(a.Text, b.Text),
                _ => 0
            };

            // Apply sort direction
            if (SortDirection == ListSortDirection.Descending)
                compareResult = -compareResult;

            // Secondary sort on Date if primary is equal
            if (compareResult == 0 && SortColumn != LogColumnEnum.Date)
                compareResult = ObjectCompare.Compare(a.Date, b.Date);

            if (SortDirection == ListSortDirection.Descending)
                return -compareResult;
            else
                return compareResult;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}


public static class LogColumns
{
    public static ObservableCollection<GridColumnDefinition<LogColumnEnum>> GetColumns()
    {
        var columns = new ObservableCollection<GridColumnDefinition<LogColumnEnum>>
        {
            new() { ColumnEnum = LogColumnEnum.Date, Header = "Date", Width = 125, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = LogColumnEnum.Line, Header = "Text", Width = 800, Alignment = HorizontalAlignment.Left},
        };

        // Initialize DisplayIndex
        int index = 0;
        foreach (var column in columns)
        {
            column.ActualWidth = new GridLength(column.Width);
            column.DisplayIndex = index;
            index++;
        }
        return columns;
    }


}