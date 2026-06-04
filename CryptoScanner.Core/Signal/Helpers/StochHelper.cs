using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class StochHelper
{

    /// <summary>
    /// Maps a lower interval to its directional higher interval for the StochDir strategy.
    /// The ratio is approximately 12x, matching the BBMA 3rd-timeframe convention.
    /// Returns false when no sensible higher interval exists (e.g. >= 6h).
    /// </summary>
    public static bool GetStochDirHigherInterval(CryptoIntervalPeriod interval, out CryptoIntervalPeriod higherInterval)
    {
        switch (interval)
        {
            case CryptoIntervalPeriod.interval1m:
                higherInterval = CryptoIntervalPeriod.interval15m;  // 15x
                return true;
            case CryptoIntervalPeriod.interval2m:
                higherInterval = CryptoIntervalPeriod.interval30m;  // 15x
                return true;
            case CryptoIntervalPeriod.interval3m:
            case CryptoIntervalPeriod.interval5m:
                higherInterval = CryptoIntervalPeriod.interval1h;   // 20x / 12x
                return true;
            case CryptoIntervalPeriod.interval10m:
                higherInterval = CryptoIntervalPeriod.interval2h;   // 12x
                return true;
            case CryptoIntervalPeriod.interval15m:
                higherInterval = CryptoIntervalPeriod.interval4h;   // 16x
                return true;
            case CryptoIntervalPeriod.interval30m:
                higherInterval = CryptoIntervalPeriod.interval8h;   // 16x
                return true;
            case CryptoIntervalPeriod.interval1h:
                higherInterval = CryptoIntervalPeriod.interval1d;   // 24x
                return true;
            case CryptoIntervalPeriod.interval2h:
            case CryptoIntervalPeriod.interval3h:
            case CryptoIntervalPeriod.interval4h:
                higherInterval = CryptoIntervalPeriod.interval1d;   // 12x / 8x / 6x
                return true;
            default:
                higherInterval = interval;
                return false;
        }
    }



    public static bool StochOversold(this MyData candle, int correction = 0)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData?.StochSignal > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
            return false;
        if (candle.CandleData?.StochOscillator > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
            return false;
        return true;
    }

    //public static bool StochSignalOversold(this CryptoCandle candle, int correction = 0)
    //{
    //    // Stochastic oscillator %D (red)
    //    if (candle.CandleData?.StochSignal > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
    //        return false;
    //    return true;
    //}


    //public static bool StochOscillatorOversold(this CryptoCandle candle, int correction = 0)
    //{
    //    // Stochastic oscillator %K (blue)
    //    if (candle.CandleData?.StochOscillator > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
    //        return false;
    //    return true;
    //}


    //public static bool StochSignalOverbought(this CryptoCandle candle, int correction = 0)
    //{
    //    // Stochastic oscillator %D (red)
    //    if (candle.CandleData?.StochSignal < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
    //        return false;
    //    return true;
    //}

    //public static bool StochOscillatorOverbought(this CryptoCandle candle, int correction = 0)
    //{
    //    // Stochastic oscillator %K (blue)
    //    if (candle.CandleData?.StochOscillator < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
    //        return false;
    //    return true;
    //}


    public static bool StochOverbought(this MyData candle, int correction = 0)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten groter zijn dan 80% (overbought)
        if (candle.CandleData?.StochSignal < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
            return false;
        if (candle.CandleData?.StochOscillator < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
            return false;
        return true;
    }


    /// <summary>
    /// Is the Stoch oscillator increasing in the last x candles
    /// allowedDown: how many candles are allowed to deviate downward
    /// </summary>
    public static bool StochIncreasingInTheLast(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? data, int candleCount, int allowedDown)
    {
        // from right to left
        int down = 0;
        bool first = true;
        while (candleCount > 0)
        {
            if (!myBase.GetPrevCandle(symbolInterval.Interval, data!, out MyData? prev))
                return false;
            if (prev!.CandleData == null || prev.CandleData.StochOscillator == null)
                return false;

            if (data?.CandleData?.StochOscillator <= prev?.CandleData?.StochOscillator)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            data = prev;
            candleCount--;
            first = false;
        }

        return true;
    }


    /// <summary>
    /// Is the Stoch oscillator decreasing in the last x candles
    /// allowedDown: how many candles are allowed to deviate upward
    /// </summary>
    public static bool StochDecreasingInTheLast(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? data, int candleCount, int allowedDown)
    {
        // from right to left
        int down = 0;
        bool first = true;
        while (candleCount > 0)
        {
            if (!myBase.GetPrevCandle(symbolInterval.Interval, data!, out MyData? prev))
                return false;
            if (prev!.CandleData == null || prev.CandleData.StochOscillator == null)
                return false;

            if (data?.CandleData?.StochOscillator >= prev?.CandleData?.StochOscillator)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            data = prev;
            candleCount--;
            first = false;
        }

        return true;
    }


    /// <summary>
    /// Calculate the Stoch surface area of the oversold part from limit to stoch
    /// </summary>
    public static double StochOversoldSurface(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candle, int candleCount, double limit)
    {
        double surface = 0;
        while (candleCount > 0)
        {
            if (candle == null || candle!.CandleData == null || candle.CandleData.StochOscillator == null)
                return 0;

            double result = limit - candle.CandleData.StochOscillator.Value;
            if (result > 0)
                surface += result;

            // stop if almost halfway
            if (candle.CandleData.StochOscillator.Value > 40)
                break;

            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle, out candle))
                return 0;
            candleCount--;
        }

        return surface;
    }


    /// <summary>
    /// Calculate the Stoch surface area of the overbought part from limit to stoch
    /// </summary>
    public static double StochOverboughtSurface(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candle, int candleCount, double limit)
    {
        double surface = 0;
        while (candleCount > 0)
        {
            if (candle == null || candle!.CandleData == null || candle.CandleData.StochOscillator == null)
                return 0;

            double result = candle.CandleData.StochOscillator.Value - limit;
            if (result > 0)
                surface += result;

            // stop if almost halfway
            if (candle.CandleData.StochOscillator.Value < 60)
                break;

            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle, out candle))
                return 0;
            candleCount--;
        }

        return surface;
    }


    /// <summary>
    /// Persistence gate (option 1): walk back at most maxLookback bars from startCandle
    /// and count the most-recent contiguous run of bars where %K was in OS (long) /
    /// OB (short). Skips post-recovery bars at the head — looks for the first run, then
    /// stops as soon as that run ends going further back. Returns 0 when no run is
    /// found within the lookback or when %K data is missing.
    /// </summary>
    public static int CountStochExtremeBarsBack(
        this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval,
        MyData? startCandle, int maxLookback, CryptoTradeSide side)
    {
        if (maxLookback <= 0)
            return 0;

        double os = GlobalData.Settings.General.SettingsStoch.Oversold;
        double ob = GlobalData.Settings.General.SettingsStoch.Overbought;

        MyData? candle = startCandle;
        bool inRun = false;
        int count = 0;
        while (maxLookback-- > 0 && candle?.CandleData?.StochOscillator != null)
        {
            double k = candle.CandleData.StochOscillator.Value;
            bool extreme = side == CryptoTradeSide.Long ? k < os : k > ob;
            if (extreme)
            {
                inRun = true;
                count++;
            }
            else if (inRun)
            {
                // Found the back-edge of the run while walking backward → stop.
                break;
            }

            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle, out candle))
                break;
        }
        return count;
    }


    /// <summary>
    /// Statistical-depth gate (option 3): compute mean / population stdev of %K over the
    /// lookback window (signal interval), then return the z-score of the most extreme
    /// observed %K in that window — minimum for Long, maximum for Short. Returns null
    /// when the sample is too small (< 5) or stdev collapses (flat window).
    /// </summary>
    public static double? StochExtremeZScore(
        this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval,
        MyData? startCandle, int lookback, CryptoTradeSide side)
    {
        if (lookback < 5)
            return null;

        MyData? candle = startCandle;
        List<double> values = new(lookback);
        while (lookback-- > 0 && candle?.CandleData?.StochOscillator != null)
        {
            values.Add(candle.CandleData.StochOscillator.Value);
            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle, out candle))
                break;
        }
        if (values.Count < 5)
            return null;

        double mean = values.Average();
        double variance = 0;
        for (int i = 0; i < values.Count; i++)
        {
            double d = values[i] - mean;
            variance += d * d;
        }
        variance /= values.Count;
        double stdev = Math.Sqrt(variance);
        if (stdev < 1e-6)
            return null;

        double extreme = side == CryptoTradeSide.Long ? values.Min() : values.Max();
        return (extreme - mean) / stdev;
    }


    /// <summary>
    /// Multi-timeframe gate (option 5): confirm the higher TF (mapped via
    /// GetStochDirHigherInterval) was also in OS (long) / OB (short) within the last
    /// mtfLookback closed bars. Returns false (with a reason) when the higher TF is
    /// unavailable, data isn't ready, or no extreme bar is found in the window.
    /// </summary>
    public static bool HasHigherTfStochExtreme(
        this SignalCreateBase myBase, int mtfLookback, CryptoTradeSide side, out string reason)
    {
        reason = "";
        if (mtfLookback <= 0)
            mtfLookback = 1;

        var period = myBase.Interval.IntervalPeriod;
        if (!GetStochDirHigherInterval(period, out CryptoIntervalPeriod higherPeriod))
        {
            reason = $"no higher tf mapped above {period}";
            return false;
        }

        var higherSymbolInterval = myBase.Symbol.GetSymbolInterval(higherPeriod);
        var higherInterval = higherSymbolInterval.Interval;

        // Make sure the higher TF indicators are populated. PrepareIndicators is a no-op
        // when already cached for this interval.
        if (!myBase.IndicatorDataList.PrepareIndicators(myBase.Symbol, higherInterval, myBase.CandleLast.Candle.OpenTime))
        {
            reason = $"higher tf indicators not ready ({higherInterval.Name})";
            return false;
        }

        // Align the current open time down to the higher-TF candle boundary.
        CandleTime aligned = myBase.CandleLast.Candle.OpenTime
                             - (myBase.CandleLast.Candle.OpenTime % higherInterval.Duration);
        if (!myBase.IndicatorDataList.TryGetCandle(higherInterval, aligned, out MyData? higherCandle) || higherCandle == null)
        {
            reason = $"higher tf candle missing ({higherInterval.Name})";
            return false;
        }

        double os = GlobalData.Settings.General.SettingsStoch.Oversold;
        double ob = GlobalData.Settings.General.SettingsStoch.Overbought;

        MyData? candle = higherCandle;
        int n = mtfLookback;
        while (n-- > 0 && candle?.CandleData?.StochOscillator != null)
        {
            double k = candle.CandleData.StochOscillator.Value;
            bool extreme = side == CryptoTradeSide.Long ? k < os : k > ob;
            if (extreme)
                return true;

            if (!myBase.GetPrevCandle(higherInterval, candle, out candle))
                break;
        }

        reason = $"no {(side == CryptoTradeSide.Long ? "OS" : "OB")} on {higherInterval.Name} in last {mtfLookback} bars";
        return false;
    }

}
