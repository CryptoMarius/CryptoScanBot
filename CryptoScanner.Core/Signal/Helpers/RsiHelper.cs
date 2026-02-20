using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class RsiHelper
{
    public static bool RsiOversold(this MyData candle, int correction = 0)
    {
        if (candle.CandleData?.Rsi > GlobalData.Settings.General.SettingsRsi.Oversold - correction)
            return false;
        return true;
    }


    public static bool RsiOverbought(this MyData candle, int correction = 0)
    {
        if (candle.CandleData?.Rsi < GlobalData.Settings.General.SettingsRsi.Overbought + correction)
            return false;
        return true;
    }


    /// <summary>
    /// Calculate the Rsi surface area of the overbought part from limit to rsi
    /// </summary>
    public static double RsiOverboughtSurface(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candle, int candleCount, double limit)
    {
        double surface = 0;
        while (candleCount > 0)
        {
            if (candle == null || candle!.CandleData == null || candle.CandleData.Rsi == null) // not calculated, exit!
                return 0;

            double result = candle.CandleData.Rsi.Value - limit;
            if (result > 0)
                surface += result;

            // stop if almost halfway
            if (candle.CandleData.Rsi.Value < 60)
                break;

            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle, out candle))
                return 0;
            candleCount--;
        }

        return surface;
    }



    /// <summary>
    /// Calculate the Rsi surface area of the oversold part from limit to rsi
    /// </summary>
    public static double RsiOversoldSurface(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candle, int candleCount, double limit)
    {
        double surface = 0;
        while (candleCount > 0)
        {
            if (candle == null || candle!.CandleData == null || candle.CandleData.Rsi == null) // not calculated, exit!
                return 0;

            double result = limit - candle.CandleData.Rsi.Value;
            if (result > 0)
                surface += result;

            // stop if almost halfway
            if (candle.CandleData.Rsi.Value > 40)
                break;

            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle, out candle))
                return 0;
            candleCount--;
        }

        return surface;
    }



    ///// <summary>
    ///// Is de RSI oversold geweest in de laatste x candles
    ///// </summary>
    //public static bool WasRsiOversoldInTheLast(this CryptoSymbolInterval symbolInterval, CryptoCandle? candle, int candleCount = 30)
    //{
    //    // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
    //    while (candleCount >= 0)
    //    {
    //        if (candle is not null && candle.RsiOversold())
    //            return true;

    //        if (!symbolInterval.GetPrevCandle(candle, out candle))
    //            return false;
    //        candleCount--;
    //    }
    //    return false;
    //}


    ///// <summary>
    ///// Is de RSI overbought geweest in de laatste x candles
    ///// </summary>
    //public static bool WasRsiOverboughtInTheLast(this CryptoSymbolInterval symbolInterval, CryptoCandle? candle, int candleCount = 30)
    //{
    //    // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
    //    while (candleCount >= 0)
    //    {
    //        if (candle is not null && candle.RsiOverbought())
    //            return true;

    //        if (!symbolInterval.GetPrevCandle(candle, out candle))
    //            return false;
    //        candleCount--;
    //    }
    //    return false;
    //}


    /// <summary>
    /// Is de RSI oplopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public static bool RsiIncreasingInTheLast(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candle, int candleCount, int allowedDown)
    {
        // from right to left
        int down = 0;
        bool first = true;
        // En van de candles daarvoor mag er een (of meer) afwijken
        while (candleCount > 0)
        {
            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle!, out MyData? prev))
                return false;
            if (prev!.CandleData == null || prev.CandleData.Rsi == null)
                return false;

            if (candle?.CandleData?.Rsi <= prev?.CandleData?.Rsi)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            candle = prev;
            candleCount--;
            first = false;
        }

        return true;
    }


    /// <summary>
    /// Is de RSI aflopend in de laatste x candles
    /// 2e parameter geeft aan hoeveel afwijkend mogen zijn
    /// </summary>
    public static bool RsiDecreasingInTheLast(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candle, int candleCount, int allowedDown)
    {
        // We gaan van rechts naar links (van de nieuwste candle richting verleden)
        int down = 0;
        bool first = true;
        while (candleCount > 0)
        {
            if (!myBase.GetPrevCandle(symbolInterval.Interval, candle!, out MyData? prev))
                return false;
            if (prev!.CandleData == null || prev.CandleData.Rsi == null)
                return false;

            if (candle?.CandleData?.Rsi >= prev!.CandleData?.Rsi)
            {
                down++;
                if (first || down > allowedDown)
                    return false;
            }

            candle = prev;
            candleCount--;
            first = false;
        }

        return true;
    }

}
