using Avalonia.Controls;
using Avalonia.Layout;

using CryptoScanner.Core.Model;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
    Event,
    SignalPrice,
    PriceChange,
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

public class SignalColumnComparer : IGridComparer<CryptoSignal, SignalColumnEnum>
{
    public SignalColumnEnum SortColumn { get; set; }
    public ListSortDirection SortDirection { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public int Compare(CryptoSignal? a, CryptoSignal? b)
    {
        if (a == null || b == null)
            return 0;

        try
        {
            int compareResult = SortColumn switch
            {
                SignalColumnEnum.Id => ObjectCompare.Compare(a.Id, b.Id),
                SignalColumnEnum.Date => ObjectCompare.Compare(a.CloseDate, b.CloseDate),
                SignalColumnEnum.Exchange => ObjectCompare.Compare(a.Exchange.Name, b.Exchange.Name),
                SignalColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
                SignalColumnEnum.Side => ObjectCompare.Compare(a.Side, b.Side),
                SignalColumnEnum.Interval => ObjectCompare.Compare(a.Interval.Name, b.Interval.Name),
                SignalColumnEnum.Strategy => ObjectCompare.Compare(a.Strategy, b.Strategy),
                SignalColumnEnum.Event => ObjectCompare.Compare(a.EventText, b.EventText),
                SignalColumnEnum.SignalPrice => ObjectCompare.Compare(a.SignalPrice, b.SignalPrice),
                SignalColumnEnum.PriceChange => ObjectCompare.Compare(a.Last24HoursChange, b.Last24HoursChange),
                SignalColumnEnum.SignalVolume => ObjectCompare.Compare(a.SignalVolume, b.SignalVolume),
                SignalColumnEnum.TrendInterval => ObjectCompare.Compare(a.TrendInterval, b.TrendInterval),
                SignalColumnEnum.TrendPercentagePrimary => ObjectCompare.Compare(a.TrendPercentagePrimary, b.TrendPercentagePrimary),
                SignalColumnEnum.TrendPercentageSecondary => ObjectCompare.Compare(a.TrendPercentageSecondary, b.TrendPercentageSecondary),
                SignalColumnEnum.Last24HoursChange => ObjectCompare.Compare(a.Last24HoursChange, b.Last24HoursChange),
                SignalColumnEnum.LastXDaysEffective => ObjectCompare.Compare(a.LastXDaysEffective, b.LastXDaysEffective),
                SignalColumnEnum.Bb => ObjectCompare.Compare(a.BollingerBandsPercentage, b.BollingerBandsPercentage),
                SignalColumnEnum.BbLower => ObjectCompare.Compare(a.BollingerBandsLowerBand, b.BollingerBandsLowerBand),
                SignalColumnEnum.BbUpper => ObjectCompare.Compare(a.BollingerBandsUpperBand, b.BollingerBandsUpperBand),
                SignalColumnEnum.AvgBB => ObjectCompare.Compare(a.AvgBB, b.AvgBB),
                SignalColumnEnum.Rsi => ObjectCompare.Compare(a.Rsi, b.Rsi),
                SignalColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.LuxIndicator5m, b.LuxIndicator5m),
                SignalColumnEnum.MacdValue => ObjectCompare.Compare(a.MacdValue, b.MacdValue),
                SignalColumnEnum.MacdSignal => ObjectCompare.Compare(a.MacdSignal, b.MacdSignal),
                SignalColumnEnum.MacdHistogram => ObjectCompare.Compare(a.MacdHistogram, b.MacdHistogram),
                SignalColumnEnum.StochOscillator => ObjectCompare.Compare(a.StochOscillator, b.StochOscillator),
                SignalColumnEnum.StochSignal => ObjectCompare.Compare(a.StochSignal, b.StochSignal),
                SignalColumnEnum.Sma200 => ObjectCompare.Compare(a.Sma200, b.Sma200),
                SignalColumnEnum.Sma50 => ObjectCompare.Compare(a.Sma50, b.Sma50),
                SignalColumnEnum.Sma20 => ObjectCompare.Compare(a.Sma20, b.Sma20),
                SignalColumnEnum.PSar => ObjectCompare.Compare(a.PSar, b.PSar),
                SignalColumnEnum.Trend15m => ObjectCompare.Compare(a.Trend15m, b.Trend15m),
                SignalColumnEnum.Trend30m => ObjectCompare.Compare(a.Trend30m, b.Trend30m),
                SignalColumnEnum.Trend1h => ObjectCompare.Compare(a.Trend1h, b.Trend1h),
                SignalColumnEnum.Trend4h => ObjectCompare.Compare(a.Trend4h, b.Trend4h),
                SignalColumnEnum.Trend1d => ObjectCompare.Compare(a.Trend1d, b.Trend1d),
                SignalColumnEnum.Barometer15m => ObjectCompare.Compare(a.Barometer15m, b.Barometer15m),
                SignalColumnEnum.Barometer30m => ObjectCompare.Compare(a.Barometer30m, b.Barometer30m),
                SignalColumnEnum.Barometer1h => ObjectCompare.Compare(a.Barometer1h, b.Barometer1h),
                SignalColumnEnum.Barometer4h => ObjectCompare.Compare(a.Barometer4h, b.Barometer4h),
                SignalColumnEnum.Barometer1d => ObjectCompare.Compare(a.Barometer1d, b.Barometer1d),
                SignalColumnEnum.MinimumEntry => ObjectCompare.Compare(a.MinEntry, b.MinEntry),
                SignalColumnEnum.PriceMinPerc => ObjectCompare.Compare(a.PriceMinPerc, b.PriceMinPerc),
                SignalColumnEnum.PriceMaxPerc => ObjectCompare.Compare(a.PriceMaxPerc, b.PriceMaxPerc),
                SignalColumnEnum.SignalStatus => ObjectCompare.Compare(a.SignalStatus, b.SignalStatus),
                _ => 0
            };

            // Sort on some more columns...
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name);
            if (compareResult == 0)
                compareResult = -ObjectCompare.Compare(a.Interval.Duration, b.Interval.Duration);
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.StrategyText, b.StrategyText);
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.CloseDate, b.CloseDate);

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


public static class SignalColumns
{
    public static ObservableCollection<GridColumnDefinition<SignalColumnEnum>> GetColumns()
    {
        var columns = new ObservableCollection<GridColumnDefinition<SignalColumnEnum>>
        {
            new() { ColumnEnum = SignalColumnEnum.Id, Header = "Id", Width = 50, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Date, Header = "Date", Width = 180, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SignalColumnEnum.Exchange, Header = "Exchange", Width = 120, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SignalColumnEnum.Symbol, Header = "Symbol", Width = 100, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SignalColumnEnum.Side, Header = "Side", Width = 50, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SignalColumnEnum.Interval, Header = "Interval", Width = 60, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SignalColumnEnum.Strategy, Header = "Strategy", Width = 80, Alignment = HorizontalAlignment.Left},
            new() { ColumnEnum = SignalColumnEnum.Event , Header = "Event", Width = 80, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.SignalPrice, Header = "Price", Width = 80, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.PriceChange, Header = "Change", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.SignalVolume, Header = "Volume", Width = 125, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.TrendInterval, Header = "Trend Int", Width = 70, Alignment = HorizontalAlignment.Center},
            new() { ColumnEnum = SignalColumnEnum.TrendPercentagePrimary, Header = "Trend % 1", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.TrendPercentageSecondary, Header = "Trend % 2", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.Last24HoursChange, Header = "24h Change", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.LastXDaysEffective, Header = "X Days", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.Bb, Header = "BB%", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.BbUpper, Header = "BB Upper", Width = 80, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.BbLower, Header = "BB Lower", Width = 80, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.AvgBB, Header = "Avg BB", Width = 60, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Rsi, Header = "RSI", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.LuxIndicator5m, Header = "Lux 5m", Width = 60, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.MacdValue, Header = "MACD", Width = 80, Alignment = HorizontalAlignment.Right, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.MacdSignal, Header = "MACD Sig", Width = 80, Alignment = HorizontalAlignment.Right, IsVisible = false},
            new() { ColumnEnum = SignalColumnEnum.MacdHistogram, Header = "MACD Hist", Width = 80, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.StochOscillator, Header = "Stoch", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.StochSignal, Header = "Stoch Sig", Width = 60, Alignment = HorizontalAlignment.Right, IsVisible = false},
            new() { ColumnEnum = SignalColumnEnum.Sma200, Header = "SMA200", Width = 80, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.Sma50, Header = "SMA50", Width = 80, Alignment = HorizontalAlignment.Right, IsVisible = false},
            new() { ColumnEnum = SignalColumnEnum.Sma20, Header = "SMA20", Width = 80, Alignment = HorizontalAlignment.Right, IsVisible = false},
            new() { ColumnEnum = SignalColumnEnum.PSar, Header = "PSAR", Width = 80, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.Trend15m, Header = "Trend 15m", Width = 60, Alignment = HorizontalAlignment.Left, IsVisible = false},
            new() { ColumnEnum = SignalColumnEnum.Trend30m, Header = "Trend 30m", Width = 60, Alignment = HorizontalAlignment.Left, IsVisible = false},
            new() { ColumnEnum = SignalColumnEnum.Trend1h, Header = "Trend 1h", Width = 60, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Trend4h, Header = "Trend 4h", Width = 60, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Trend1d, Header = "Trend 1d", Width = 60, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Barometer15m, Header = "Baro 15m", Width = 70, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Barometer30m, Header = "Baro 30m", Width = 70, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Barometer1h, Header = "Baro 1h", Width = 70, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Barometer4h, Header = "Baro 4h", Width = 70, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.Barometer1d, Header = "Baro 1d", Width = 70, Alignment = HorizontalAlignment.Left, IsVisible=false},
            new() { ColumnEnum = SignalColumnEnum.MinimumEntry, Header = "Min Entry", Width = 70, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.PriceMinPerc, Header = "Min %", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.PriceMaxPerc, Header = "Max %", Width = 60, Alignment = HorizontalAlignment.Right},
            new() { ColumnEnum = SignalColumnEnum.SignalStatus, Header = "Status", Width = 60, Alignment = HorizontalAlignment.Left},
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