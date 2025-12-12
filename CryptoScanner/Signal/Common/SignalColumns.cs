using Avalonia.Layout;

using CryptoScanner.Signal.Model;

using System;
using System.Collections;

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

// Helper class for column configuration
public class SignalColumnDefinition
{
    public ColumnEnum Column { get; set; }
    public string Caption { get; set; } = string.Empty;
    public Type Type { get; set; } = typeof(string);
    public HorizontalAlignment Align { get; set; } = HorizontalAlignment.Left;

    // Other attributes
    public int Index { get; set; }
    public int Width { get; set; }
    public bool Visible { get; set; } = true;
    public string Format { get; set; } = string.Empty;
}

public class SignalColumnDefinitions
{
    private static readonly CaseInsensitiveComparer ObjectCompare = new();

    public SignalColumnDefinition? SortColumn { get; set; }
    public GridSortDirection? SortDirection { get; set; }

    //    if (SortDirection == null)
    //        SortDirection = GridSortDirection.Ascending;
    //    //if (SortColumn == null || (int)SortDirection > Enum.GetNames<ColumnEnum>().Length)
    //    //    SortColumn = ColumnEnum.Symbol;

    public int Compare(SignalInfo a, SignalInfo b)
    {
        if (SortColumn == null || SortDirection == null)
            return 0;

        try
        {
            int compareResult = SortColumn.Column switch
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
                // todo,, the rest of the Columns
                _ => 0
            };


            // secondary sort
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.SignalObject.Interval.Duration, b.SignalObject.Interval.Duration);


            // Calculate correct return value based on object comparison
            if (SortDirection == GridSortDirection.Ascending)
                return +compareResult;
            else if (SortDirection == GridSortDirection.Descending)
                return -compareResult;
            else
                return 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
