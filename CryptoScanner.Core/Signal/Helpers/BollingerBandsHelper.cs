using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class BollingerBandsHelper
{

    public static bool CheckBollingerBandsWidth(this MyData data, double minValue, double maxValue)
    {
        double boundary = minValue;
        if (boundary > 0 && data.CandleData!.BollingerBandsPercentage <= boundary)
            return false;

        boundary = maxValue;
        if (boundary > 0 && data.CandleData!.BollingerBandsPercentage >= boundary)
            return false;

        return true;
    }


    public static bool IsBelowBollingerBands(this MyData data, bool useLowHigh)
    {
        // Opens or closes below the bollinger band
        decimal value;
        if (useLowHigh)
            value = data.Candle.Low;
        else
            value = Math.Min(data.Candle.Open, data.Candle.Close);
        double? band = data.CandleData!.Sma20 - data.CandleData.BollingerBandsDeviation;
        if (band.HasValue && value <= (decimal)band)
            return true;
        return false;
    }


    public static bool IsAboveBollingerBands(this MyData data, bool useLowHigh)
    {
        // Opens or closes above the bollinger band
        decimal value;
        if (useLowHigh)
            value = data.Candle.High;
        else
            value = Math.Max(data.Candle.Open, data.Candle.Close);
        double? band = data.CandleData!.Sma20 + data.CandleData.BollingerBandsDeviation;
        if (band.HasValue && value >= (decimal)band)
            return true;
        return false;
    }
}
