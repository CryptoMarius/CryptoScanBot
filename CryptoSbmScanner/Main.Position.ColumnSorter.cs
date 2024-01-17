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

            int compareResult;
            try
            {
                if (ClosedPositions)
                {
                    compareResult = SortColumn switch
                    {
                        00 => ObjectCompare.Compare(positionA.Id, positionB.Id),
                        01 => ObjectCompare.Compare(positionA.CreateTime, positionB.CreateTime),
                        02 => ObjectCompare.Compare(positionA.CloseTime, positionB.CloseTime),
                        03 => ObjectCompare.Compare(positionA.Duration().TotalSeconds, positionB.Duration().TotalSeconds),
                        04 => ObjectCompare.Compare(positionA.TradeAccount.Name, positionB.TradeAccount.Name),
                        05 => ObjectCompare.Compare(positionA.Exchange.Name, positionB.Exchange.Name),
                        06 => ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name),
                        07 => ObjectCompare.Compare(positionA.Interval.IntervalPeriod, positionB.Interval.IntervalPeriod),
                        08 => ObjectCompare.Compare(positionA.StrategyText, positionB.StrategyText),
                        09 => ObjectCompare.Compare(positionA.SideText, positionB.SideText),
                        10 => ObjectCompare.Compare(positionA.Status, positionB.Status),

                        11 => ObjectCompare.Compare(positionA.Invested, positionB.Invested),
                        12 => ObjectCompare.Compare(positionA.Returned, positionB.Returned),
                        13 => ObjectCompare.Compare(positionA.Commission, positionB.Commission),

                        14 => ObjectCompare.Compare(positionA.Profit, positionB.Profit),
                        15 => ObjectCompare.Compare(positionA.Percentage, positionB.Percentage),
                        16 => ObjectCompare.Compare(positionA.PartCount, positionB.PartCount),
                        17 => ObjectCompare.Compare(positionA.EntryPrice, positionB.EntryPrice),
                        18 => ObjectCompare.Compare(positionA.ProfitPrice, positionB.ProfitPrice),
                        19 => ObjectCompare.Compare(positionA.Symbol.FundingRate, positionB.Symbol.FundingRate),
                        _ => 0
                    };
                }
                else
                {
                    compareResult = SortColumn switch
                    {
                        00 => ObjectCompare.Compare(positionA.Id, positionB.Id),
                        01 => ObjectCompare.Compare(positionA.CreateTime, positionB.CreateTime),
                        02 => ObjectCompare.Compare(positionA.UpdateTime, positionB.UpdateTime),
                        03 => ObjectCompare.Compare(positionA.Duration().TotalSeconds, positionB.Duration().TotalSeconds),
                        04 => ObjectCompare.Compare(positionA.TradeAccount.Name, positionB.TradeAccount.Name),
                        05 => ObjectCompare.Compare(positionA.Exchange.Name, positionB.Exchange.Name),
                        06 => ObjectCompare.Compare(positionA.Symbol.Name, positionB.Symbol.Name),
                        07 => ObjectCompare.Compare(positionA.Interval.IntervalPeriod, positionB.Interval.IntervalPeriod),
                        08 => ObjectCompare.Compare(positionA.StrategyText, positionB.StrategyText),
                        09 => ObjectCompare.Compare(positionA.SideText, positionB.SideText),
                        10 => ObjectCompare.Compare(positionA.Status, positionB.Status),

                        11 => ObjectCompare.Compare(positionA.Invested, positionB.Invested),
                        12 => ObjectCompare.Compare(positionA.Returned, positionB.Returned),
                        13 => ObjectCompare.Compare(positionA.Commission, positionB.Commission),

                        14 => ObjectCompare.Compare(positionA.BreakEvenPrice, positionB.BreakEvenPrice),
                        15 => ObjectCompare.Compare(positionA.Quantity, positionB.Quantity),
                        16 => ObjectCompare.Compare(positionA.Invested - positionA.Returned - positionA.Commission, positionB.Invested - positionB.Returned - positionB.Commission),
                        17 => ObjectCompare.Compare(positionA.CurrentProfit(), positionB.CurrentProfit()),
                        18 => ObjectCompare.Compare(positionA.CurrentProfitPercentage(), positionB.CurrentProfitPercentage()),

                        19 => ObjectCompare.Compare(positionA.PartCount, positionB.PartCount),
                        20 => ObjectCompare.Compare(positionA.EntryPrice, positionB.EntryPrice),
                        21 => ObjectCompare.Compare(positionA.ProfitPrice, positionB.ProfitPrice),
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
