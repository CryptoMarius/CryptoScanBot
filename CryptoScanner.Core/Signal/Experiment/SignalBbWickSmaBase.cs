using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// Base class for the BbWickSma strategy.
/// Detects a potential reversal by combining four conditions:
///   1. A candle wick recently penetrated a Bollinger Band (upper for short, lower for long).
///   2. The SMA20 slope has turned in the expected direction (negative for short, positive for long).
///   3. The close recently crossed the SMA50 in the expected direction.
///   4. The high-low price range over the last LookbackCandles candles meets the minimum threshold.
/// </summary>
public class SignalBbWickSmaBase : SignalCreateBase
{
    // Number of candles to look back for the BB wick and the SMA50 cross.
    protected const int LookbackCandles = 10;

    // Minimum high-low price range as a percentage of the mid price over the lookback window.
    protected const double MinPriceRangePercentage = 2.0;


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
            || data.Candle.OpenTime == 0
            || data.CandleData == null
            || data.CandleData.Sma20 == null
            || data.CandleData.Sma50 == null
            || data.CandleData.BollingerBandsDeviation == null)
            return false;

        return true;
    }


    /// <summary>
    /// Returns the high-low price range over the last <paramref name="count"/> candles as a percentage of the mid price.
    /// Formula: (maxHigh - minLow) / ((maxHigh + minLow) / 2) * 100
    /// </summary>
    protected double GetPriceRangePercentage(int count)
    {
        double maxHigh = double.MinValue;
        double minLow = double.MaxValue;

        MyData? candle = CandleLast;
        for (int i = 0; i < count; i++)
        {
            if (candle == null)
                break;

            double high = (double)candle.Candle.High;
            double low = (double)candle.Candle.Low;

            if (high > maxHigh) maxHigh = high;
            if (low < minLow) minLow = low;

            if (!GetPrevCandle(candle, out candle))
                break;
        }

        if (maxHigh == double.MinValue || minLow <= 0)
            return 0;

        double midPrice = (maxHigh + minLow) / 2.0;
        return (maxHigh - minLow) / midPrice * 100.0;
    }


    /// <summary>
    /// Returns true if any of the last <paramref name="count"/> candles had a wick above the upper Bollinger Band.
    /// </summary>
    protected bool HadWickAboveBb(int count)
    {
        MyData? candle = CandleLast;
        for (int i = 0; i < count; i++)
        {
            if (candle!.IsAboveBollingerBands(true))
                return true;
            if (!GetPrevCandle(candle, out candle))
                return false;
        }
        return false;
    }


    /// <summary>
    /// Returns true if any of the last <paramref name="count"/> candles had a wick below the lower Bollinger Band.
    /// </summary>
    protected bool HadWickBelowBb(int count)
    {
        MyData? candle = CandleLast;
        for (int i = 0; i < count; i++)
        {
            if (candle!.IsBelowBollingerBands(true))
                return true;
            if (!GetPrevCandle(candle, out candle))
                return false;
        }
        return false;
    }


    /// <summary>
    /// Returns true if the SMA20 slope on the current candle is negative (SMA20 is declining).
    /// </summary>
    protected bool IsSma20SlopeNegative()
    {
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
            return false;
        return CandleLast.CandleData.Sma20!.Value < prev.CandleData.Sma20!.Value;
    }


    /// <summary>
    /// Returns true if the SMA20 slope on the current candle is positive (SMA20 is rising).
    /// </summary>
    protected bool IsSma20SlopePositive()
    {
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
            return false;
        return CandleLast.CandleData.Sma20!.Value > prev.CandleData.Sma20!.Value;
    }


    /// <summary>
    /// Returns true if within the last <paramref name="count"/> candle pairs the close crossed
    /// from above to below the SMA50 (bearish crossover).
    /// </summary>
    protected bool HadCrossBelowSma50(int count)
    {
        MyData? current = CandleLast;
        for (int i = 0; i < count; i++)
        {
            if (!GetPrevCandle(current, out MyData? prev) || prev == null)
                return false;

            double sma50Current = current!.CandleData!.Sma50!.Value;
            double sma50Prev = prev.CandleData!.Sma50!.Value;

            if ((double)current.Candle.Close < sma50Current && (double)prev.Candle.Close >= sma50Prev)
                return true;

            current = prev;
        }
        return false;
    }


    /// <summary>
    /// Returns true if within the last <paramref name="count"/> candle pairs the close crossed
    /// from below to above the SMA50 (bullish crossover).
    /// </summary>
    protected bool HadCrossAboveSma50(int count)
    {
        MyData? current = CandleLast;
        for (int i = 0; i < count; i++)
        {
            if (!GetPrevCandle(current, out MyData? prev) || prev == null)
                return false;

            double sma50Current = current!.CandleData!.Sma50!.Value;
            double sma50Prev = prev.CandleData!.Sma50!.Value;
            if ((double)current.Candle.Close > sma50Current && (double)prev.Candle.Close <= sma50Prev)
                return true;

            current = prev;
        }
        return false;
    }
}
