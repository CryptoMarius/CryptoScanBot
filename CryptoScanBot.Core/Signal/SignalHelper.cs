using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;

public static class SignalHelper
{
    public static bool IsStochOversold(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten kleiner zijn dan 20% (oversold)
        if (candle.CandleData?.StochSignal > GlobalData.Settings.General.StochValueOversold - correction)
            return false;
        if (candle.CandleData?.StochOscillator > GlobalData.Settings.General.StochValueOversold - correction)
            return false;
        return true;
    }


    public static bool IsRsiOversold(this CryptoCandle candle, int correction = 0)
    {
        if (candle.CandleData?.Rsi > GlobalData.Settings.General.RsiValueOversold - correction)
            return false;
        return true;
    }

    public static bool IsStochOverbought(this CryptoCandle candle, int correction = 0)
    {
        // Stochastic Oscillator: K en D (langzaam) moeten groter zijn dan 80% (overbought)
        if (candle.CandleData?.StochSignal < GlobalData.Settings.General.StochValueOverbought + correction)
            return false;
        if (candle.CandleData?.StochOscillator < GlobalData.Settings.General.StochValueOverbought + correction)
            return false;
        return true;
    }


    public static bool IsRsiOverbought(this CryptoCandle candle, int correction = 0)
    {
        if (candle.CandleData?.Rsi < GlobalData.Settings.General.RsiValueOverbought + correction)
            return false;
        return true;
    }


    public static bool CheckBollingerBandsWidth(this CryptoCandle candle, double minValue, double maxValue)
    {
        double boundary = minValue;
        if (boundary > 0 && candle.CandleData!.BollingerBandsPercentage <= boundary)
            return false;

        boundary = maxValue;
        if (boundary > 0 && candle.CandleData!.BollingerBandsPercentage >= boundary)
            return false;

        return true;
    }


    public static bool IsBelowBollingerBands(this CryptoCandle candle, bool useLowHigh)
    {
        // Geopend of gesloten onder de bollinger band
        decimal value;
        if (useLowHigh)
            value = candle.Low;
        else
            value = Math.Min(candle.Open, candle.Close);
        double? band = candle.CandleData!.Sma20 - candle.CandleData.BollingerBandsDeviation;
        if (band.HasValue && value <= (decimal)band)
            return true;
        return false;
    }


    public static bool IsAboveBollingerBands(this CryptoCandle candle, bool useLowHigh)
    {
        // Geopend of gesloten boven de bollinger band
        decimal value;
        if (useLowHigh)
            value = candle.High;
        else
            value = Math.Max(candle.Open, candle.Close);
        double? band = candle.CandleData!.Sma20 + candle.CandleData.BollingerBandsDeviation;
        if (band.HasValue && value >= (decimal)band)
            return true;
        return false;
    }
}