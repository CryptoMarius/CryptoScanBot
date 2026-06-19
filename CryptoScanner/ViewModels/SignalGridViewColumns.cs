using System.Collections;

namespace CryptoScanner.ViewModels;

public enum SignalColumnEnum
{
    Id,
    Date,
    Exchange,
    Symbol,
    Side,
    Interval,
    Strategy,
    EventText,
    SignalPrice,
    PriceChange,
    SignalVolume,

    TrendInterval,
    TrendPercentagePrimary,
    TrendPercentageSecondary,
    Last24HoursChange,
    LastXDaysEffective,

    BB,
    BbUpper,
    BbLower,
    AvgBB,

    Rsi,
    LuxIndicator5m,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    StochOscillator,
    StochSignal,
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
    //PriceMinPerc,
    //PriceMaxPerc,
    //SignalStatus,

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
    private SignalColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public SignalColumnComparer(SignalColumnEnum? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is SignalViewModel a && y is SignalViewModel b)
        {
            try
            {
                int compareResult = SortColumn switch
                {
                    SignalColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                    SignalColumnEnum.Date => ObjectCompare.Compare(a.Object.CloseDate, b.Object.CloseDate),
                    SignalColumnEnum.Exchange => ObjectCompare.Compare(a.Object.Exchange.Name, b.Object.Exchange.Name),
                    SignalColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol, b.Symbol),
                    SignalColumnEnum.Side => ObjectCompare.Compare(a.Object.Side, b.Object.Side),
                    SignalColumnEnum.Interval => ObjectCompare.Compare(a.Object.Interval.Name, b.Object.Interval.Name),
                    SignalColumnEnum.Strategy => ObjectCompare.Compare(a.Strategy, b.Strategy),
                    SignalColumnEnum.EventText => ObjectCompare.Compare(a.Object.EventText, b.Object.EventText),
                    SignalColumnEnum.SignalPrice => ObjectCompare.Compare(a.Object.SignalPrice, b.Object.SignalPrice),
                    SignalColumnEnum.PriceChange => ObjectCompare.Compare(a.Object.Last24HoursChange, b.Object.Last24HoursChange),
                    SignalColumnEnum.SignalVolume => ObjectCompare.Compare(a.Object.SignalVolume, b.Object.SignalVolume),
                    SignalColumnEnum.TrendInterval => ObjectCompare.Compare(a.Object.TrendInterval, b.Object.TrendInterval),
                    SignalColumnEnum.TrendPercentagePrimary => ObjectCompare.Compare(a.Object.TrendPercentagePrimary, b.Object.TrendPercentagePrimary),
                    SignalColumnEnum.TrendPercentageSecondary => ObjectCompare.Compare(a.Object.TrendPercentageSecondary, b.Object.TrendPercentageSecondary),
                    SignalColumnEnum.Last24HoursChange => ObjectCompare.Compare(a.Object.Last24HoursChange, b.Object.Last24HoursChange),
                    SignalColumnEnum.LastXDaysEffective => ObjectCompare.Compare(a.Object.LastXDaysEffective, b.Object.LastXDaysEffective),
                    SignalColumnEnum.BB => ObjectCompare.Compare(a.Object.BollingerBandsPercentage, b.Object.BollingerBandsPercentage),
                    SignalColumnEnum.BbLower => ObjectCompare.Compare(a.Object.BollingerBandsLowerBand, b.Object.BollingerBandsLowerBand),
                    SignalColumnEnum.BbUpper => ObjectCompare.Compare(a.Object.BollingerBandsUpperBand, b.Object.BollingerBandsUpperBand),
                    SignalColumnEnum.AvgBB => ObjectCompare.Compare(a.Object.AvgBB, b.Object.AvgBB),
                    SignalColumnEnum.Rsi => ObjectCompare.Compare(a.Object.Rsi, b.Object.Rsi),
                    SignalColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.Object.LuxIndicator5m, b.Object.LuxIndicator5m),
                    SignalColumnEnum.MacdValue => ObjectCompare.Compare(a.Object.MacdValue, b.Object.MacdValue),
                    SignalColumnEnum.MacdSignal => ObjectCompare.Compare(a.Object.MacdSignal, b.Object.MacdSignal),
                    SignalColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Object.MacdHistogram, b.Object.MacdHistogram),
                    SignalColumnEnum.StochOscillator => ObjectCompare.Compare(a.Object.StochOscillator, b.Object.StochOscillator),
                    SignalColumnEnum.StochSignal => ObjectCompare.Compare(a.Object.StochSignal, b.Object.StochSignal),
                    SignalColumnEnum.Sma200 => ObjectCompare.Compare(a.Object.Sma200, b.Object.Sma200),
                    SignalColumnEnum.Sma50 => ObjectCompare.Compare(a.Object.Sma50, b.Object.Sma50),
                    SignalColumnEnum.Sma20 => ObjectCompare.Compare(a.Object.Sma20, b.Object.Sma20),
                    SignalColumnEnum.PSar => ObjectCompare.Compare(a.Object.PSar, b.Object.PSar),
                    SignalColumnEnum.Trend15m => ObjectCompare.Compare(a.Object.Trend15m, b.Object.Trend15m),
                    SignalColumnEnum.Trend30m => ObjectCompare.Compare(a.Object.Trend30m, b.Object.Trend30m),
                    SignalColumnEnum.Trend1h => ObjectCompare.Compare(a.Object.Trend1h, b.Object.Trend1h),
                    SignalColumnEnum.Trend4h => ObjectCompare.Compare(a.Object.Trend4h, b.Object.Trend4h),
                    SignalColumnEnum.Trend1d => ObjectCompare.Compare(a.Object.Trend1d, b.Object.Trend1d),
                    SignalColumnEnum.Barometer15m => ObjectCompare.Compare(a.Object.Barometer15m, b.Object.Barometer15m),
                    SignalColumnEnum.Barometer30m => ObjectCompare.Compare(a.Object.Barometer30m, b.Object.Barometer30m),
                    SignalColumnEnum.Barometer1h => ObjectCompare.Compare(a.Object.Barometer1h, b.Object.Barometer1h),
                    SignalColumnEnum.Barometer4h => ObjectCompare.Compare(a.Object.Barometer4h, b.Object.Barometer4h),
                    SignalColumnEnum.Barometer1d => ObjectCompare.Compare(a.Object.Barometer1d, b.Object.Barometer1d),
                    SignalColumnEnum.MinimumEntry => ObjectCompare.Compare(a.Object.MinEntry, b.Object.MinEntry),
                    //SignalColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.Object.PriceMinPerc, b.Object.PriceMinPerc),
                    //SignalColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.Object.PriceMaxPerc, b.Object.PriceMaxPerc),
                    //SignalColumnEnum.SignalStatus => ObjectCompare.Compare(a.Object.SignalStatus, b.Object.SignalStatus),
                    _ => 0
                };

                // Sort on some more columns...
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);
                if (compareResult == 0)
                    compareResult = -ObjectCompare.Compare(a.Object.Interval.Duration, b.Object.Interval.Duration);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Object.StrategyText, b.Object.StrategyText);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Object.CloseDate, b.Object.CloseDate);

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
