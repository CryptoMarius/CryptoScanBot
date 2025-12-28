using CryptoScanner.Signal.Model;

using System.Collections;

namespace CryptoScanner.Signal.Common;

public enum GridSortDirection
{
    Ascending,
    Descending,
}

public enum GridColumnEnum
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

    MarketTrendPrimary,
    MarketTrendSecondary,
    Change24h,
    MoveXDaysEffective,

    BB,
    BbUpper,
    BbLower,
    AvgBB,

    Rsi,
    Lux5m,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    Stoch,
    Signal,
    Sma200,
    Sma50,
    Sma20,
    PSar,

    Trend15m,
    Trend30m,
    Trend1h,
    Trend4h,
    Trend1d,

    Barometer15m,
    Barometer30m,
    Barometer1h,
    Barometer4h,
    Barometer1d,

    MinimumEntry,
    PriceMinPerc,
    PriceMaxPerc,
    SignalStatus,

    // BBMA properties, but the strategy isn't working properly yet
    //#if StrategyBbma
    //        Wma05Low,
    //        Wma05High,
    //        Wma10Low,
    //        Wma10High,
    //#endif
}

public class SignalColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private GridColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public SignalColumnComparer(GridColumnEnum? sortColumn)
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
                    GridColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                    GridColumnEnum.Date => ObjectCompare.Compare(a.SignalObject.CloseDate, b.SignalObject.CloseDate),
                    GridColumnEnum.Exchange => ObjectCompare.Compare(a.SignalObject.Exchange.Name, b.SignalObject.Exchange.Name),
                    GridColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol, b.Symbol),
                    GridColumnEnum.Side => ObjectCompare.Compare(a.SignalObject.Side, b.SignalObject.Side),
                    GridColumnEnum.Interval => ObjectCompare.Compare(a.SignalObject.Interval.Name, b.SignalObject.Interval.Name),
                    GridColumnEnum.Strategy => ObjectCompare.Compare(a.Strategy, b.Strategy),
                    GridColumnEnum.Text => ObjectCompare.Compare(a.SignalObject.EventText, b.SignalObject.EventText),
                    GridColumnEnum.SignalPrice => ObjectCompare.Compare(a.SignalObject.SignalPrice, b.SignalObject.SignalPrice),
                    GridColumnEnum.PriceChange => ObjectCompare.Compare(a.SignalObject.Last24HoursChange, b.SignalObject.Last24HoursChange),
                    GridColumnEnum.SignalVolume => ObjectCompare.Compare(a.SignalObject.SignalVolume, b.SignalObject.SignalVolume),
                    GridColumnEnum.TfTrend => ObjectCompare.Compare(a.SignalObject.TrendInterval, b.SignalObject.TrendInterval),
                    GridColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(a.SignalObject.TrendPercentagePrimary, b.SignalObject.TrendPercentagePrimary),
                    GridColumnEnum.MarketTrendSecondary => ObjectCompare.Compare(a.SignalObject.TrendPercentageSecondary, b.SignalObject.TrendPercentageSecondary),
                    GridColumnEnum.Change24h => ObjectCompare.Compare(a.SignalObject.Last24HoursChange, b.SignalObject.Last24HoursChange),
                    GridColumnEnum.MoveXDaysEffective => ObjectCompare.Compare(a.SignalObject.LastXDaysEffective, b.SignalObject.LastXDaysEffective),
                    GridColumnEnum.BB => ObjectCompare.Compare(a.SignalObject.BollingerBandsPercentage, b.SignalObject.BollingerBandsPercentage),
                    GridColumnEnum.BbLower => ObjectCompare.Compare(a.SignalObject.BollingerBandsLowerBand, b.SignalObject.BollingerBandsLowerBand),
                    GridColumnEnum.BbUpper => ObjectCompare.Compare(a.SignalObject.BollingerBandsUpperBand, b.SignalObject.BollingerBandsUpperBand),
                    GridColumnEnum.AvgBB => ObjectCompare.Compare(a.SignalObject.AvgBB, b.SignalObject.AvgBB),
                    GridColumnEnum.Rsi => ObjectCompare.Compare(a.SignalObject.Rsi, b.SignalObject.Rsi),
                    GridColumnEnum.Lux5m => ObjectCompare.Compare(a.SignalObject.LuxIndicator5m, b.SignalObject.LuxIndicator5m),
                    GridColumnEnum.MacdValue => ObjectCompare.Compare(a.SignalObject.MacdValue, b.SignalObject.MacdValue),
                    GridColumnEnum.MacdSignal => ObjectCompare.Compare(a.SignalObject.MacdSignal, b.SignalObject.MacdSignal),
                    GridColumnEnum.MacdHistogram => ObjectCompare.Compare(a.SignalObject.MacdHistogram, b.SignalObject.MacdHistogram),
                    GridColumnEnum.Stoch => ObjectCompare.Compare(a.SignalObject.StochOscillator, b.SignalObject.StochOscillator),
                    GridColumnEnum.Signal => ObjectCompare.Compare(a.SignalObject.StochSignal, b.SignalObject.StochSignal),
                    GridColumnEnum.Sma200 => ObjectCompare.Compare(a.SignalObject.Sma200, b.SignalObject.Sma200),
                    GridColumnEnum.Sma50 => ObjectCompare.Compare(a.SignalObject.Sma50, b.SignalObject.Sma50),
                    GridColumnEnum.Sma20 => ObjectCompare.Compare(a.SignalObject.Sma20, b.SignalObject.Sma20),
                    GridColumnEnum.PSar => ObjectCompare.Compare(a.SignalObject.PSar, b.SignalObject.PSar),
                    GridColumnEnum.Trend15m => ObjectCompare.Compare(a.SignalObject.Trend15m, b.SignalObject.Trend15m),
                    GridColumnEnum.Trend30m => ObjectCompare.Compare(a.SignalObject.Trend30m, b.SignalObject.Trend30m),
                    GridColumnEnum.Trend1h => ObjectCompare.Compare(a.SignalObject.Trend1h, b.SignalObject.Trend1h),
                    GridColumnEnum.Trend4h => ObjectCompare.Compare(a.SignalObject.Trend4h, b.SignalObject.Trend4h),
                    GridColumnEnum.Trend1d => ObjectCompare.Compare(a.SignalObject.Trend1d, b.SignalObject.Trend1d),
                    GridColumnEnum.Barometer15m => ObjectCompare.Compare(a.SignalObject.Barometer15m, b.SignalObject.Barometer15m),
                    GridColumnEnum.Barometer30m => ObjectCompare.Compare(a.SignalObject.Barometer30m, b.SignalObject.Barometer30m),
                    GridColumnEnum.Barometer1h => ObjectCompare.Compare(a.SignalObject.Barometer1h, b.SignalObject.Barometer1h),
                    GridColumnEnum.Barometer4h => ObjectCompare.Compare(a.SignalObject.Barometer4h, b.SignalObject.Barometer4h),
                    GridColumnEnum.Barometer1d => ObjectCompare.Compare(a.SignalObject.Barometer1d, b.SignalObject.Barometer1d),
                    GridColumnEnum.MinimumEntry => ObjectCompare.Compare(a.SignalObject.MinEntry, b.SignalObject.MinEntry),
                    GridColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.SignalObject.PriceMinPerc, b.SignalObject.PriceMinPerc),
                    GridColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.SignalObject.PriceMaxPerc, b.SignalObject.PriceMaxPerc),
                    GridColumnEnum.SignalStatus => ObjectCompare.Compare(a.SignalObject.SignalStatus, b.SignalObject.SignalStatus),
                    _ => 0
                };

                // Sort on some more columns...
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);
                if (compareResult == 0)
                    compareResult = -ObjectCompare.Compare(a.SignalObject.Interval.Duration, b.SignalObject.Interval.Duration);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.SignalObject.StrategyText, b.SignalObject.StrategyText);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.SignalObject.CloseDate, b.SignalObject.CloseDate);

                return compareResult;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        return 0;
    }
}
