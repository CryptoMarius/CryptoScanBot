using CryptoScanner.Core.Model;

using System.Collections;

namespace CryptoScanner.ViewModels;

public enum PositionOpenColumnEnum
{
    Id,
    AltradyId,
    Created,
    Updated,
    Duration,
    Exchange,
    Symbol,
    Interval,
    Side,
    Strategy,
    Status,
    Invested,
    Returned,
    Commission,
    BreakEven,
    Quantity,
    Open,
    Profit,
    Percentage,
    BreakEvenPercent,
    Parts,
    EntryPrice,
    ProfitPrice,
    FundingRate,
    QuantityTick,
    RemainingDust,
    DustValue,

    //Object information
    SignalDate,
    SignalPrice,
    SignalVolume,
    TfTrend,
    MarketTrendPrimary,
    MarketTrendSecondary,
    Change24h,
    MoveLastXDaysEffective,

    BB,
    BbUpper,
    BbLower,
    AvgBB,

    Rsi,
    Lux5m,
    //SlopeRsi,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    Stoch,
    Signal,
    Sma200,
    Sma50,
    Sma20,
    PSar,

    //FundingRate,
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
    // statistics
    PriceMin,
    PriceMax,
    PriceMinPerc,
    PriceMaxPerc,

}



public class PositionOpenColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private PositionOpenColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public PositionOpenColumnComparer(PositionOpenColumnEnum? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is PositionViewModel a && y is PositionViewModel b)
        {

            try
            {
                int compareResult = SortColumn switch
                {
                    PositionOpenColumnEnum.Id => ObjectCompare.Compare(a.Object.Id, b.Object.Id),
                    PositionOpenColumnEnum.AltradyId => ObjectCompare.Compare(a.Object.AltradyPositionId, b.Object.AltradyPositionId),
                    PositionOpenColumnEnum.Created => ObjectCompare.Compare(a.Object.CreateTime, b.Object.CreateTime),
                    PositionOpenColumnEnum.Updated => ObjectCompare.Compare(a.Object.UpdateTime, b.Object.UpdateTime),
                    PositionOpenColumnEnum.Duration => ObjectCompare.Compare(a.Object.Duration().TotalSeconds, b.Object.Duration().TotalSeconds),
                    PositionOpenColumnEnum.Exchange => ObjectCompare.Compare(a.Object.Exchange.Name, b.Object.Exchange.Name),
                    PositionOpenColumnEnum.Symbol => ObjectCompare.Compare(a.Object.Symbol.Name, b.Object.Symbol.Name),
                    PositionOpenColumnEnum.Interval => ObjectCompare.Compare(a.Object.Interval!.IntervalPeriod, b.Object.Interval!.IntervalPeriod),
                    PositionOpenColumnEnum.Strategy => ObjectCompare.Compare(a.Object.StrategyText, b.Object.StrategyText),
                    PositionOpenColumnEnum.Side => ObjectCompare.Compare(a.Object.SideText, b.Object.SideText),
                    PositionOpenColumnEnum.Status => ObjectCompare.Compare(a.Object.Status, b.Object.Status),
                    PositionOpenColumnEnum.Invested => ObjectCompare.Compare(a.Object.Invested, b.Object.Invested),
                    PositionOpenColumnEnum.Returned => ObjectCompare.Compare(a.Object.Returned, b.Object.Returned),
                    PositionOpenColumnEnum.Commission => ObjectCompare.Compare(a.Object.Commission, b.Object.Commission),
                    PositionOpenColumnEnum.BreakEven => ObjectCompare.Compare(a.Object.BreakEvenPrice, b.Object.BreakEvenPrice),
                    PositionOpenColumnEnum.Quantity => ObjectCompare.Compare(a.Object.Quantity, b.Object.Quantity),
                    PositionOpenColumnEnum.Open => ObjectCompare.Compare(a.Object.Invested - a.Object.Returned - a.Object.Commission, b.Object.Invested - b.Object.Returned - b.Object.Commission),
                    PositionOpenColumnEnum.Profit => ObjectCompare.Compare(a.Object.CurrentProfit(), b.Object.CurrentProfit()),
                    PositionOpenColumnEnum.BreakEvenPercent => ObjectCompare.Compare(a.Object.CurrentBreakEvenPercentage(), b.Object.CurrentBreakEvenPercentage()),
                    PositionOpenColumnEnum.Parts => ObjectCompare.Compare(a.Object.PartCount, b.Object.PartCount),
                    PositionOpenColumnEnum.EntryPrice => ObjectCompare.Compare(a.Object.EntryPrice, b.Object.EntryPrice),
                    PositionOpenColumnEnum.ProfitPrice => ObjectCompare.Compare(a.Object.ProfitPrice, b.Object.ProfitPrice),
                    PositionOpenColumnEnum.Percentage => ObjectCompare.Compare(a.Object.CurrentProfitPercentage(), b.Object.CurrentProfitPercentage()),
                    PositionOpenColumnEnum.FundingRate => ObjectCompare.Compare(a.Object.Symbol.FundingRate, b.Object.Symbol.FundingRate),
                    PositionOpenColumnEnum.QuantityTick => ObjectCompare.Compare(a.Object.Symbol.QuantityTickSize, b.Object.Symbol.QuantityTickSize),
                    PositionOpenColumnEnum.RemainingDust => ObjectCompare.Compare(a.Object.RemainingDust, b.Object.RemainingDust),
                    PositionOpenColumnEnum.DustValue => ObjectCompare.Compare(a.Object.RemainingDust * a.Object.Symbol.LastPrice, b.Object.RemainingDust * b.Object.Symbol.LastPrice),

                    // Object information
                    PositionOpenColumnEnum.SignalDate => ObjectCompare.Compare(a.Object.SignalEventTime, b.Object.SignalEventTime),
                    PositionOpenColumnEnum.SignalPrice => ObjectCompare.Compare(a.Object.SignalPrice, b.Object.SignalPrice),
                    PositionOpenColumnEnum.SignalVolume => ObjectCompare.Compare(a.Object.SignalVolume, b.Object.SignalVolume),
                    PositionOpenColumnEnum.TfTrend => ObjectCompare.Compare(a.Object.TrendInterval, b.Object.TrendInterval),
                    PositionOpenColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(a.Object.TrendPercentagePrimary, b.Object.TrendPercentagePrimary),
                    PositionOpenColumnEnum.MarketTrendSecondary => ObjectCompare.Compare(a.Object.TrendPercentageSecondary, b.Object.TrendPercentageSecondary),
                    PositionOpenColumnEnum.Change24h => ObjectCompare.Compare(a.Object.Last24HoursChange, b.Object.Last24HoursChange),
                    PositionOpenColumnEnum.MoveLastXDaysEffective => ObjectCompare.Compare(a.Object.LastXDaysEffective, b.Object.LastXDaysEffective),
                    PositionOpenColumnEnum.BB => ObjectCompare.Compare(a.Object.BollingerBandsPercentage, b.Object.BollingerBandsPercentage),
                    PositionOpenColumnEnum.AvgBB => ObjectCompare.Compare(a.Object.AvgBB, b.Object.AvgBB),
                    PositionOpenColumnEnum.MacdValue => ObjectCompare.Compare(a.Object.MacdValue, b.Object.MacdValue),
                    PositionOpenColumnEnum.MacdSignal => ObjectCompare.Compare(a.Object.MacdSignal, b.Object.MacdSignal),
                    PositionOpenColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Object.MacdHistogram, b.Object.MacdHistogram),
                    PositionOpenColumnEnum.Rsi => ObjectCompare.Compare(a.Object.Rsi, b.Object.Rsi),
                    //LiveDataColumnEnum.SlopeRsi => ObjectCompare.Compare(a.Object.SlopeRsi, b.Object.SlopeRsi),
                    PositionOpenColumnEnum.Stoch => ObjectCompare.Compare(a.Object.StochOscillator, b.Object.StochOscillator),
                    PositionOpenColumnEnum.Signal => ObjectCompare.Compare(a.Object.StochSignal, b.Object.StochSignal),
                    PositionOpenColumnEnum.Sma200 => ObjectCompare.Compare(a.Object.Sma200, b.Object.Sma200),
                    PositionOpenColumnEnum.Sma50 => ObjectCompare.Compare(a.Object.Sma50, b.Object.Sma50),
                    PositionOpenColumnEnum.Sma20 => ObjectCompare.Compare(a.Object.Sma20, b.Object.Sma20),
                    PositionOpenColumnEnum.PSar => ObjectCompare.Compare(a.Object.PSar, b.Object.PSar),
                    PositionOpenColumnEnum.Lux5m => ObjectCompare.Compare(a.Object.LuxIndicator5m, b.Object.LuxIndicator5m),
                    PositionOpenColumnEnum.Trend15m => ObjectCompare.Compare(a.Object.Trend15m, b.Object.Trend15m),
                    PositionOpenColumnEnum.Trend30m => ObjectCompare.Compare(a.Object.Trend30m, b.Object.Trend30m),
                    PositionOpenColumnEnum.Trend1h => ObjectCompare.Compare(a.Object.Trend1h, b.Object.Trend1h),
                    PositionOpenColumnEnum.Trend4h => ObjectCompare.Compare(a.Object.Trend4h, b.Object.Trend4h),
                    PositionOpenColumnEnum.Trend1d => ObjectCompare.Compare(a.Object.Trend1d, b.Object.Trend1d),
                    PositionOpenColumnEnum.Barometer15m => ObjectCompare.Compare(a.Object.Barometer15m, b.Object.Barometer15m),
                    PositionOpenColumnEnum.Barometer30m => ObjectCompare.Compare(a.Object.Barometer30m, b.Object.Barometer30m),
                    PositionOpenColumnEnum.Barometer1h => ObjectCompare.Compare(a.Object.Barometer1h, b.Object.Barometer1h),
                    PositionOpenColumnEnum.Barometer4h => ObjectCompare.Compare(a.Object.Barometer4h, b.Object.Barometer4h),
                    PositionOpenColumnEnum.Barometer1d => ObjectCompare.Compare(a.Object.Barometer1d, b.Object.Barometer1d),
                    PositionOpenColumnEnum.MinimumEntry => ObjectCompare.Compare(a.Object.MinEntry, b.Object.MinEntry),
                    PositionOpenColumnEnum.PriceMin => ObjectCompare.Compare(a.Object.PriceMin, b.Object.PriceMin),
                    PositionOpenColumnEnum.PriceMax => ObjectCompare.Compare(a.Object.PriceMax, b.Object.PriceMax),
                    PositionOpenColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.Object.PriceMinPerc, b.Object.PriceMinPerc),
                    PositionOpenColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.Object.PriceMaxPerc, b.Object.PriceMaxPerc),
                    _ => 0
                };


                // extend if still the same
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Object.CreateTime, b.Object.CreateTime);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Object.Symbol.Name, b.Object.Symbol.Name);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Object.Interval!.IntervalPeriod, b.Object.Interval!.IntervalPeriod);


                //// Calculate correct return value based on object comparison
                //if (SortDirection == ListSortDirection.Ascending)
                //    return +compareResult;
                //else if (SortDirection == ListSortDirection.Descending)
                //    return -compareResult;
                //else
                //    return 0;

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