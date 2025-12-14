using Avalonia.Layout;

using CryptoScanner.Signal.Model;

using System;
using System.Collections;
using System.ComponentModel;

namespace CryptoScanner.Signal.Common;

public enum GridSortDirection
{
    Ascending,
    Descending,
}

public enum ColumnEnum
{
    Id,
    Date,
    Exchange,
    Symbol,
    Side,
    Interval,
    Strategy,
    Text,
    SignalPrice,
    PriceChange,
    SignalVolume,
    TfTrend,

//    MarketTrendPrimary,
//    MarketTrendSecondary,
//    Change24h,
//    MoveXDaysEffective,
//    BB,
//    BbUpper,
//    BbLower,
//    AvgBB,
//    Rsi,
//    //RsiSurface,
//    Lux5m,
//    //SlopeRsi,
//    MacdValue,
//    MacdSignal,
//    MacdHistogram,
//    Stoch,
//    Signal,
//    //StochSurface,
//    Sma200,
//    Sma50,
//    Sma20,
//    PSar,
//    FundingRate,

//    Trend15m,
//    Trend30m,
//    Trend1h,
//    Trend4h,
//    Trend1d,

//    Barometer15m,
//    Barometer30m,
//    Barometer1h,
//    Barometer4h,
//    Barometer1d,

//    MinimumEntry,
//    // statistics
//    PriceMinPerc,
//    PriceMaxPerc,
//#if DEBUG
//    SignalStatus,
//#endif
//#if StrategyBbma
//        // Debug
//        Wma05Low,
//        Wma05High,
//        Wma10Low,
//        Wma10High,
//#endif
}

public class SignalColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private ColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public SignalColumnComparer(ColumnEnum? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is SignalInfo a && y is SignalInfo b)
        {
            try
            {
                int compareResult = SortColumn switch
                {
                    ColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                    ColumnEnum.Date => ObjectCompare.Compare(a.SignalObject.CloseDate, b.SignalObject.CloseDate),
                    ColumnEnum.Exchange => ObjectCompare.Compare(a.SignalObject.Exchange.Name, b.SignalObject.Exchange.Name),
                    ColumnEnum.Side => ObjectCompare.Compare(a.SignalObject.Side, b.SignalObject.Side),
                    ColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol, b.Symbol),
                    ColumnEnum.Text => ObjectCompare.Compare(a.SignalObject.EventText, b.SignalObject.EventText),
                    ColumnEnum.Interval => ObjectCompare.Compare(a.SignalObject.Interval.Name, b.SignalObject.Interval.Name),
                    ColumnEnum.Strategy => ObjectCompare.Compare(a.Strategy, b.Strategy),
                    ColumnEnum.SignalVolume => ObjectCompare.Compare(a.SignalObject.SignalVolume, b.SignalObject.SignalVolume),
                    ColumnEnum.SignalPrice => ObjectCompare.Compare(a.SignalObject.SignalPrice, b.SignalObject.SignalPrice),
                    ColumnEnum.PriceChange => ObjectCompare.Compare(a.SignalObject.Last24HoursChange, b.SignalObject.Last24HoursChange),
                    ColumnEnum.TfTrend => ObjectCompare.Compare(a.SignalObject.TrendInterval, b.SignalObject.TrendInterval),
                    // todo,, the rest of the Columns (lots....)
                    _ => 0
                };


                // Sort on more columns because signal for example signal date needs much more!
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.SignalObject.Interval.Duration, b.SignalObject.Interval.Duration);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.SignalObject.StrategyText, b.SignalObject.StrategyText);

                return compareResult;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        else 
            return 0;
    }
}
