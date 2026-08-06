using CryptoScanner.Core.Enums;

using System.Collections;

namespace CryptoScanner.ViewModels;

public class LogColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private LogColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public LogColumnComparer(LogColumnEnum? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is LogViewModel a && y is LogViewModel b)
        {
            try
            {
                int compareResult = SortColumn switch
                {
                    LogColumnEnum.Date => ObjectCompare.Compare(a.Date, b.Date),
                    LogColumnEnum.Text => ObjectCompare.Compare(a.Text, b.Text),
                    _ => 0
                };

                // Sort on some more columns...
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Date, b.Date);

                return compareResult;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        return 0;
    }
}
