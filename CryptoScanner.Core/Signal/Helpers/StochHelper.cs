using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class StochHelper
{

    public static bool StochOversold(this MyData candle)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData?.StochSignal > GlobalData.Settings.General.SettingsStoch.Oversold)
            return false;
        if (candle.CandleData?.StochOscillator > GlobalData.Settings.General.SettingsStoch.Oversold)
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


    public static bool StochOverbought(this MyData candle)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten groter zijn dan 80% (overbought)
        if (candle.CandleData?.StochSignal < GlobalData.Settings.General.SettingsStoch.Overbought)
            return false;
        if (candle.CandleData?.StochOscillator < GlobalData.Settings.General.SettingsStoch.Overbought)
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

}
