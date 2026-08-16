using CryptoScanner.Core.Enums;

using System.Collections;

namespace CryptoScanner.ViewModels;

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
                    LiveDataColumnEnum.BB => ObjectCompare.Compare(a.Object.CandleData?.BollingerBandsPercentage, b.Object.CandleData?.BollingerBandsPercentage),
                    LiveDataColumnEnum.BbUpper => ObjectCompare.Compare(a.Object.CandleData?.BollingerBandsUpperBand, b.Object.CandleData?.BollingerBandsUpperBand),
                    LiveDataColumnEnum.BbLower => ObjectCompare.Compare(a.Object.CandleData?.BollingerBandsLowerBand, b.Object.CandleData?.BollingerBandsLowerBand),
                    //LiveDataColumnEnum.AvgBB => ObjectCompare.Compare(a.Object.CandleData?.AvgBB, b.Object.CandleData?.AvgBB),
                    LiveDataColumnEnum.RangeIndex => ObjectCompare.Compare(
                        a.Object.Symbol.GetSymbolInterval(a.Object.Interval.IntervalPeriod).BandRange?.Index,
                        b.Object.Symbol.GetSymbolInterval(b.Object.Interval.IntervalPeriod).BandRange?.Index),
                    LiveDataColumnEnum.RangeCount => ObjectCompare.Compare(
                        a.Object.Symbol.GetSymbolInterval(a.Object.Interval.IntervalPeriod).BandRange?.MeasurementCount,
                        b.Object.Symbol.GetSymbolInterval(b.Object.Interval.IntervalPeriod).BandRange?.MeasurementCount),
                    LiveDataColumnEnum.MacdValue => ObjectCompare.Compare(a.Object.CandleData?.MacdValue, b.Object.CandleData?.MacdValue),
                    LiveDataColumnEnum.MacdSignal => ObjectCompare.Compare(a.Object.CandleData?.MacdSignal, b.Object.CandleData?.MacdSignal),
                    LiveDataColumnEnum.MacdHistogram => ObjectCompare.Compare(a.Object.CandleData?.MacdHistogram, b.Object.CandleData?.MacdHistogram),
                    LiveDataColumnEnum.Rsi => ObjectCompare.Compare(a.Object.CandleData?.Rsi, b.Object.CandleData?.Rsi),
                    LiveDataColumnEnum.StochOscillator => ObjectCompare.Compare(a.Object.CandleData?.StochOscillator, b.Object.CandleData?.StochOscillator),
                    LiveDataColumnEnum.StochSignal => ObjectCompare.Compare(a.Object.CandleData?.StochSignal, b.Object.CandleData?.StochSignal),
                    LiveDataColumnEnum.Sma200 => ObjectCompare.Compare(a.Object.CandleData?.Sma200, b.Object.CandleData?.Sma200),
                    LiveDataColumnEnum.Sma50 => ObjectCompare.Compare(a.Object.CandleData?.Sma50, b.Object.CandleData?.Sma50),
                    LiveDataColumnEnum.Sma20 => ObjectCompare.Compare(a.Object.CandleData?.Sma20, b.Object.CandleData?.Sma20),
                    LiveDataColumnEnum.PSar => ObjectCompare.Compare(a.Object.CandleData?.PSar, b.Object.CandleData?.PSar),
                    LiveDataColumnEnum.LuxIndicator5m => ObjectCompare.Compare(a.Object.CandleData?.Lux5mValue, b.Object.CandleData?.Lux5mValue),
                    LiveDataColumnEnum.FundingRate => ObjectCompare.Compare(a.Object.Symbol.FundingRate, b.Object.Symbol.FundingRate),
#if StrategyBbma
                    LiveDataColumnEnum.Wma05Low => ObjectCompare.Compare(a.LiveDataObject.CandleData?.Wma05Low, b.LiveDataObject.CandleData?.Wma05Low),
                    LiveDataColumnEnum.ma05High => ObjectCompare.Compare(a.LiveDataObject.CandleData?.Wma05High, b.LiveDataObject.CandleData?.Wma05High),
                    LiveDataColumnEnum.Wma10Low => ObjectCompare.Compare(a.LiveDataObject.CandleData?.Wma10Low, b.LiveDataObject.CandleData?.Wma10Low),
                    LiveDataColumnEnum.Wma10High => ObjectCompare.Compare(a.LiveDataObject.CandleData?.Wma10High, b.LiveDataObject.CandleData?.Wma10High),
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
