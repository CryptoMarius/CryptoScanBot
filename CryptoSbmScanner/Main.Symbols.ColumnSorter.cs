using CryptoSbmScanner.Model;

namespace CryptoSbmScanner
{
    public class ListViewColumnSorterSymbol : ListViewColumnSorter
    {
        public override int Compare(object x, object y)
        {
            ListViewItem itemA = (ListViewItem)x;
            CryptoSymbol symbolA = (CryptoSymbol)itemA.Tag;

            ListViewItem itemB = (ListViewItem)y;
            CryptoSymbol symbolB = (CryptoSymbol)itemB.Tag;

            int compareResult = SortColumn switch
            {
                00 => ObjectCompare.Compare(symbolA.Name, symbolB.Name),
                01 => ObjectCompare.Compare(symbolA.Volume, symbolB.Volume),
                02 => ObjectCompare.Compare(symbolA.LastPrice, symbolB.LastPrice),
                _ => 0
            };


            // Extra defaults (maar waarom omgedraaid?)
            if (compareResult == 0 && SortColumn > 0)
            {
                compareResult = ObjectCompare.Compare(symbolA.Name, symbolB.Name);
            }


            // Calculate correct return value based on object comparison
            if (SortOrder == SortOrder.Ascending)
                return compareResult;
            else if (SortOrder == SortOrder.Descending)
                return (-compareResult);
            else
                return 0;
        }
    }

}
