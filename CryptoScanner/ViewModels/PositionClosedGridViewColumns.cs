using CryptoScanner.Core.Model;

using System.Collections;

namespace CryptoScanner.ViewModels;

public enum PositionClosedColumnEnum
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



public class PositionClosedColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private PositionClosedColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public PositionClosedColumnComparer(PositionClosedColumnEnum? sortColumn)
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
                    PositionClosedColumnEnum.Id => ObjectCompare.Compare(a.Object.Id, b.Object.Id),
                    PositionClosedColumnEnum.AltradyId => ObjectCompare.Compare(a.Object.AltradyPositionId, b.Object.AltradyPositionId),
                    PositionClosedColumnEnum.Created => ObjectCompare.Compare(a.Object.CreateTime, b.Object.CreateTime),
                    PositionClosedColumnEnum.Updated => ObjectCompare.Compare(a.Object.UpdateTime, b.Object.UpdateTime),
                    PositionClosedColumnEnum.Duration => ObjectCompare.Compare(a.Object.Duration().TotalSeconds, b.Object.Duration().TotalSeconds),
                    PositionClosedColumnEnum.Exchange => ObjectCompare.Compare(a.Object.Exchange.Name, b.Object.Exchange.Name),
                    PositionClosedColumnEnum.Symbol => ObjectCompare.Compare(a.Object.Symbol.Name, b.Object.Symbol.Name),
                    PositionClosedColumnEnum.Interval => ObjectCompare.Compare(a.Object.Interval!.IntervalPeriod, b.Object.Interval!.IntervalPeriod),
                    PositionClosedColumnEnum.Strategy => ObjectCompare.Compare(a.Object.StrategyText, b.Object.StrategyText),
                    PositionClosedColumnEnum.Side => ObjectCompare.Compare(a.Object.SideText, b.Object.SideText),
                    PositionClosedColumnEnum.Status => ObjectCompare.Compare(a.Object.Status, b.Object.Status),
                    PositionClosedColumnEnum.Invested => ObjectCompare.Compare(a.Object.Invested, b.Object.Invested),
                    PositionClosedColumnEnum.Returned => ObjectCompare.Compare(a.Object.Returned, b.Object.Returned),
                    PositionClosedColumnEnum.Commission => ObjectCompare.Compare(a.Object.Commission, b.Object.Commission),
                    PositionClosedColumnEnum.BreakEven => ObjectCompare.Compare(a.Object.BreakEvenPrice, b.Object.BreakEvenPrice),
                    PositionClosedColumnEnum.Quantity => ObjectCompare.Compare(a.Object.Quantity, b.Object.Quantity),
                    PositionClosedColumnEnum.Open => ObjectCompare.Compare(a.Object.Invested - a.Object.Returned - a.Object.Commission, b.Object.Invested - b.Object.Returned - b.Object.Commission),
                    PositionClosedColumnEnum.Profit => ObjectCompare.Compare(a.Object.CurrentProfit(), b.Object.CurrentProfit()),
                    PositionClosedColumnEnum.BreakEvenPercent => ObjectCompare.Compare(a.Object.CurrentBreakEvenPercentage(), b.Object.CurrentBreakEvenPercentage()),
                    PositionClosedColumnEnum.Parts => ObjectCompare.Compare(a.Object.PartCount, b.Object.PartCount),
                    PositionClosedColumnEnum.EntryPrice => ObjectCompare.Compare(a.Object.EntryPrice, b.Object.EntryPrice),
                    PositionClosedColumnEnum.ProfitPrice => ObjectCompare.Compare(a.Object.ProfitPrice, b.Object.ProfitPrice),
                    PositionClosedColumnEnum.Percentage => ObjectCompare.Compare(a.Object.CurrentProfitPercentage(), b.Object.CurrentProfitPercentage()),
                    PositionClosedColumnEnum.FundingRate => ObjectCompare.Compare(a.Object.Symbol.FundingRate, b.Object.Symbol.FundingRate),
                    PositionClosedColumnEnum.QuantityTick => ObjectCompare.Compare(a.Object.Symbol.QuantityTickSize, b.Object.Symbol.QuantityTickSize),
                    PositionClosedColumnEnum.RemainingDust => ObjectCompare.Compare(a.Object.RemainingDust, b.Object.RemainingDust),
                    PositionClosedColumnEnum.DustValue => ObjectCompare.Compare(a.Object.RemainingDust * a.Object.Symbol.LastPrice, b.Object.RemainingDust * b.Object.Symbol.LastPrice),

                    // Object information
                    PositionClosedColumnEnum.SignalDate => ObjectCompare.Compare(a.Object.SignalEventTime, b.Object.SignalEventTime),
                    PositionClosedColumnEnum.SignalPrice => ObjectCompare.Compare(a.Object.SignalPrice, b.Object.SignalPrice),
                    PositionClosedColumnEnum.SignalVolume => ObjectCompare.Compare(a.Object.SignalVolume, b.Object.SignalVolume),
                    PositionClosedColumnEnum.TfTrend => ObjectCompare.Compare(a.Object.TrendInterval, b.Object.TrendInterval),
                    PositionClosedColumnEnum.MarketTrendPrimary => ObjectCompare.Compare(a.Object.TrendPercentagePrimary, b.Object.TrendPercentagePrimary),
                    PositionClosedColumnEnum.MarketTrendSecondary => ObjectCompare.Compare(a.Object.TrendPercentageSecondary, b.Object.TrendPercentageSecondary),
                    PositionClosedColumnEnum.Change24h => ObjectCompare.Compare(a.Object.Last24HoursChange, b.Object.Last24HoursChange),
                    PositionClosedColumnEnum.MoveLastXDaysEffective => ObjectCompare.Compare(a.Object.LastXDaysEffective, b.Object.LastXDaysEffective),
                    PositionClosedColumnEnum.BB => ObjectCompare.Compare(a.Object.BollingerBandsPercentage, b.Object.BollingerBandsPercentage),
                    PositionClosedColumnEnum.AvgBB => ObjectCompare.Compare(a.Object.AvgBB, b.Object.AvgBB),
                    PositionClosedColumnEnum.MacdValue => ObjectCompare.Compare(a.Object.MacdValue, b.Object.MacdValue),
                    PositionClosedColumnEnum.MacdSignal => ObjectCompare.Compare(a.Object.MacdSignal, b.Object.MacdSignal),
                    PositionClosedColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Object.MacdHistogram, b.Object.MacdHistogram),
                    PositionClosedColumnEnum.Rsi => ObjectCompare.Compare(a.Object.Rsi, b.Object.Rsi),
                    //LiveDataColumnEnum.SlopeRsi => ObjectCompare.Compare(a.Object.SlopeRsi, b.Object.SlopeRsi),
                    PositionClosedColumnEnum.Stoch => ObjectCompare.Compare(a.Object.StochOscillator, b.Object.StochOscillator),
                    PositionClosedColumnEnum.Signal => ObjectCompare.Compare(a.Object.StochSignal, b.Object.StochSignal),
                    PositionClosedColumnEnum.Sma200 => ObjectCompare.Compare(a.Object.Sma200, b.Object.Sma200),
                    PositionClosedColumnEnum.Sma50 => ObjectCompare.Compare(a.Object.Sma50, b.Object.Sma50),
                    PositionClosedColumnEnum.Sma20 => ObjectCompare.Compare(a.Object.Sma20, b.Object.Sma20),
                    PositionClosedColumnEnum.PSar => ObjectCompare.Compare(a.Object.PSar, b.Object.PSar),
                    PositionClosedColumnEnum.Lux5m => ObjectCompare.Compare(a.Object.LuxIndicator5m, b.Object.LuxIndicator5m),
                    PositionClosedColumnEnum.Trend15m => ObjectCompare.Compare(a.Object.Trend15m, b.Object.Trend15m),
                    PositionClosedColumnEnum.Trend30m => ObjectCompare.Compare(a.Object.Trend30m, b.Object.Trend30m),
                    PositionClosedColumnEnum.Trend1h => ObjectCompare.Compare(a.Object.Trend1h, b.Object.Trend1h),
                    PositionClosedColumnEnum.Trend4h => ObjectCompare.Compare(a.Object.Trend4h, b.Object.Trend4h),
                    PositionClosedColumnEnum.Trend1d => ObjectCompare.Compare(a.Object.Trend1d, b.Object.Trend1d),
                    PositionClosedColumnEnum.Barometer15m => ObjectCompare.Compare(a.Object.Barometer15m, b.Object.Barometer15m),
                    PositionClosedColumnEnum.Barometer30m => ObjectCompare.Compare(a.Object.Barometer30m, b.Object.Barometer30m),
                    PositionClosedColumnEnum.Barometer1h => ObjectCompare.Compare(a.Object.Barometer1h, b.Object.Barometer1h),
                    PositionClosedColumnEnum.Barometer4h => ObjectCompare.Compare(a.Object.Barometer4h, b.Object.Barometer4h),
                    PositionClosedColumnEnum.Barometer1d => ObjectCompare.Compare(a.Object.Barometer1d, b.Object.Barometer1d),
                    PositionClosedColumnEnum.MinimumEntry => ObjectCompare.Compare(a.Object.MinEntry, b.Object.MinEntry),
                    PositionClosedColumnEnum.PriceMin => ObjectCompare.Compare(a.Object.PriceMin, b.Object.PriceMin),
                    PositionClosedColumnEnum.PriceMax => ObjectCompare.Compare(a.Object.PriceMax, b.Object.PriceMax),
                    PositionClosedColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.Object.PriceMinPerc, b.Object.PriceMinPerc),
                    PositionClosedColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.Object.PriceMaxPerc, b.Object.PriceMaxPerc),
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