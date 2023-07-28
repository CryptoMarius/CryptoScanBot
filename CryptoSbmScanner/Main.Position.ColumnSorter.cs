using CryptoSbmScanner.Model;
using System.Collections;

namespace CryptoSbmScanner
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    public class ListViewColumnSorterPosition : IComparer
    {
        public bool ClosedPositions = false;
        public int SortColumn { set; get; } = 0;
        public SortOrder SortOrder = SortOrder.Descending;

        private readonly CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor. Initializes various elements
        /// </summary>
        public ListViewColumnSorterPosition()
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
            CryptoPosition positionA = (CryptoPosition)itemA.Tag;

            ListViewItem itemB = (ListViewItem)y;
            CryptoPosition positionB = (CryptoPosition)itemB.Tag;

            int compareResult = 0;

            try
            {
                if (ClosedPositions)
                {
                    //listViewPositionsClosed.Columns.Add("Datum", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Closed", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Account", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Exchange", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Interval", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Mode", -2, HorizontalAlignment.Left);
                    //listViewPositionsClosed.Columns.Add("Status", -2, HorizontalAlignment.Left);

                    //listViewPositionsClosed.Columns.Add("Invested", -2, HorizontalAlignment.Right);
                    //listViewPositionsClosed.Columns.Add("Returned", -2, HorizontalAlignment.Right);
                    //listViewPositionsClosed.Columns.Add("Commission", -2, HorizontalAlignment.Right);

                    //listViewPositionsClosed.Columns.Add("Profit", -2, HorizontalAlignment.Right);
                    //listViewPositionsClosed.Columns.Add("Percentage", -2, HorizontalAlignment.Right);

                    //listViewPositionsClosed.Columns.Add("Parts", -2, HorizontalAlignment.Right);
                    //listViewPositionsClosed.Columns.Add("BuyPrice", -2, HorizontalAlignment.Right);
                    //listViewPositionsClosed.Columns.Add("SellPrice", -2, HorizontalAlignment.Right);
                    //listViewPositionsClosed.Columns.Add("Quantity", -2, HorizontalAlignment.Right);

                    compareResult = SortColumn switch
                    {
                        00 => ObjectCompare.Compare(positionA.Id, positionB.Id),
                        01 => ObjectCompare.Compare(positionA.CreateTime, positionB.CreateTime),
                        02 => ObjectCompare.Compare(positionA.CloseTime, positionB.CloseTime),
                        03 => ObjectCompare.Compare(positionA.TradeAccount.Name, positionB.TradeAccount.Name),
                        04 => ObjectCompare.Compare(positionA.Exchange.Name, positionB.Exchange.Name),
                        05 => ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name),
                        06 => ObjectCompare.Compare(positionA.Interval.IntervalPeriod, positionB.Interval.IntervalPeriod),
                        07 => ObjectCompare.Compare(positionA.StrategyText, positionB.StrategyText),
                        08 => ObjectCompare.Compare(positionA.SideText, positionB.SideText),
                        09 => ObjectCompare.Compare(positionA.Status, positionB.Status),

                        10 => ObjectCompare.Compare(positionA.Invested, positionB.Invested),
                        11 => ObjectCompare.Compare(positionA.Returned, positionB.Returned),
                        12 => ObjectCompare.Compare(positionA.Commission, positionB.Commission),

                        13 => ObjectCompare.Compare(positionA.Profit, positionB.Profit),
                        14 => ObjectCompare.Compare(positionA.Percentage, positionB.Percentage),

                        15 => ObjectCompare.Compare(positionA.PartCount, positionB.PartCount),
                        16 => ObjectCompare.Compare(positionA.BuyPrice, positionB.BuyPrice),
                        17 => ObjectCompare.Compare(positionA.SellPrice, positionB.SellPrice),
                        18 => ObjectCompare.Compare(positionA.Quantity, positionB.Quantity),
                        _ => 0
                    };
                }
                else
                {
                    //listViewPositionsOpen.Columns.Add("Datum", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Account", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Exchange", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Symbol", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Interval", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Strategie", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Mode", -2, HorizontalAlignment.Left);
                    //listViewPositionsOpen.Columns.Add("Status", -2, HorizontalAlignment.Left);

                    //listViewPositionsOpen.Columns.Add("BreakEven", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("Quantity", -2, HorizontalAlignment.Right);

                    //listViewPositionsOpen.Columns.Add("Invested", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("Returned", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("Open", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("Commission", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("Net NPL", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("Percentage", -2, HorizontalAlignment.Right);

                    //listViewPositionsOpen.Columns.Add("Parts", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("BuyPrice", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("SellPrice", -2, HorizontalAlignment.Right);
                    //listViewPositionsOpen.Columns.Add("LastPrice", -2, HorizontalAlignment.Right);                
                    compareResult = SortColumn switch
                    {
                        00 => ObjectCompare.Compare(positionA.Id, positionB.Id),
                        01 => ObjectCompare.Compare(positionA.CreateTime, positionB.CreateTime),
                        02 => ObjectCompare.Compare(positionA.TradeAccount.Name, positionB.TradeAccount.Name),
                        03 => ObjectCompare.Compare(positionA.Exchange.Name, positionB.Exchange.Name),
                        04 => ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name),
                        05 => ObjectCompare.Compare(positionA.Interval.IntervalPeriod, positionB.Interval.IntervalPeriod),
                        06 => ObjectCompare.Compare(positionA.StrategyText, positionB.StrategyText),
                        07 => ObjectCompare.Compare(positionA.SideText, positionB.SideText),
                        08 => ObjectCompare.Compare(positionA.Status, positionB.Status),

                        09 => ObjectCompare.Compare(positionA.BreakEvenPrice, positionB.BreakEvenPrice),
                        10 => ObjectCompare.Compare(positionA.Quantity, positionB.Quantity),

                        11 => ObjectCompare.Compare(positionA.Invested, positionB.Invested),
                        12 => ObjectCompare.Compare(positionA.Returned, positionB.Returned),
                        13 => ObjectCompare.Compare(positionA.Invested - positionA.Returned, positionB.Invested - positionB.Returned),
                        14 => ObjectCompare.Compare(positionA.Commission, positionB.Commission),

                        15 => ObjectCompare.Compare(positionA.Profit, positionB.Profit),
                        16 => ObjectCompare.Compare(positionA.Percentage, positionB.Percentage),
                        17 => ObjectCompare.Compare(positionA.MarketValue, positionB.MarketValue),
                        18 => ObjectCompare.Compare(positionA.MarketValuePercentage, positionB.MarketValuePercentage),

                        19 => ObjectCompare.Compare(positionA.PartCount, positionB.PartCount),
                        20 => ObjectCompare.Compare(positionA.BuyPrice, positionB.BuyPrice),
                        21 => ObjectCompare.Compare(positionA.SellPrice, positionB.SellPrice),
                        22 => ObjectCompare.Compare(positionA.Symbol.LastPrice, positionB.Symbol.LastPrice),
                        _ => 0
                    };
                }
            }
            catch
            {
                // ignore...
                compareResult = 0;
            }

            // Binnen dezelfde records toch een extra onderverdeling maken, anders is het nog steeds "random"
            if (compareResult == 0)
            {
                if (ClosedPositions)
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
