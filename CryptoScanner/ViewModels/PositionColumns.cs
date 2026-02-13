using Avalonia.Controls;
using Avalonia.Layout;

using CryptoScanner.Core.Model;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

public enum PositionColumnEnum
{
    Id,
    AltradyId,
    CreateTime,
    UpdateTime,
    CloseTime,
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
    BreakEvenPrice,
    BreakEvenPercent,
    Quantity,
    Open,
    CurrentProfit,
    CurrentProfitPercentage,
    Parts,
    EntryPrice,
    ProfitPrice,
    FundingRate,
    QuantityTick,
    RemainingDust,
    RemainingDustValue,

    //Object information
    SignalDate,
    SignalPrice,
    SignalVolume,

    TrendInterval,
    TrendPercentagePrimary,
    TrendPercentageSecondary,
    Last24HoursChange,
    LastXDaysEffective,

    Bb,
    BbUpper,
    BbLower,
    AvgBB,

    Rsi,
    LuxIndicator5m,
    //SlopeRsi,
    MacdValue,
    MacdSignal,
    MacdHistogram,
    StochOscillator,
    StochSignal,
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

public class PositionColumnComparer : IGridComparer<CryptoPosition, PositionColumnEnum>
{
    public PositionColumnEnum SortColumn { get; set; }
    public ListSortDirection SortDirection { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public int Compare(CryptoPosition? a, CryptoPosition? b)
    {
        if (a == null || b == null)
            return 0;

        try
        {
            int compareResult = SortColumn switch
            {
                PositionColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                PositionColumnEnum.AltradyId => ObjectCompare.Compare(a.AltradyPositionId, b.AltradyPositionId),
                PositionColumnEnum.CreateTime => ObjectCompare.Compare(a.CreateTime, b.CreateTime),
                PositionColumnEnum.UpdateTime => ObjectCompare.Compare(a.UpdateTime, b.UpdateTime),
                PositionColumnEnum.CloseTime => ObjectCompare.Compare(a.CloseTime, b.CloseTime),
                PositionColumnEnum.Duration => ObjectCompare.Compare(a.Duration().TotalSeconds, b.Duration().TotalSeconds),
                PositionColumnEnum.Exchange => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
                PositionColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
                PositionColumnEnum.Interval => ObjectCompare.Compare(a.Interval!.IntervalPeriod, b.Interval!.IntervalPeriod),
                PositionColumnEnum.Strategy => ObjectCompare.Compare(a.StrategyText, b.StrategyText),
                PositionColumnEnum.Side => ObjectCompare.Compare(a.SideText, b.SideText),
                PositionColumnEnum.Status => ObjectCompare.Compare(a.Status, b.Status),
                PositionColumnEnum.Invested => ObjectCompare.Compare(a.Invested, b.Invested),
                PositionColumnEnum.Returned => ObjectCompare.Compare(a.Returned, b.Returned),
                PositionColumnEnum.Commission => ObjectCompare.Compare(a.Commission, b.Commission),
                PositionColumnEnum.BreakEvenPrice => ObjectCompare.Compare(a.BreakEvenPrice, b.BreakEvenPrice),
                PositionColumnEnum.Quantity => ObjectCompare.Compare(a.Quantity, b.Quantity),
                PositionColumnEnum.Open => ObjectCompare.Compare(a.Invested - a.Returned - a.Commission, b.Invested - b.Returned - b.Commission),
                PositionColumnEnum.CurrentProfit => ObjectCompare.Compare(a.CurrentProfit(), b.CurrentProfit()),
                PositionColumnEnum.BreakEvenPercent => ObjectCompare.Compare(a.CurrentBreakEvenPercentage(), b.CurrentBreakEvenPercentage()),
                PositionColumnEnum.Parts => ObjectCompare.Compare(a.PartCount, b.PartCount),
                PositionColumnEnum.EntryPrice => ObjectCompare.Compare(a.EntryPrice, b.EntryPrice),
                PositionColumnEnum.ProfitPrice => ObjectCompare.Compare(a.ProfitPrice, b.ProfitPrice),
                PositionColumnEnum.CurrentProfitPercentage => ObjectCompare.Compare(a.CurrentProfitPercentage(), b.CurrentProfitPercentage()),
                PositionColumnEnum.FundingRate => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
                PositionColumnEnum.QuantityTick => ObjectCompare.Compare(a.Symbol.QuantityTickSize, b.Symbol.QuantityTickSize),
                PositionColumnEnum.RemainingDust => ObjectCompare.Compare(a.RemainingDust, b.RemainingDust),
                PositionColumnEnum.RemainingDustValue => ObjectCompare.Compare(a.RemainingDust * a.Symbol.LastPrice, b.RemainingDust * b.Symbol.LastPrice),

                // Object information
                PositionColumnEnum.SignalDate => ObjectCompare.Compare(a.SignalEventTime, b.SignalEventTime),
                PositionColumnEnum.SignalPrice => ObjectCompare.Compare(a.SignalPrice, b.SignalPrice),
                PositionColumnEnum.SignalVolume => ObjectCompare.Compare(a.SignalVolume, b.SignalVolume),
                PositionColumnEnum.TrendInterval => ObjectCompare.Compare(a.TrendInterval, b.TrendInterval),
                PositionColumnEnum.TrendPercentagePrimary => ObjectCompare.Compare(a.TrendPercentagePrimary, b.TrendPercentagePrimary),
                PositionColumnEnum.TrendPercentageSecondary => ObjectCompare.Compare(a.TrendPercentageSecondary, b.TrendPercentageSecondary),
                PositionColumnEnum.Last24HoursChange => ObjectCompare.Compare(a.Last24HoursChange, b.Last24HoursChange),
                PositionColumnEnum.LastXDaysEffective => ObjectCompare.Compare(a.LastXDaysEffective, b.LastXDaysEffective),
                PositionColumnEnum.Bb => ObjectCompare.Compare(a.BollingerBandsPercentage, b.BollingerBandsPercentage),
                PositionColumnEnum.AvgBB => ObjectCompare.Compare(a.AvgBB, b.AvgBB),
                PositionColumnEnum.MacdValue => ObjectCompare.Compare(a.MacdValue, b.MacdValue),
                PositionColumnEnum.MacdSignal => ObjectCompare.Compare(a.MacdSignal, b.MacdSignal),
                PositionColumnEnum.MacdHistogram => ObjectCompare.Compare(a.MacdHistogram, b.MacdHistogram),
                PositionColumnEnum.Rsi => ObjectCompare.Compare(a.Rsi, b.Rsi),
                //LiveDataColumnEnum.SlopeRsi => ObjectCompare.Compare(a.SlopeRsi, b.SlopeRsi),
                PositionColumnEnum.StochOscillator => ObjectCompare.Compare(a.StochOscillator, b.StochOscillator),
                PositionColumnEnum.StochSignal => ObjectCompare.Compare(a.StochSignal, b.StochSignal),
                PositionColumnEnum.Sma200 => ObjectCompare.Compare(a.Sma200, b.Sma200),
                PositionColumnEnum.Sma50 => ObjectCompare.Compare(a.Sma50, b.Sma50),
                PositionColumnEnum.Sma20 => ObjectCompare.Compare(a.Sma20, b.Sma20),
                PositionColumnEnum.PSar => ObjectCompare.Compare(a.PSar, b.PSar),
                PositionColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.LuxIndicator5m, b.LuxIndicator5m),
                PositionColumnEnum.Trend15m => ObjectCompare.Compare(a.Trend15m, b.Trend15m),
                PositionColumnEnum.Trend30m => ObjectCompare.Compare(a.Trend30m, b.Trend30m),
                PositionColumnEnum.Trend1h => ObjectCompare.Compare(a.Trend1h, b.Trend1h),
                PositionColumnEnum.Trend4h => ObjectCompare.Compare(a.Trend4h, b.Trend4h),
                PositionColumnEnum.Trend1d => ObjectCompare.Compare(a.Trend1d, b.Trend1d),
                PositionColumnEnum.Barometer15m => ObjectCompare.Compare(a.Barometer15m, b.Barometer15m),
                PositionColumnEnum.Barometer30m => ObjectCompare.Compare(a.Barometer30m, b.Barometer30m),
                PositionColumnEnum.Barometer1h => ObjectCompare.Compare(a.Barometer1h, b.Barometer1h),
                PositionColumnEnum.Barometer4h => ObjectCompare.Compare(a.Barometer4h, b.Barometer4h),
                PositionColumnEnum.Barometer1d => ObjectCompare.Compare(a.Barometer1d, b.Barometer1d),
                PositionColumnEnum.MinimumEntry => ObjectCompare.Compare(a.MinEntry, b.MinEntry),
                PositionColumnEnum.PriceMin => ObjectCompare.Compare(a.PriceMin, b.PriceMin),
                PositionColumnEnum.PriceMax => ObjectCompare.Compare(a.PriceMax, b.PriceMax),
                PositionColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.PriceMinPerc, b.PriceMinPerc),
                PositionColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.PriceMaxPerc, b.PriceMaxPerc),
                _ => 0
            };


            // extend if still the same
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.CreateTime, b.CreateTime);
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name);
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Interval!.IntervalPeriod, b.Interval!.IntervalPeriod);


            //// Calculate correct return value based on object comparison
            //if (SortDirection == ListSortDirection.Ascending)
            //    return +compareResult;
            //else if (SortDirection == ListSortDirection.Descending)
            //    return -compareResult;
            //else
            //    return 0;

            if (SortDirection == ListSortDirection.Descending)
                return -compareResult;
            else
                return compareResult;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}

public static class PositionColumns
{
    public static ObservableCollection<GridColumnDefinition<PositionColumnEnum>> GetColumns()
    {
        var columns = new ObservableCollection<GridColumnDefinition<PositionColumnEnum>>
        {
            new() { ColumnEnum = PositionColumnEnum.Id, Header = "Id", Width = 60, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.AltradyId, Header = "AltradyId", Width = 110, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.CreateTime, Header = "Created", Width = 110, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = PositionColumnEnum.UpdateTime, Header = "Updated", Width = 110, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = PositionColumnEnum.CloseTime, Header = "Closed", Width = 110, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = PositionColumnEnum.Duration, Header = "Duration", Width = 90, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = PositionColumnEnum.Exchange, Header = "Exchange", Width = 125, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.Symbol, Header = "Symbol", Width = 100, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = PositionColumnEnum.Side, Header = "Side", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Interval, Header = "Interval", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Strategy, Header = "Strategy", Width = 60, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = PositionColumnEnum.Status, Header = "Status", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Invested, Header = "Invested", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Returned, Header = "Returned", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Commission, Header = "Commission", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Quantity, Header = "Quantity", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Open, Header = "Open", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.CurrentProfit, Header = "Profit", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.CurrentProfitPercentage, Header = "Percentage", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Parts, Header = "Parts", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.EntryPrice, Header = "Entry", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.BreakEvenPrice, Header = "Break Even", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.BreakEvenPercent, Header = "Break Even%", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.ProfitPrice, Header = "Profit Price", Width = 75, Alignment = HorizontalAlignment.Right},
            //new() { ColumnEnum = PositionColumnEnum.CurrentPrice, Header = "Current", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.SignalPrice, Header = "Signal", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.SignalVolume, Header = "Volume", Width = 100, Alignment = HorizontalAlignment.Right},
            //new() { ColumnEnum = PositionColumnEnum.PriceChange, Header = "Change", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.TrendPercentagePrimary, Header = "Trend % 1", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.TrendPercentageSecondary, Header = "Trend % 2", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Last24HoursChange, Header = "24h Change", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.LastXDaysEffective, Header = "X Days", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.AvgBB, Header = "Avg BB", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Bb, Header = "BB%", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.BbUpper, Header = "BB Upper", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.BbLower, Header = "BB Lower", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Rsi, Header = "RSI", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.LuxIndicator5m, Header = "Lux 5m", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.MacdValue, Header = "MACD", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.MacdSignal, Header = "MACD Sig", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.MacdHistogram, Header = "MACD Hist", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.StochOscillator, Header = "Stoch", Width = 80, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.StochSignal, Header = "Stoch Sig", Width = 80, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Sma200, Header = "Sma200", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Sma50, Header = "Sma50", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Sma20, Header = "Sma20", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.PSar, Header = "PSar", Width = 75, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = PositionColumnEnum.Trend15m, Header = "Trend 15m", Width = 60, Alignment = HorizontalAlignment.Center, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.Trend30m, Header = "Trend 30m", Width = 60, Alignment = HorizontalAlignment.Center, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.Trend1h, Header = "Trend 1h", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Trend4h, Header = "Trend 4h", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Trend1d, Header = "Trend 1d", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Barometer15m, Header = "Bm 15m", Width = 60, Alignment = HorizontalAlignment.Center, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.Barometer30m, Header = "Bm 30m", Width = 60, Alignment = HorizontalAlignment.Center, IsVisible=false},
            new() { ColumnEnum = PositionColumnEnum.Barometer1h, Header = "Bm 1h", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Barometer4h, Header = "Bm 4h", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.Barometer1d, Header = "Bm 1d", Width = 60, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = PositionColumnEnum.MinimumEntry, Header = "Min Entry", Width = 60, Alignment = HorizontalAlignment.Right},
        };

        // Initialize DisplayIndex
        int index = 0;
        foreach (var column in columns)
        {
            column.ActualWidth = new GridLength(column.Width);
            column.DisplayIndex = index;
            index++;
        }
        return columns;
    }
}