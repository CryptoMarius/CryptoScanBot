using CryptoSbmScanner.Model;
using System.Collections;

namespace CryptoSbmScanner
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        public int SortColumn { set; get; } = 0;
        public SortOrder SortOrder = SortOrder.Descending;

        private readonly CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor. Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            // Initialize the CaseInsensitiveComparer object
            ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface. It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            // uit de c# documentatie

            // Cast the objects to be compared to ListViewItem objects
            ListViewItem itemA = (ListViewItem)x;
            CryptoSignal signalA = (CryptoSignal)itemA.Tag;

            ListViewItem itemB = (ListViewItem)y;
            CryptoSignal signalB = (CryptoSignal)itemB.Tag;

            int compareResult = SortColumn switch
            {
                00 => ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate),
                01 => ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name),
                02 => ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod),
                03 => ObjectCompare.Compare(signalA.ModeText, signalB.ModeText),
                04 => ObjectCompare.Compare(signalA.StrategyText, signalB.StrategyText),
                05 => ObjectCompare.Compare(signalA.EventText, signalB.EventText),
                06 => ObjectCompare.Compare(signalA.Price, signalB.Price),
                07 => ObjectCompare.Compare(signalA.PriceDiff, signalB.PriceDiff),
                08 => ObjectCompare.Compare(signalA.Volume, signalB.Volume),
                09 => ObjectCompare.Compare(signalA.TrendIndicator, signalB.TrendIndicator),
                10 => ObjectCompare.Compare(signalA.TrendPercentage, signalB.TrendPercentage),
                11 => ObjectCompare.Compare(signalA.Last24HoursChange, signalB.Last24HoursChange),
                12 => ObjectCompare.Compare(signalA.Last24HoursEffective, signalB.Last24HoursEffective),
                13 => ObjectCompare.Compare(signalA.BollingerBandsPercentage, signalB.BollingerBandsPercentage),
                14 => ObjectCompare.Compare(signalA.Rsi, signalB.Rsi),
                15 => ObjectCompare.Compare(signalA.StochOscillator, signalB.StochOscillator),
                16 => ObjectCompare.Compare(signalA.StochSignal, signalB.StochSignal),
                17 => ObjectCompare.Compare(signalA.Sma200, signalB.Sma200),
                18 => ObjectCompare.Compare(signalA.Sma50, signalB.Sma50),
                19 => ObjectCompare.Compare(signalA.Sma20, signalB.Sma20),
                20 => ObjectCompare.Compare(signalA.PSar, signalB.PSar),
                21 => ObjectCompare.Compare(signalA.FluxIndicator5m, signalB.FluxIndicator5m),
                _ => 0
            };

            // Binnen dezelfde records toch een extra onderverdeling maken, anders is het nog steeds "random"
            if (compareResult == 0)
            {
                compareResult = ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate);
                if (compareResult == 0)
                {
                    if (this.SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name);
                    else
                        compareResult = ObjectCompare.Compare(signalB.Symbol.Name, signalA.Symbol.Name);
                }
                if (compareResult == 0)
                {
                    if (this.SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod);
                    else
                        compareResult = ObjectCompare.Compare(signalB.Interval.IntervalPeriod, signalA.Interval.IntervalPeriod);
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
