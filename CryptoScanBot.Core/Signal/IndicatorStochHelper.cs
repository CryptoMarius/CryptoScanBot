using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;

public static class IndicatorStochHelper
{

    public static bool StochOversold(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData?.StochSignal > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
            return false;
        if (candle.CandleData?.StochOscillator > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
            return false;
        return true;
    }

    public static bool StochSignalOversold(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic oscillator %D (red)
        if (candle.CandleData?.StochSignal > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
            return false;
        return true;
    }


    public static bool StochOscillatorOversold(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic oscillator %K (blue)
        if (candle.CandleData?.StochOscillator > GlobalData.Settings.General.SettingsStoch.Oversold - correction)
            return false;
        return true;
    }


    public static bool StochSignalOverbought(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic oscillator %D (red)
        if (candle.CandleData?.StochSignal < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
            return false;
        return true;
    }

    public static bool StochOscillatorOverbought(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic oscillator %K (blue)
        if (candle.CandleData?.StochOscillator < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
            return false;
        return true;
    }


    public static bool StochOverbought(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten groter zijn dan 80% (overbought)
        if (candle.CandleData?.StochSignal < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
            return false;
        if (candle.CandleData?.StochOscillator < GlobalData.Settings.General.SettingsStoch.Overbought + correction)
            return false;
        return true;
    }


    /// <summary>
    /// Calculate the Stoch surface area of the oversold part from limit to stoch
    /// </summary>
    public static double StochOversoldSurface(this CryptoSymbolInterval symbolInterval, CryptoCandle? candle, int candleCount, double limit)
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

            if (!symbolInterval.GetPrevCandle(candle, out candle))
                return 0;
            candleCount--;
        }

        return surface;
    }


    /// <summary>
    /// Calculate the Stoch surface area of the overbought part from limit to stoch
    /// </summary>
    public static double StochOverboughtSurface(this CryptoSymbolInterval symbolInterval, CryptoCandle? candle, int candleCount, double limit)
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

            if (!symbolInterval.GetPrevCandle(candle, out candle))
                return 0;
            candleCount--;
        }

        return surface;
    }

}
