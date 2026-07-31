using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Vbs;

/// <summary>
/// Shared calculations for the "Mean Reversion Bands" construction — volume-weighted VWAP bands:
///   basis = VWMA(hlc3, Length)                                   (rolling VWAP, Skender GetVwma)
///   band  = Mult * vwStdev(hlc3, Length)
///   upper = basis + band,  lower = basis - band
/// where vwStdev is the volume-weighted standard deviation of hlc3 over the same window
/// (sqrt(E_w[hlc3^2] - E_w[hlc3]^2)). This is NOT Bollinger (no SMA of close, no plain stdev): it was
/// reverse-engineered from the reference chart and matches the green bands to ~pixel level.
/// The chart drawer (VbsBands) still draws a whole history in one go via <see cref="ComputeBands"/>.
/// The signal (VbsSignalLong/Short) no longer recomputes the window itself: IndicatorEngine (via
/// IntervalIndicatorHub or the batch path) computes the SAME band once per candle and stores it on
/// CryptoData.VbsBasis/Upper/Lower/VbsAtrSl, shared by both sides — so a candle with both a long and a
/// short check active only pays for the VWMA/ATR once, not twice. A break is simply a wick or close
/// outside the band (no lowest/highest filter; the signal supersede rule keeps only the latest break).
/// A break is simply a wick or close outside the band.
/// </summary>
public static class VbsBandsHelper
{
    /// <summary>One candle's band values; <see cref="HasValue"/> is false during the indicator warm-up.</summary>
    public readonly struct BandValue
    {
        public readonly bool HasValue;
        public readonly double Basis;    // VWMA(hlc3, Length)
        public readonly double Upper;    // basis + Mult * vwStdev
        public readonly double Lower;    // basis - Mult * vwStdev
        public readonly double VwStdev;  // volume-weighted stdev of hlc3
        public readonly double Acs;      // Average Candle Size %: AcsFactor * SMA((high-low)/close, AcsLength) * 100

        public BandValue(double basis, double upper, double lower, double vwStdev, double acs)
        {
            HasValue = true;
            Basis = basis;
            Upper = upper;
            Lower = lower;
            VwStdev = vwStdev;
            Acs = acs;
        }
    }

    /// <summary>
    /// Computes the volume-weighted VWAP band (basis/upper/lower) for every candle in <paramref name="candles"/>,
    /// index-aligned with the input list (so result[i] belongs to candles[i]). Skender's volume-weighted
    /// GetVwma is reused twice — once on hlc3 (the basis) and once on hlc3^2 (the second moment) — so the
    /// volume-weighted variance is E_w[hlc3^2] - E_w[hlc3]^2. The single source of truth for both the chart
    /// and the signal.
    /// </summary>
    public static BandValue[] ComputeBands(IReadOnlyList<CryptoCandle> candles)
    {
        var settings = VbsPlugin.Settings;
        int n = candles.Count;
        var result = new BandValue[n];
        if (n == 0)
            return result;

        // hlc3 and hlc3^2 carried as volume-bearing Skender quotes; GetVwma then gives the volume-weighted
        // mean of each. hlc3^2 needs decimal headroom (price^2 overflows the tick-int storage of CryptoCandle),
        // which is why we build Skender Quote objects instead of reusing the candle struct.
        var srcQuotes = new List<Quote>(n);
        var sqQuotes = new List<Quote>(n);
        foreach (var c in candles)
        {
            decimal hlc3 = (c.High + c.Low + c.Close) / 3m;
            // v3 Quote is a positional record (Timestamp, Open, High, Low, Close, Volume); GetVwma only
            // uses Close + Volume, so Open/High/Low are left 0.
            srcQuotes.Add(new Quote(c.Timestamp, 0m, 0m, 0m, hlc3, c.Volume));
            sqQuotes.Add(new Quote(c.Timestamp, 0m, 0m, 0m, hlc3 * hlc3, c.Volume));
        }

        var vwmaSrc = (IReadOnlyList<VwmaResult>)srcQuotes.ToVwma(settings.Length);
        var vwmaSq = (IReadOnlyList<VwmaResult>)sqQuotes.ToVwma(settings.Length);

        // Per-candle range% = (high-low)/close*100, plus a prefix sum so the ACS (its SMA over AcsLength)
        // is an O(1) lookup per candle. ACS% = AcsFactor * SMA(range%, AcsLength).
        int acsLen = Math.Max(1, settings.AcsLength);
        var rangePct = new double[n];
        var prefix = new double[n + 1];
        for (int i = 0; i < n; i++)
        {
            double close = (double)candles[i].Close;
            rangePct[i] = close != 0 ? (double)(candles[i].High - candles[i].Low) / close * 100.0 : 0;
            prefix[i + 1] = prefix[i] + rangePct[i];
        }

        for (int i = 0; i < n; i++)
        {
            double? mean = vwmaSrc[i].Vwma;
            double? second = vwmaSq[i].Vwma;
            if (!mean.HasValue || !second.HasValue)
                continue;

            double variance = second.Value - mean.Value * mean.Value;
            double vwStdev = variance > 0 ? Math.Sqrt(variance) : 0;

            double pad = settings.Mult * vwStdev;

            double acs = 0;
            if (i + 1 >= acsLen)
                acs = settings.AcsFactor * (prefix[i + 1] - prefix[i + 1 - acsLen]) / acsLen;

            result[i] = new BandValue(mean.Value, mean.Value + pad, mean.Value - pad, vwStdev, acs);
        }
        return result;
    }

    //public static bool IsLowerBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
    //    out double pctDeviation, out double lowerBand)
    //{
    //    pctDeviation = 0;
    //    lowerBand = 0;

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.VbsLower is not double band)
    //        return false;

    //    lowerBand = band;
    //    if ((double)data.Candle.Low >= lowerBand && (double)data.Candle.Close >= lowerBand)
    //        return false;

    //    pctDeviation = StopLossPercent(data.CandleData, data.Candle);
    //    return true;
    //}

    //public static bool IsUpperBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
    //    out double pctDeviation, out double upperBand)
    //{
    //    pctDeviation = 0;
    //    upperBand = 0;

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.VbsUpper is not double band)
    //        return false;

    //    upperBand = band;
    //    if ((double)data.Candle.High <= upperBand && (double)data.Candle.Close <= upperBand)
    //        return false;

    //    pctDeviation = StopLossPercent(data.CandleData, data.Candle);
    //    return true;
    //}

    //public static bool TryGetLowerBand(CryptoSymbolInterval symbolInterval, CandleTime openTime,
    //    out double lowerBand, out double pctDeviation)
    //{
    //    lowerBand = 0;
    //    pctDeviation = 0;

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.VbsLower is not double band)
    //        return false;

    //    lowerBand = band;
    //    pctDeviation = StopLossPercent(data.CandleData, data.Candle);
    //    return true;
    //}

    //public static bool TryGetUpperBand(CryptoSymbolInterval symbolInterval, CandleTime openTime,
    //    out double upperBand, out double pctDeviation)
    //{
    //    upperBand = 0;
    //    pctDeviation = 0;

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.VbsUpper is not double band)
    //        return false;

    //    upperBand = band;
    //    pctDeviation = StopLossPercent(data.CandleData, data.Candle);
    //    return true;
    //}

    //private static double StopLossPercent(CryptoData data, CryptoCandle candle)
    //{
    //    if (data.VbsAtrSl is not double atr)
    //        return 0;
    //    return VbsPlugin.Settings.StopLossAtrFactor * (atr / (double)candle.Close * 100);
    //}

}
