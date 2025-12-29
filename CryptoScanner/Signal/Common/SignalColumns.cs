using CryptoScanner.Signal.Model;

using System.Collections;

namespace CryptoScanner.Signal.Common;

public enum SignalColumnEnum
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
    private SignalColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public SignalColumnComparer(SignalColumnEnum? sortColumn)
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
                    SignalColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                    SignalColumnEnum.Date => ObjectCompare.Compare(a.SignalObject.CloseDate, b.SignalObject.CloseDate),
                    SignalColumnEnum.Exchange => ObjectCompare.Compare(a.SignalObject.Exchange.Name, b.SignalObject.Exchange.Name),
                    SignalColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol, b.Symbol),
                    SignalColumnEnum.Side => ObjectCompare.Compare(a.SignalObject.Side, b.SignalObject.Side),
                    SignalColumnEnum.Interval => ObjectCompare.Compare(a.SignalObject.Interval.Name, b.SignalObject.Interval.Name),
                    SignalColumnEnum.Strategy => ObjectCompare.Compare(a.Strategy, b.Strategy),
                    SignalColumnEnum.Text => ObjectCompare.Compare(a.SignalObject.EventText, b.SignalObject.EventText),
                    SignalColumnEnum.SignalPrice => ObjectCompare.Compare(a.SignalObject.SignalPrice, b.SignalObject.SignalPrice),
                    SignalColumnEnum.PriceChange => ObjectCompare.Compare(a.SignalObject.Last24HoursChange, b.SignalObject.Last24HoursChange),
                    SignalColumnEnum.SignalVolume => ObjectCompare.Compare(a.SignalObject.SignalVolume, b.SignalObject.SignalVolume),
                    SignalColumnEnum.TfTrend => ObjectCompare.Compare(a.SignalObject.TrendInterval, b.SignalObject.TrendInterval),
                    SignalColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(a.SignalObject.TrendPercentagePrimary, b.SignalObject.TrendPercentagePrimary),
                    SignalColumnEnum.MarketTrendSecondary => ObjectCompare.Compare(a.SignalObject.TrendPercentageSecondary, b.SignalObject.TrendPercentageSecondary),
                    SignalColumnEnum.Change24h => ObjectCompare.Compare(a.SignalObject.Last24HoursChange, b.SignalObject.Last24HoursChange),
                    SignalColumnEnum.MoveXDaysEffective => ObjectCompare.Compare(a.SignalObject.LastXDaysEffective, b.SignalObject.LastXDaysEffective),
                    SignalColumnEnum.BB => ObjectCompare.Compare(a.SignalObject.BollingerBandsPercentage, b.SignalObject.BollingerBandsPercentage),
                    SignalColumnEnum.BbLower => ObjectCompare.Compare(a.SignalObject.BollingerBandsLowerBand, b.SignalObject.BollingerBandsLowerBand),
                    SignalColumnEnum.BbUpper => ObjectCompare.Compare(a.SignalObject.BollingerBandsUpperBand, b.SignalObject.BollingerBandsUpperBand),
                    SignalColumnEnum.AvgBB => ObjectCompare.Compare(a.SignalObject.AvgBB, b.SignalObject.AvgBB),
                    SignalColumnEnum.Rsi => ObjectCompare.Compare(a.SignalObject.Rsi, b.SignalObject.Rsi),
                    SignalColumnEnum.Lux5m => ObjectCompare.Compare(a.SignalObject.LuxIndicator5m, b.SignalObject.LuxIndicator5m),
                    SignalColumnEnum.MacdValue => ObjectCompare.Compare(a.SignalObject.MacdValue, b.SignalObject.MacdValue),
                    SignalColumnEnum.MacdSignal => ObjectCompare.Compare(a.SignalObject.MacdSignal, b.SignalObject.MacdSignal),
                    SignalColumnEnum.MacdHistogram => ObjectCompare.Compare(a.SignalObject.MacdHistogram, b.SignalObject.MacdHistogram),
                    SignalColumnEnum.Stoch => ObjectCompare.Compare(a.SignalObject.StochOscillator, b.SignalObject.StochOscillator),
                    SignalColumnEnum.Signal => ObjectCompare.Compare(a.SignalObject.StochSignal, b.SignalObject.StochSignal),
                    SignalColumnEnum.Sma200 => ObjectCompare.Compare(a.SignalObject.Sma200, b.SignalObject.Sma200),
                    SignalColumnEnum.Sma50 => ObjectCompare.Compare(a.SignalObject.Sma50, b.SignalObject.Sma50),
                    SignalColumnEnum.Sma20 => ObjectCompare.Compare(a.SignalObject.Sma20, b.SignalObject.Sma20),
                    SignalColumnEnum.PSar => ObjectCompare.Compare(a.SignalObject.PSar, b.SignalObject.PSar),
                    SignalColumnEnum.Trend15m => ObjectCompare.Compare(a.SignalObject.Trend15m, b.SignalObject.Trend15m),
                    SignalColumnEnum.Trend30m => ObjectCompare.Compare(a.SignalObject.Trend30m, b.SignalObject.Trend30m),
                    SignalColumnEnum.Trend1h => ObjectCompare.Compare(a.SignalObject.Trend1h, b.SignalObject.Trend1h),
                    SignalColumnEnum.Trend4h => ObjectCompare.Compare(a.SignalObject.Trend4h, b.SignalObject.Trend4h),
                    SignalColumnEnum.Trend1d => ObjectCompare.Compare(a.SignalObject.Trend1d, b.SignalObject.Trend1d),
                    SignalColumnEnum.Barometer15m => ObjectCompare.Compare(a.SignalObject.Barometer15m, b.SignalObject.Barometer15m),
                    SignalColumnEnum.Barometer30m => ObjectCompare.Compare(a.SignalObject.Barometer30m, b.SignalObject.Barometer30m),
                    SignalColumnEnum.Barometer1h => ObjectCompare.Compare(a.SignalObject.Barometer1h, b.SignalObject.Barometer1h),
                    SignalColumnEnum.Barometer4h => ObjectCompare.Compare(a.SignalObject.Barometer4h, b.SignalObject.Barometer4h),
                    SignalColumnEnum.Barometer1d => ObjectCompare.Compare(a.SignalObject.Barometer1d, b.SignalObject.Barometer1d),
                    SignalColumnEnum.MinimumEntry => ObjectCompare.Compare(a.SignalObject.MinEntry, b.SignalObject.MinEntry),
                    SignalColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.SignalObject.PriceMinPerc, b.SignalObject.PriceMinPerc),
                    SignalColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.SignalObject.PriceMaxPerc, b.SignalObject.PriceMaxPerc),
                    SignalColumnEnum.SignalStatus => ObjectCompare.Compare(a.SignalObject.SignalStatus, b.SignalObject.SignalStatus),
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
