using CryptoSbmScanner.Model;
using System.Collections;

namespace CryptoSbmScanner
{
    public class ListViewColumnSorterPosition : ListViewColumnSorter
    {
        public bool ClosedPositions = false;

        public override int Compare(object x, object y)
        {
            ListViewItem itemA = (ListViewItem)x;
            CryptoPosition positionA = (CryptoPosition)itemA.Tag;

            ListViewItem itemB = (ListViewItem)y;
            CryptoPosition positionB = (CryptoPosition)itemB.Tag;

            int compareResult = 0;
            try
            {
                if (ClosedPositions)
                {
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
                        _ => 0
                    };
                }
                else
                {
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

                        15 => ObjectCompare.Compare(positionA.MarketValue, positionB.MarketValue),
                        16 => ObjectCompare.Compare(positionA.MarketValuePercentage, positionB.MarketValuePercentage),

                        17 => ObjectCompare.Compare(positionA.PartCount, positionB.PartCount),
                        18 => ObjectCompare.Compare(positionA.BuyPrice, positionB.BuyPrice),
                        19 => ObjectCompare.Compare(positionA.SellPrice, positionB.SellPrice),
                        20 => ObjectCompare.Compare(positionA.Symbol.LastPrice, positionB.Symbol.LastPrice),
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
                    if (SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name);
                    else
                        compareResult = ObjectCompare.Compare(positionB.Symbol.Name, positionA.Symbol.Name);
                }

                if (compareResult == 0)
                {
                    if (SortOrder == SortOrder.Ascending)
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

    }

}
