using System.Collections;

namespace CryptoSbmScanner;

public class ListViewColumnSorter : IComparer
{
    public int SortColumn { set; get; } = 0;
    public SortOrder SortOrder { set; get; } = SortOrder.Descending;

    internal CaseInsensitiveComparer ObjectCompare = new();

    //public ListViewColumnSorter()
    //{
    //    // Initialize the CaseInsensitiveComparer object
    //    //ObjectCompare = new CaseInsensitiveComparer();
    //}

    public virtual int Compare(object x, object y)
    {
        return 0;
    }


    public void ClickedOnColumn(int column)
    {
        // Determine if clicked column is already the column that is being sorted.
        if (column == SortColumn)
        {
            // Reverse the current sort direction for this column.
            if (SortOrder == SortOrder.Ascending)
                SortOrder = SortOrder.Descending;
            else
                SortOrder = SortOrder.Ascending;
        }
        else
        {
            // Set the column number that is to be sorted; default to ascending.
            SortColumn = column;
            SortOrder = SortOrder.Ascending;
        }
    }
}
