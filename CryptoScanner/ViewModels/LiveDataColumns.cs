using Avalonia.Controls;
using Avalonia.Layout;

using CryptoScanner.Core.Model;

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CryptoScanner.ViewModels;

public enum LiveDataColumnEnum
{
    Date,
    Exchange,
    Symbol,
    Interval,
    Price,
    Volume,
    BB,
    BbUpper,
    BbLower,
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
    FundingRate,
#if StrategyBbma
    // Debug
    Wma05Low,
    ma05High,
    Wma10Low,
    Wma10High,
#endif
}

public class LiveDataColumnComparer : IGridComparer<CryptoLiveData, LiveDataColumnEnum>
{
    public LiveDataColumnEnum SortColumn { get; set; }
    public ListSortDirection SortDirection { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();


    public int Compare(CryptoLiveData? a, CryptoLiveData? b)
    {
        if (a == null || b == null)
            return 0;

        try
        {
            int compareResult = SortColumn switch
            {
                //LiveDataColumnEnum.Id => ObjectCompare.Compare(a.Symbol.Id, b.Symbol.Id),
                LiveDataColumnEnum.Date => ObjectCompare.Compare(a.Candle.OpenTime + a.Interval.Duration,
                b.Candle.OpenTime + b.Interval.Duration),
                LiveDataColumnEnum.Exchange => ObjectCompare.Compare(a.Symbol.Exchange.Name, b.Symbol.Exchange.Name),
                LiveDataColumnEnum.Symbol => ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name),
                LiveDataColumnEnum.Interval => ObjectCompare.Compare(a.Interval.IntervalPeriod, b.Interval.IntervalPeriod),
                LiveDataColumnEnum.Price => ObjectCompare.Compare(a.Symbol.LastPrice, b.Symbol.LastPrice),
                LiveDataColumnEnum.Volume => ObjectCompare.Compare(a.Symbol.Volume, b.Symbol.Volume),
                LiveDataColumnEnum.BB => ObjectCompare.Compare(a.Candle.CandleData?.BollingerBandsPercentage, b.Candle.CandleData?.BollingerBandsPercentage),
                LiveDataColumnEnum.BbUpper => ObjectCompare.Compare(a.Candle.CandleData?.BollingerBandsUpperBand, b.Candle.CandleData?.BollingerBandsUpperBand),
                LiveDataColumnEnum.BbLower => ObjectCompare.Compare(a.Candle.CandleData?.BollingerBandsLowerBand, b.Candle.CandleData?.BollingerBandsLowerBand),
                //LiveDataColumnEnum.AvgBB => ObjectCompare.Compare(a.Candle.CandleData?.AvgBB, b.Candle.CandleData?.AvgBB),
                LiveDataColumnEnum.MacdValue => ObjectCompare.Compare(a.Candle.CandleData?.MacdValue, b.Candle.CandleData?.MacdValue),
                LiveDataColumnEnum.MacdSignal => ObjectCompare.Compare(a.Candle.CandleData?.MacdSignal, b.Candle.CandleData?.MacdSignal),
                LiveDataColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Candle.CandleData?.MacdHistogram, b.Candle.CandleData?.MacdHistogram),
                LiveDataColumnEnum.Rsi => ObjectCompare.Compare(a.Candle.CandleData?.Rsi, b.Candle.CandleData?.Rsi),
                LiveDataColumnEnum.StochOscillator => ObjectCompare.Compare(a.Candle.CandleData?.StochOscillator, b.Candle.CandleData?.StochOscillator),
                LiveDataColumnEnum.StochSignal => ObjectCompare.Compare(a.Candle.CandleData?.StochSignal, b.Candle.CandleData?.StochSignal),
                LiveDataColumnEnum.Sma200 => ObjectCompare.Compare(a.Candle.CandleData?.Sma200, b.Candle.CandleData?.Sma200),
                LiveDataColumnEnum.Sma50 => ObjectCompare.Compare(a.Candle.CandleData?.Sma50, b.Candle.CandleData?.Sma50),
                LiveDataColumnEnum.Sma20 => ObjectCompare.Compare(a.Candle.CandleData?.Sma20, b.Candle.CandleData?.Sma20),
                LiveDataColumnEnum.PSar => ObjectCompare.Compare(a.Candle.CandleData?.PSar, b.Candle.CandleData?.PSar),
                LiveDataColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.Candle.CandleData?.Lux5mValue, b.Candle.CandleData?.Lux5mValue),
                LiveDataColumnEnum.FundingRate => ObjectCompare.Compare(a.Symbol.FundingRate, b.Symbol.FundingRate),
#if StrategyBbma
                LiveDataColumnEnum.Wma05Low => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Wma05Low, b.LiveDataObject.Candle.CandleData?.Wma05Low),
                LiveDataColumnEnum.ma05High => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Wma05High, b.LiveDataObject.Candle.CandleData?.Wma05High),
                LiveDataColumnEnum.Wma10Low => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Wma10Low, b.LiveDataObject.Candle.CandleData?.Wma10Low),
                LiveDataColumnEnum.Wma10High => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Wma10High, b.LiveDataObject.Candle.CandleData?.Wma10High),
#endif
                _ => 0
            };

            // Sort on some more columns...
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Symbol.Name, b.Symbol.Name);
            if (compareResult == 0)
                compareResult = -ObjectCompare.Compare(a.Interval.Duration, b.Interval.Duration);
            if (compareResult == 0)
                compareResult = ObjectCompare.Compare(a.Candle.OpenTime + a.Interval.Duration,
                    b.Candle.OpenTime + b.Interval.Duration);

            return compareResult;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}


public static class LiveDataColumns
{
    public static ObservableCollection<GridColumnDefinition<LiveDataColumnEnum>> GetColumns()
    {
        var columns = new ObservableCollection<GridColumnDefinition<LiveDataColumnEnum>>
        {
            new() { ColumnEnum = LiveDataColumnEnum.Date, Header = "Date", Width = 180, Alignment = HorizontalAlignment.Left },
            new() { ColumnEnum = LiveDataColumnEnum.Exchange, Header = "Exchange", Width = 110, Alignment = HorizontalAlignment.Left, IsVisible = false },
            new() { ColumnEnum = LiveDataColumnEnum.Symbol, Header = "Symbol", Width = 100, Alignment = HorizontalAlignment.Left },
            new() { ColumnEnum = LiveDataColumnEnum.Interval, Header = "Interval", Width = 60, Alignment = HorizontalAlignment.Center },
            new() { ColumnEnum = LiveDataColumnEnum.Price, Header = "Price", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.Volume, Header = "Volume", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.BB, Header = "BB%", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.BbLower, Header = "BbLower", Width = 60, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.BbUpper, Header = "BbUpper", Width = 60, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.Rsi, Header = "Rsi", Width = 60, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.LuxIndicator5m, Header = "Lux 5m", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.MacdValue, Header = "Macd Value", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.MacdSignal, Header = "Macd signal", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.MacdHistogram, Header = "Macd Histo", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.StochOscillator, Header = "Stoch Oscillator", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.StochSignal, Header = "Stoch Signal", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.Sma200, Header = "Sma200", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.Sma50, Header = "Sma50", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.Sma20, Header = "Sma20", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.PSar, Header = "PSar", Width = 75, Alignment = HorizontalAlignment.Right },
            new() { ColumnEnum = LiveDataColumnEnum.FundingRate, Header = "FundingRate", Width = 75, Alignment = HorizontalAlignment.Right },
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