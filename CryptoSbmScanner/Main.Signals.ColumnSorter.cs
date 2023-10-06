using CryptoSbmScanner.Model;

namespace CryptoSbmScanner
{
    public class ListViewColumnSorterSignal : ListViewColumnSorter
    {
        public override int Compare(object x, object y)
        {
            ListViewItem itemA = (ListViewItem)x;
            CryptoSignal signalA = (CryptoSignal)itemA.Tag;

            ListViewItem itemB = (ListViewItem)y;
            CryptoSignal signalB = (CryptoSignal)itemB.Tag;

            int compareResult = SortColumn switch
            {
                00 => ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate),
                01 => ObjectCompare.Compare(signalA.Exchange.Name, signalB.Exchange.Name),
                02 => ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name),
                03 => ObjectCompare.Compare(signalA.Interval.IntervalPeriod, signalB.Interval.IntervalPeriod),
                04 => ObjectCompare.Compare(signalA.SideText, signalB.SideText),
                05 => ObjectCompare.Compare(signalA.StrategyText, signalB.StrategyText),
                06 => ObjectCompare.Compare(signalA.EventText, signalB.EventText),
                07 => ObjectCompare.Compare(signalA.Price, signalB.Price),
                08 => ObjectCompare.Compare(signalA.PriceDiff, signalB.PriceDiff),
                09 => ObjectCompare.Compare(signalA.Volume, signalB.Volume),
                10 => ObjectCompare.Compare(signalA.TrendIndicator, signalB.TrendIndicator),
                11 => ObjectCompare.Compare(signalA.TrendPercentage, signalB.TrendPercentage),
                12 => ObjectCompare.Compare(signalA.Last24HoursChange, signalB.Last24HoursChange),
                13 => ObjectCompare.Compare(signalA.Last24HoursEffective, signalB.Last24HoursEffective),
                14 => ObjectCompare.Compare(signalA.BollingerBandsPercentage, signalB.BollingerBandsPercentage),
                15 => ObjectCompare.Compare(signalA.Rsi, signalB.Rsi),
                16 => ObjectCompare.Compare(signalA.StochOscillator, signalB.StochOscillator),
                17 => ObjectCompare.Compare(signalA.StochSignal, signalB.StochSignal),
                18 => ObjectCompare.Compare(signalA.Sma200, signalB.Sma200),
                19 => ObjectCompare.Compare(signalA.Sma50, signalB.Sma50),
                20 => ObjectCompare.Compare(signalA.Sma20, signalB.Sma20),
                21 => ObjectCompare.Compare(signalA.PSar, signalB.PSar),
                22 => ObjectCompare.Compare(signalA.FluxIndicator5m, signalB.FluxIndicator5m),
                23 => ObjectCompare.Compare(signalA.Symbol.FundingRate, signalB.Symbol.FundingRate),
                _ => 0
            };

            // Extra defaults (maar waarom omgedraaid?)
            if (compareResult == 0)
            {
                compareResult = ObjectCompare.Compare(signalA.CloseDate, signalB.CloseDate);
                if (compareResult == 0)
                {
                    if (SortOrder == SortOrder.Ascending)
                        compareResult = ObjectCompare.Compare(signalA.Symbol.Name, signalB.Symbol.Name);
                    else
                        compareResult = ObjectCompare.Compare(signalB.Symbol.Name, signalA.Symbol.Name);
                }
                if (compareResult == 0)
                {
                    if (SortOrder == SortOrder.Ascending)
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
    }

}
