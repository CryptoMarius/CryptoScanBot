using CryptoScanner.Signal.Model;

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
    // FundingRate, only for trading futures

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
                    ColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(a.SignalObject.TrendPercentagePrimary, b.SignalObject.TrendPercentagePrimary),
                    ColumnEnum.MarketTrendSecondary => ObjectCompare.Compare(a.SignalObject.TrendPercentageSecondary, b.SignalObject.TrendPercentageSecondary),
                    ColumnEnum.Change24h => ObjectCompare.Compare(a.SignalObject.Last24HoursChange, b.SignalObject.Last24HoursChange),
                    ColumnEnum.MoveXDaysEffective => ObjectCompare.Compare(a.SignalObject.LastXDaysEffective, b.SignalObject.LastXDaysEffective),
                    ColumnEnum.Lux5m => ObjectCompare.Compare(a.SignalObject.LuxIndicator5m, b.SignalObject.LuxIndicator5m),

                    ColumnEnum.MacdValue => ObjectCompare.Compare(a.SignalObject.MacdValue, b.SignalObject.MacdValue),
                    ColumnEnum.MacdSignal => ObjectCompare.Compare(a.SignalObject.MacdSignal, b.SignalObject.MacdSignal),
                    ColumnEnum.MacdHistogram => ObjectCompare.Compare(a.SignalObject.MacdHistogram, b.SignalObject.MacdHistogram),
                    ColumnEnum.Stoch => ObjectCompare.Compare(a.SignalObject.StochOscillator, b.SignalObject.StochOscillator),
                    ColumnEnum.Signal => ObjectCompare.Compare(a.SignalObject.StochSignal, b.SignalObject.StochSignal),
                    ColumnEnum.Sma200 => ObjectCompare.Compare(a.SignalObject.Sma200, b.SignalObject.Sma200),
                    ColumnEnum.Sma50 => ObjectCompare.Compare(a.SignalObject.Sma50, b.SignalObject.Sma50),
                    ColumnEnum.Sma20 => ObjectCompare.Compare(a.SignalObject.Sma20, b.SignalObject.Sma20),
                    ColumnEnum.PSar => ObjectCompare.Compare(a.SignalObject.PSar, b.SignalObject.PSar),

                    ColumnEnum.Rsi => ObjectCompare.Compare(a.SignalObject.Rsi, b.SignalObject.Rsi),
                    ColumnEnum.BB => ObjectCompare.Compare(a.SignalObject.BollingerBandsPercentage, b.SignalObject.BollingerBandsPercentage),
                    ColumnEnum.BbLower => ObjectCompare.Compare(a.SignalObject.BollingerBandsLowerBand, b.SignalObject.BollingerBandsLowerBand),
                    ColumnEnum.BbUpper => ObjectCompare.Compare(a.SignalObject.BollingerBandsUpperBand, b.SignalObject.BollingerBandsUpperBand),
                    ColumnEnum.AvgBB => ObjectCompare.Compare(a.SignalObject.AvgBB, b.SignalObject.AvgBB),
                    
                    ColumnEnum.Trend15m => ObjectCompare.Compare(a.SignalObject.Trend15m, b.SignalObject.Trend15m),
                    ColumnEnum.Trend30m => ObjectCompare.Compare(a.SignalObject.Trend30m, b.SignalObject.Trend30m),
                    ColumnEnum.Trend1h => ObjectCompare.Compare(a.SignalObject.Trend1h, b.SignalObject.Trend1h),
                    ColumnEnum.Trend4h => ObjectCompare.Compare(a.SignalObject.Trend4h, b.SignalObject.Trend4h),
                    ColumnEnum.Trend1d => ObjectCompare.Compare(a.SignalObject.Trend1d, b.SignalObject.Trend1d),

                    ColumnEnum.Barometer15m => ObjectCompare.Compare(a.SignalObject.Barometer15m, b.SignalObject.Barometer15m),
                    ColumnEnum.Barometer30m => ObjectCompare.Compare(a.SignalObject.Barometer30m, b.SignalObject.Barometer30m),
                    ColumnEnum.Barometer1h => ObjectCompare.Compare(a.SignalObject.Barometer1h, b.SignalObject.Barometer1h),
                    ColumnEnum.Barometer4h => ObjectCompare.Compare(a.SignalObject.Barometer4h, b.SignalObject.Barometer4h),
                    ColumnEnum.Barometer1d => ObjectCompare.Compare(a.SignalObject.Barometer1d, b.SignalObject.Barometer1d),

                    ColumnEnum.MinimumEntry => ObjectCompare.Compare(a.SignalObject.MinEntry, b.SignalObject.MinEntry),

                    ColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.SignalObject.PriceMinPerc, b.SignalObject.PriceMinPerc),
                    ColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.SignalObject.PriceMaxPerc, b.SignalObject.PriceMaxPerc),
                    ColumnEnum.SignalStatus => ObjectCompare.Compare(a.SignalObject.SignalStatus, b.SignalObject.SignalStatus),

                    _ => 0
                };


                // Sort on more columns because signal for example signal date needs much more!
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);
                if (compareResult == 0)
                    compareResult = -ObjectCompare.Compare(a.SignalObject.Interval.Duration, b.SignalObject.Interval.Duration);
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
