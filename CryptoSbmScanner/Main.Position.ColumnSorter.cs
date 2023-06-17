using CryptoSbmScanner.Model;
using System.Collections;

namespace CryptoSbmScanner
{
    public class ListViewColumnSorterPosition : IComparer
    {
        public int SortColumn { set; get; } = 0;
        public SortOrder SortOrder = SortOrder.Descending;

        private readonly CaseInsensitiveComparer ObjectCompare;

        public ListViewColumnSorterPosition()
        {
            ObjectCompare = new CaseInsensitiveComparer();
        }

        public int Compare(object x, object y)
        {
            // uit de c# documentatie

            // Cast the objects to be compared to ListViewItem objects
            ListViewItem itemA = (ListViewItem)x;
            CryptoPosition positionA = (CryptoPosition)itemA.Tag;

            ListViewItem itemB = (ListViewItem)y;
            CryptoPosition positionB = (CryptoPosition)itemB.Tag;

            int compareResult = 0;

            //int compareResult = SortColumn switch
            //{
            // wellicht later, eerst maar gewoon op CreateTime
            //    00 => ObjectCompare.Compare(positionA.CreateTime, positionB.CreateTime),
            //    01 => ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name),
            //    02 => ObjectCompare.Compare(positionA.Interval.IntervalPeriod, positionB.Interval.IntervalPeriod),
            //    03 => ObjectCompare.Compare(positionA.ModeText, positionB.ModeText),
            //    04 => ObjectCompare.Compare(positionA.StrategyText, positionB.StrategyText),
            //    05 => ObjectCompare.Compare(positionA.BuyPrice, positionB.BuyPrice),
            //    _ => 0
            //};

            // Binnen dezelfde records toch een extra onderverdeling maken, anders is het nog steeds "random"
            if (compareResult == 0)
            {
                if (positionA.CloseTime.HasValue)
                    compareResult = ObjectCompare.Compare(positionA.CloseTime, positionB.CloseTime);
                else
                    compareResult = ObjectCompare.Compare(positionA.CreateTime, positionB.CreateTime);
                if (compareResult == 0)
                {
                    if (this.SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name);
                    else
                        compareResult = ObjectCompare.Compare(positionB.Symbol.Name, positionA.Symbol.Name);
                }
                if (compareResult == 0)
                {
                    if (this.SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(positionA.Interval.IntervalPeriod, positionB.Interval.IntervalPeriod);
                    else
                        compareResult = ObjectCompare.Compare(positionB.Interval.IntervalPeriod, positionA.Interval.IntervalPeriod);
                }
            }


            // Calculate correct return value based on object comparison
            if (SortOrder == SortOrder.Ascending)
                return compareResult;
            else if (SortOrder == SortOrder.Descending)
                return (-compareResult);
            else
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


}
