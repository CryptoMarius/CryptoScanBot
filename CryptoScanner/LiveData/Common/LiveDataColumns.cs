using CryptoScanner.LiveData.Model;

using System.Collections;

namespace CryptoScanner.LiveData.Common;

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
        if (SortColumn != null && x is LiveDataInfo a && y is LiveDataInfo b)
        {
            try
            {
                int compareResult = SortColumn switch
                {
                    //LiveDataColumnEnum.Id => ObjectCompare.Compare(a.LiveDataObject.Symbol.Id, b.LiveDataObject.Symbol.Id),
                    LiveDataColumnEnum.Date => ObjectCompare.Compare(a.LiveDataObject.Candle.OpenTime + a.LiveDataObject.Interval.Duration, 
                    b.LiveDataObject.Candle.OpenTime + b.LiveDataObject.Interval.Duration),
                    LiveDataColumnEnum.Exchange => ObjectCompare.Compare(a.LiveDataObject.Symbol.Exchange.Name, b.LiveDataObject.Symbol.Exchange.Name),
                    LiveDataColumnEnum.Symbol => ObjectCompare.Compare(a.LiveDataObject.Symbol.Name, b.LiveDataObject.Symbol.Name),
                    LiveDataColumnEnum.Interval => ObjectCompare.Compare(a.LiveDataObject.Interval.IntervalPeriod, b.LiveDataObject.Interval.IntervalPeriod),
                    LiveDataColumnEnum.Price => ObjectCompare.Compare(a.LiveDataObject.Symbol.LastPrice, b.LiveDataObject.Symbol.LastPrice),
                    LiveDataColumnEnum.Volume => ObjectCompare.Compare(a.LiveDataObject.Symbol.Volume, b.LiveDataObject.Symbol.Volume),
                    LiveDataColumnEnum.BB => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.BollingerBandsPercentage, b.LiveDataObject.Candle.CandleData?.BollingerBandsPercentage),
                    LiveDataColumnEnum.BbUpper => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.BollingerBandsUpperBand, b.LiveDataObject.Candle.CandleData?.BollingerBandsUpperBand),
                    LiveDataColumnEnum.BbLower => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.BollingerBandsLowerBand, b.LiveDataObject.Candle.CandleData?.BollingerBandsLowerBand),
                    //LiveDataColumnEnum.AvgBB => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.AvgBB, b.LiveDataObject.Candle.CandleData?.AvgBB),
                    LiveDataColumnEnum.MacdValue => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.MacdValue, b.LiveDataObject.Candle.CandleData?.MacdValue),
                    LiveDataColumnEnum.MacdSignal => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.MacdSignal, b.LiveDataObject.Candle.CandleData?.MacdSignal),
                    LiveDataColumnEnum.MacdHistogram => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.MacdHistogram, b.LiveDataObject.Candle.CandleData?.MacdHistogram),
                    LiveDataColumnEnum.Rsi => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Rsi, b.LiveDataObject.Candle.CandleData?.Rsi),
                    LiveDataColumnEnum.Stoch => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.StochOscillator, b.LiveDataObject.Candle.CandleData?.StochOscillator),
                    LiveDataColumnEnum.Signal => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.StochSignal, b.LiveDataObject.Candle.CandleData?.StochSignal),
                    LiveDataColumnEnum.Sma200 => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Sma200, b.LiveDataObject.Candle.CandleData?.Sma200),
                    LiveDataColumnEnum.Sma50 => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Sma50, b.LiveDataObject.Candle.CandleData?.Sma50),
                    LiveDataColumnEnum.Sma20 => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Sma20, b.LiveDataObject.Candle.CandleData?.Sma20),
                    LiveDataColumnEnum.PSar => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.PSar, b.LiveDataObject.Candle.CandleData?.PSar),
                    LiveDataColumnEnum.Lux5m => ObjectCompare.Compare(a.LiveDataObject.Candle.CandleData?.Lux5mValue, b.LiveDataObject.Candle.CandleData?.Lux5mValue),
                    LiveDataColumnEnum.FundingRate => ObjectCompare.Compare(a.LiveDataObject.Symbol.FundingRate, b.LiveDataObject.Symbol.FundingRate),
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
                    compareResult = -ObjectCompare.Compare(a.LiveDataObject.Interval.Duration, b.LiveDataObject.Interval.Duration);
                if (compareResult == 0)
                    compareResult = ObjectCompare.Compare(a.LiveDataObject.Candle.OpenTime + a.LiveDataObject.Interval.Duration,
                        b.LiveDataObject.Candle.OpenTime + b.LiveDataObject.Interval.Duration);

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
