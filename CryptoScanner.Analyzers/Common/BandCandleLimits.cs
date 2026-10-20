using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.Common;

/// <summary>
/// The candle limits of the band strategies (dbr, vbs, atrrb): skip a band break on a candle that is
/// far taller than normal, or that carries far more volume than normal. Such a candle is an outside
/// blow (news, liquidations) more often than a band break that springs back. Measured on dbr first
/// (15-09-2026, see DbrSettings.MaxCandleSizeRatio); the same rule is offered to the other two.
/// </summary>
public static class BandCandleLimits
{
    /// <summary>
    /// The verdict on one candle: blocked with a reason, or allowed, possibly with an entry price
    /// further out (the retracement on a large candle).
    /// </summary>
    public readonly record struct Verdict(bool Blocked, string Reason, decimal? EntryPrice)
    {
        public static readonly Verdict Allowed = new(false, "", null);
    }


    /// <summary>
    /// Judges the candle at <paramref name="openTime"/>: too tall compared with the previous
    /// <paramref name="window"/> candles, or carrying too much volume compared with the same window.
    /// Both limits are off at 0, and then this costs nothing: it returns before touching the candle
    /// list.
    /// <para>
    /// A candle over the SIZE limit is dropped, unless <paramref name="retracementPart"/> is above 0 -
    /// then the signal is kept and handed an entry price that far into the candle beyond its close,
    /// so a long buys lower and a short sells higher. That price only reaches the exchange when the
    /// entry is placed as a limit order; a market order ignores it. A candle over the VOLUME limit is
    /// always dropped: no retracement was measured for that one.
    /// </para>
    /// <para>
    /// Size is (high - low) as a percentage of the close per candle, so a cheap coin and an expensive
    /// one are on the same scale, and the average is taken over those percentages. Too few candles to
    /// judge means no judgement - the caller keeps the signal, exactly as it would without the limits.
    /// </para>
    /// </summary>
    public static Verdict Check(CryptoSymbolInterval symbolInterval, CandleTime openTime, bool isLong,
        double maxSizeRatio, double maxVolumeRatio, int window, double retracementPart)
    {
        bool checkSize = maxSizeRatio > 0;
        bool checkVolume = maxVolumeRatio > 0;
        if (!checkSize && !checkVolume)
            return Verdict.Allowed;
        if (window <= 0)
            return Verdict.Allowed;

        // Thread-safe ascending snapshot; one more than the window, for the breaking candle itself.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(window + 1, symbolInterval.Interval.Duration);
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            idx = candles.Count - 1;
        if (idx < window)
            return Verdict.Allowed;

        CryptoCandle candle = candles[idx];

        if (checkSize)
        {
            double sizeSum = 0;
            for (int i = idx - window; i < idx; i++)
                sizeSum += CandleSizePercentage(candles[i]);
            double average = sizeSum / window;
            if (average > 0)
            {
                double ratio = CandleSizePercentage(candle) / average;
                if (ratio > maxSizeRatio)
                {
                    if (retracementPart > 0)
                    {
                        // A part of the candle's own height beyond the close, in the direction the
                        // candle was already going: further down for a long, further up for a short.
                        decimal height = candle.High - candle.Low;
                        decimal offset = height * (decimal)retracementPart;
                        decimal price = isLong ? candle.Close - offset : candle.Close + offset;
                        if (price > 0)
                            return new Verdict(false, "", price);
                    }
                    return new Verdict(true, $"candle {ratio:N1}x the average size (max {maxSizeRatio:N1})", null);
                }
            }
        }

        if (checkVolume)
        {
            double volumeSum = 0;
            for (int i = idx - window; i < idx; i++)
                volumeSum += (double)candles[i].Volume;
            double average = volumeSum / window;
            if (average > 0)
            {
                double ratio = (double)candle.Volume / average;
                if (ratio > maxVolumeRatio)
                    return new Verdict(true, $"candle {ratio:N1}x the average volume (max {maxVolumeRatio:N1})", null);
            }
        }

        return Verdict.Allowed;
    }


    /// <summary>High minus low as a percentage of the close, 0 when the close is missing.</summary>
    public static double CandleSizePercentage(CryptoCandle candle)
    {
        if (candle.Close == 0)
            return 0;
        return (double)(100m * (candle.High - candle.Low) / candle.Close);
    }
}
