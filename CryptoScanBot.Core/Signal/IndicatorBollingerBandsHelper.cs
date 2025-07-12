using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;

public static class IndicatorBollingerBandsHelper
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


    public static bool AboveBollingerBands(this CryptoCandle candle, bool useLowHigh)
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
