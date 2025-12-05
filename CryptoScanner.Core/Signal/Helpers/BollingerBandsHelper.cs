using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class BollingerBandsHelper
{

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
        // Opens or closes below the bollinger band
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
        // Opens or closes above the bollinger band
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
