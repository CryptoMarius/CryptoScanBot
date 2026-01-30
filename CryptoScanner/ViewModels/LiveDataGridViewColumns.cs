using System.Collections;

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

public class LiveDataColumnComparer : IComparer
{
    // Kind of overkill, but its much nicer having everything in 1 comparer
    private LiveDataColumnEnum? SortColumn { get; set; }
    private readonly CaseInsensitiveComparer ObjectCompare = new();

    public LiveDataColumnComparer(LiveDataColumnEnum? sortColumn)
    {
        SortColumn = sortColumn;
    }


    public int Compare(object? x, object? y)
    {
        if (SortColumn != null && x is LiveDataViewModel a && y is LiveDataViewModel b)
        {
            try
            {
                int compareResult = SortColumn switch
                {
                    //LiveDataColumnEnum.Id => ObjectCompare.Compare(a.Object.Symbol.Id, b.Object.Symbol.Id),
                    LiveDataColumnEnum.Date => ObjectCompare.Compare(a.Object.Candle.OpenTime + a.Object.Interval.Duration,
                    b.Object.Candle.OpenTime + b.Object.Interval.Duration),
                    LiveDataColumnEnum.Exchange => ObjectCompare.Compare(a.Object.Symbol.Exchange.Name, b.Object.Symbol.Exchange.Name),
                    LiveDataColumnEnum.Symbol => ObjectCompare.Compare(a.Object.Symbol.Name, b.Object.Symbol.Name),
                    LiveDataColumnEnum.Interval => ObjectCompare.Compare(a.Object.Interval.IntervalPeriod, b.Object.Interval.IntervalPeriod),
                    LiveDataColumnEnum.Price => ObjectCompare.Compare(a.Object.Symbol.LastPrice, b.Object.Symbol.LastPrice),
                    LiveDataColumnEnum.Volume => ObjectCompare.Compare(a.Object.Symbol.Volume, b.Object.Symbol.Volume),
                    LiveDataColumnEnum.BB => ObjectCompare.Compare(a.Object.Candle.CandleData?.BollingerBandsPercentage, b.Object.Candle.CandleData?.BollingerBandsPercentage),
                    LiveDataColumnEnum.BbUpper => ObjectCompare.Compare(a.Object.Candle.CandleData?.BollingerBandsUpperBand, b.Object.Candle.CandleData?.BollingerBandsUpperBand),
                    LiveDataColumnEnum.BbLower => ObjectCompare.Compare(a.Object.Candle.CandleData?.BollingerBandsLowerBand, b.Object.Candle.CandleData?.BollingerBandsLowerBand),
                    //LiveDataColumnEnum.AvgBB => ObjectCompare.Compare(a.Object.Candle.CandleData?.AvgBB, b.Object.Candle.CandleData?.AvgBB),
                    LiveDataColumnEnum.MacdValue => ObjectCompare.Compare(a.Object.Candle.CandleData?.MacdValue, b.Object.Candle.CandleData?.MacdValue),
                    LiveDataColumnEnum.MacdSignal => ObjectCompare.Compare(a.Object.Candle.CandleData?.MacdSignal, b.Object.Candle.CandleData?.MacdSignal),
                    LiveDataColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Object.Candle.CandleData?.MacdHistogram, b.Object.Candle.CandleData?.MacdHistogram),
                    LiveDataColumnEnum.Rsi => ObjectCompare.Compare(a.Object.Candle.CandleData?.Rsi, b.Object.Candle.CandleData?.Rsi),
                    LiveDataColumnEnum.StochOscillator => ObjectCompare.Compare(a.Object.Candle.CandleData?.StochOscillator, b.Object.Candle.CandleData?.StochOscillator),
                    LiveDataColumnEnum.StochSignal => ObjectCompare.Compare(a.Object.Candle.CandleData?.StochSignal, b.Object.Candle.CandleData?.StochSignal),
                    LiveDataColumnEnum.Sma200 => ObjectCompare.Compare(a.Object.Candle.CandleData?.Sma200, b.Object.Candle.CandleData?.Sma200),
                    LiveDataColumnEnum.Sma50 => ObjectCompare.Compare(a.Object.Candle.CandleData?.Sma50, b.Object.Candle.CandleData?.Sma50),
                    LiveDataColumnEnum.Sma20 => ObjectCompare.Compare(a.Object.Candle.CandleData?.Sma20, b.Object.Candle.CandleData?.Sma20),
                    LiveDataColumnEnum.PSar => ObjectCompare.Compare(a.Object.Candle.CandleData?.PSar, b.Object.Candle.CandleData?.PSar),
                    LiveDataColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.Object.Candle.CandleData?.Lux5mValue, b.Object.Candle.CandleData?.Lux5mValue),
                    LiveDataColumnEnum.FundingRate => ObjectCompare.Compare(a.Object.Symbol.FundingRate, b.Object.Symbol.FundingRate),
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
                    compareResult = ObjectCompare.Compare(a.Symbol, b.Symbol);
                if (compareResult == 0)
                    compareResult = -ObjectCompare.Compare(a.Object.Interval.Duration, b.Object.Interval.Duration);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.Object.Candle.OpenTime + a.Object.Interval.Duration,
                        b.Object.Candle.OpenTime + b.Object.Interval.Duration);

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
