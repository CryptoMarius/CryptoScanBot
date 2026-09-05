using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections;

namespace CryptoScanner.ViewModels;


public class PositionColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private PositionColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public PositionColumnComparer(PositionColumnEnum? sortColumn)
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
                    PositionColumnEnum.Id => ObjectCompare.Compare(a.Object.Id, b.Object.Id),
                    PositionColumnEnum.AltradyId => ObjectCompare.Compare(a.Object.AltradyPositionId, b.Object.AltradyPositionId),
                    PositionColumnEnum.CreateTime => ObjectCompare.Compare(a.Object.CreateTime, b.Object.CreateTime),
                    PositionColumnEnum.UpdateTime => ObjectCompare.Compare(a.Object.UpdateTime, b.Object.UpdateTime),
                    PositionColumnEnum.CloseTime => ObjectCompare.Compare(a.Object.CloseTime, b.Object.CloseTime),
                    PositionColumnEnum.Duration => ObjectCompare.Compare(a.Object.Duration().TotalSeconds, b.Object.Duration().TotalSeconds),
                    PositionColumnEnum.Exchange => ObjectCompare.Compare(a.Object.Exchange.Name, b.Object.Exchange.Name),
                    PositionColumnEnum.Symbol => ObjectCompare.Compare(a.Object.Symbol.Name, b.Object.Symbol.Name),
                    PositionColumnEnum.Interval => ObjectCompare.Compare(a.Object.Interval!.IntervalPeriod, b.Object.Interval!.IntervalPeriod),
                    PositionColumnEnum.Strategy => ObjectCompare.Compare(a.Object.StrategyText, b.Object.StrategyText),
                    PositionColumnEnum.Side => ObjectCompare.Compare(a.Object.SideText, b.Object.SideText),
                    PositionColumnEnum.Status => ObjectCompare.Compare(a.Object.Status, b.Object.Status),
                    PositionColumnEnum.Invested => ObjectCompare.Compare(a.Object.Invested, b.Object.Invested),
                    PositionColumnEnum.Returned => ObjectCompare.Compare(a.Object.Returned, b.Object.Returned),
                    PositionColumnEnum.Commission => ObjectCompare.Compare(a.Object.Commission, b.Object.Commission),
                    PositionColumnEnum.BreakEvenPrice => ObjectCompare.Compare(a.Object.BreakEvenPrice, b.Object.BreakEvenPrice),
                    PositionColumnEnum.Quantity => ObjectCompare.Compare(a.Object.Quantity, b.Object.Quantity),
                    PositionColumnEnum.Open => ObjectCompare.Compare(a.Object.Invested - a.Object.Returned - a.Object.Commission, b.Object.Invested - b.Object.Returned - b.Object.Commission),
                    PositionColumnEnum.CurrentProfit => ObjectCompare.Compare(a.Object.CurrentProfit(), b.Object.CurrentProfit()),
                    PositionColumnEnum.BreakEvenPercent => ObjectCompare.Compare(a.Object.CurrentBreakEvenPercentage(), b.Object.CurrentBreakEvenPercentage()),
                    PositionColumnEnum.Parts => ObjectCompare.Compare(a.Object.PartCount, b.Object.PartCount),
                    PositionColumnEnum.EntryPrice => ObjectCompare.Compare(a.Object.EntryPrice, b.Object.EntryPrice),
                    PositionColumnEnum.ProfitPrice => ObjectCompare.Compare(a.Object.ProfitPrice, b.Object.ProfitPrice),
                    PositionColumnEnum.CurrentProfitPercentage => ObjectCompare.Compare(a.Object.CurrentProfitPercentage(), b.Object.CurrentProfitPercentage()),
                    PositionColumnEnum.FundingRate => ObjectCompare.Compare(a.Object.Symbol.FundingRate, b.Object.Symbol.FundingRate),
                    PositionColumnEnum.QuantityTick => ObjectCompare.Compare(a.Object.Symbol.QuantityTickSize, b.Object.Symbol.QuantityTickSize),
                    PositionColumnEnum.RemainingDust => ObjectCompare.Compare(a.Object.RemainingDust, b.Object.RemainingDust),
                    PositionColumnEnum.RemainingDustValue => ObjectCompare.Compare(a.Object.RemainingDust * a.Object.Symbol.LastPrice, b.Object.RemainingDust * b.Object.Symbol.LastPrice),

                    // Object information
                    PositionColumnEnum.SignalDate => ObjectCompare.Compare(a.Object.SignalEventTime, b.Object.SignalEventTime),
                    PositionColumnEnum.SignalPrice => ObjectCompare.Compare(a.Object.SignalPrice, b.Object.SignalPrice),
                    PositionColumnEnum.SignalVolume => ObjectCompare.Compare(a.Object.SignalVolume, b.Object.SignalVolume),
                    PositionColumnEnum.TrendInterval => ObjectCompare.Compare(a.Object.TrendInterval, b.Object.TrendInterval),
                    PositionColumnEnum.TrendPercentagePrimary => ObjectCompare.Compare(a.Object.TrendPercentagePrimary, b.Object.TrendPercentagePrimary),
                    PositionColumnEnum.TrendPercentageSecondary => ObjectCompare.Compare(a.Object.TrendPercentageSecondary, b.Object.TrendPercentageSecondary),
                    PositionColumnEnum.Last24HoursChange => ObjectCompare.Compare(a.Object.Last24HoursChange, b.Object.Last24HoursChange),
                    PositionColumnEnum.LastXDaysEffective => ObjectCompare.Compare(a.Object.LastXDaysEffective, b.Object.LastXDaysEffective),
                    PositionColumnEnum.BB => ObjectCompare.Compare(a.Object.BollingerBandsPercentage, b.Object.BollingerBandsPercentage),
                    PositionColumnEnum.AvgBB => ObjectCompare.Compare(a.Object.AvgBB, b.Object.AvgBB),
                    PositionColumnEnum.MacdValue => ObjectCompare.Compare(a.Object.MacdValue, b.Object.MacdValue),
                    PositionColumnEnum.MacdSignal => ObjectCompare.Compare(a.Object.MacdSignal, b.Object.MacdSignal),
                    PositionColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Object.MacdHistogram, b.Object.MacdHistogram),
                    PositionColumnEnum.Rsi => ObjectCompare.Compare(a.Object.Rsi, b.Object.Rsi),
                    //LiveDataColumnEnum.SlopeRsi => ObjectCompare.Compare(a.Object.SlopeRsi, b.Object.SlopeRsi),
                    PositionColumnEnum.StochOscillator => ObjectCompare.Compare(a.Object.StochOscillator, b.Object.StochOscillator),
                    PositionColumnEnum.StochSignal => ObjectCompare.Compare(a.Object.StochSignal, b.Object.StochSignal),
                    PositionColumnEnum.Sma200 => ObjectCompare.Compare(a.Object.Sma200, b.Object.Sma200),
                    PositionColumnEnum.Sma50 => ObjectCompare.Compare(a.Object.Sma50, b.Object.Sma50),
                    PositionColumnEnum.Sma20 => ObjectCompare.Compare(a.Object.Sma20, b.Object.Sma20),
                    PositionColumnEnum.PSar => ObjectCompare.Compare(a.Object.PSar, b.Object.PSar),
                    PositionColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.Object.LuxIndicator5m, b.Object.LuxIndicator5m),
                    PositionColumnEnum.Trend15m => ObjectCompare.Compare(a.Object.Trend15m, b.Object.Trend15m),
                    PositionColumnEnum.Trend30m => ObjectCompare.Compare(a.Object.Trend30m, b.Object.Trend30m),
                    PositionColumnEnum.Trend1h => ObjectCompare.Compare(a.Object.Trend1h, b.Object.Trend1h),
                    PositionColumnEnum.Trend4h => ObjectCompare.Compare(a.Object.Trend4h, b.Object.Trend4h),
                    PositionColumnEnum.Trend1d => ObjectCompare.Compare(a.Object.Trend1d, b.Object.Trend1d),
                    PositionColumnEnum.Barometer15m => ObjectCompare.Compare(a.Object.Barometer15m, b.Object.Barometer15m),
                    PositionColumnEnum.Barometer30m => ObjectCompare.Compare(a.Object.Barometer30m, b.Object.Barometer30m),
                    PositionColumnEnum.Barometer1h => ObjectCompare.Compare(a.Object.Barometer1h, b.Object.Barometer1h),
                    PositionColumnEnum.Barometer4h => ObjectCompare.Compare(a.Object.Barometer4h, b.Object.Barometer4h),
                    PositionColumnEnum.Barometer1d => ObjectCompare.Compare(a.Object.Barometer1d, b.Object.Barometer1d),
                    PositionColumnEnum.MinimumEntry => ObjectCompare.Compare(a.Object.MinEntry, b.Object.MinEntry),
                    //PositionColumnEnum.PriceMin => ObjectCompare.Compare(a.Object.PriceMin, b.Object.PriceMin),
                    //PositionColumnEnum.PriceMax => ObjectCompare.Compare(a.Object.PriceMax, b.Object.PriceMax),
                    //PositionColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.Object.PriceMinPerc, b.Object.PriceMinPerc),
                    //PositionColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.Object.PriceMaxPerc, b.Object.PriceMaxPerc),
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