using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.KumoSqueeze.Signal;

public class KumoSqueezeSignalBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.BollingerBandsPercentage == null
           || data.CandleData.Rsi == null
           )
            return false;

        if (KumoSqueezePlugin.Settings.UseMacdFilter && data.CandleData.MacdHistogram == null)
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
    /// Check whether current volume exceeds multiplier × SMA(volume, length)
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
    /// Compute Ichimoku and return the cloud values aligned with the current candle.
    /// Returns null when there is insufficient data.
    /// </summary>
    protected IchimokuResult? GetIchimokuCloud(int tenkanPeriods, int kijunPeriods, int senkouBPeriods)
    {
        List<IQuote>? quotes = IndicatorEngine.CollectCandles(Symbol, SymbolInterval.Interval, CandleLast.Candle.OpenTime, out string _);
        if (quotes == null)
            return null;

        IEnumerable<IchimokuResult> results = quotes.ToIchimoku(tenkanPeriods, kijunPeriods, senkouBPeriods);
        if (results == null || !results.Any())
            return null;

        // Senkou Span A/B are projected kijunPeriods forward; the cloud values that
        // align with the current candle sit at index (count - 1 - kijunPeriods).
        List<IchimokuResult> resultList = results.ToList();
        int cloudIndex = resultList.Count - 1 - kijunPeriods;
        if (cloudIndex < 0)
            return null;

        IchimokuResult cloud = resultList[cloudIndex];
        if (cloud.SenkouSpanA == null || cloud.SenkouSpanB == null)
            return null;

        return cloud;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (base.GiveUp(signal))
            return true;

        // Give up when the BB width collapses again (re-squeeze) after the breakout
        var settings = KumoSqueezePlugin.Settings;
        if (CandleLast?.CandleData?.BollingerBandsPercentage <= settings.BBSqueezeMaxPercentage)
        {
            ExtraText = "BB re-squeezed after breakout";
            return true;
        }

        ExtraText = "";
        return false;
    }
}
