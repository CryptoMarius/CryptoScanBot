using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.BbSqueeze.Signal;

public class SignalBbSqueezeBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.BollingerBandsPercentage == null
           )
            return false;

        if (BbSqueezePlugin.Settings.UseMacdFilter && data.CandleData.MacdHistogram == null)
            return false;

        return true;
    }


    /// <summary>
    /// Check whether the BB stayed squeezed for the required number of candles
    /// </summary>
    protected bool WasSqueezed(int minCandles, double maxPercentage)
    {
        MyData? current = CandleLast!;
        int count = 0;

        while (count < minCandles)
        {
            if (!GetPrevCandle(current, out MyData? prev))
                return false;

            if (prev!.CandleData?.BollingerBandsPercentage == null)
                return false;

            if (prev.CandleData.BollingerBandsPercentage > maxPercentage)
                return false;

            current = prev;
            count++;
        }

        return true;
    }


    /// <summary>
    /// Check whether the MACD histogram has been rising for the required number of candles
    /// </summary>
    protected bool IsMacdHistogramRising(int confirmCandles)
    {
        MyData current = CandleLast!;

        for (int i = 0; i < confirmCandles; i++)
        {
            if (!GetPrevCandle(current, out MyData? prev))
                return false;

            if (current.CandleData!.MacdHistogram <= prev!.CandleData!.MacdHistogram)
                return false;

            current = prev;
        }

        return true;
    }


    /// <summary>
    /// Check whether the MACD histogram has been falling for the required number of candles
    /// </summary>
    protected bool IsMacdHistogramFalling(int confirmCandles)
    {
        MyData current = CandleLast!;

        for (int i = 0; i < confirmCandles; i++)
        {
            if (!GetPrevCandle(current, out MyData? prev))
                return false;

            if (current.CandleData!.MacdHistogram >= prev!.CandleData!.MacdHistogram)
                return false;

            current = prev;
        }

        return true;
    }


    /// <summary>
    /// Check whether current volume exceeds multiplier x SMA(volume, length)
    /// </summary>
    protected bool IsVolumeSpike(double multiplier, int smaLength)
    {
        MyData? current = CandleLast!;
        double totalVolume = 0;
        int count = 0;

        for (int i = 0; i < smaLength; i++)
        {
            if (!GetPrevCandle(current, out MyData? prev))
                return false;

            totalVolume += (double)prev!.Candle.Volume;
            current = prev;
            count++;
        }

        if (count == 0)
            return false;

        double avgVolume = totalVolume / count;
        return (double)CandleLast.Candle.Volume > avgVolume * multiplier;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (base.GiveUp(signal))
            return true;

        var settings = BbSqueezePlugin.Settings;

        // Skip the re-squeeze check during the grace period after the signal fired;
        // the bands need a few candles to really expand after the breakout candle.
        CandleTime signalTime = CandleTime.FromDateTime(signal.OpenDate);
        CandleTime graceEnd = signalTime + settings.ReSqueezeGraceCandles * signal.Interval.Duration;
        if (CandleLast.Candle.OpenTime <= graceEnd)
        {
            ExtraText = "";
            return false;
        }

        // Give up when the BB width collapses again (re-squeeze) after the breakout
        if (CandleLast?.CandleData?.BollingerBandsPercentage <= settings.BBSqueezeMaxPercentage)
        {
            ExtraText = "BB re-squeezed after breakout";
            return true;
        }

        ExtraText = "";
        return false;
    }
}
