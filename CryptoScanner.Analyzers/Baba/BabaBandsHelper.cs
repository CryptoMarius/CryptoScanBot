using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Baba;

/// <summary>
/// Shared calculations for the "Mean Reversion Bands" construction — volume-weighted VWAP bands:
///   basis = VWMA(hlc3, Length)                                   (rolling VWAP, Skender GetVwma)
///   band  = Mult * vwStdev(hlc3, Length) + AtrMult * ATR(AtrLength)
///   upper = basis + band,  lower = basis - band
/// where vwStdev is the volume-weighted standard deviation of hlc3 over the same window
/// (sqrt(E_w[hlc3^2] - E_w[hlc3]^2)). This is NOT Bollinger (no SMA of close, no plain stdev): it was
/// reverse-engineered from the reference chart and matches the green bands to ~pixel level.
/// The chart drawer (BabaBands) still draws a whole history in one go via <see cref="ComputeBands"/>.
/// The signal (SignalBabaLong/Short) no longer recomputes the window itself: IndicatorEngine (via
/// IntervalIndicatorHub or the batch path) computes the SAME band once per candle and stores it on
/// CryptoData.BabaBasis/Upper/Lower/BabaAtrSl, shared by both sides — so a candle with both a long and a
/// short check active only pays for the VWMA/ATR once, not twice. A break is simply a wick or close
/// outside the band (no lowest/highest filter; the signal supersede rule keeps only the latest break).
/// The symmetric slide ("glijbaan") detection lives here too.
/// </summary>
public static class BabaBandsHelper
{
    /// <summary>One candle's band values; <see cref="HasValue"/> is false during the indicator warm-up.</summary>
    public readonly struct BandValue
    {
        public readonly bool HasValue;
        public readonly double Basis;    // VWMA(hlc3, Length)
        public readonly double Upper;    // basis + Mult * vwStdev + AtrMult * ATR
        public readonly double Lower;    // basis - Mult * vwStdev - AtrMult * ATR
        public readonly double VwStdev;  // volume-weighted stdev of hlc3 — used for SL in vwStdev units

        public BandValue(double basis, double upper, double lower, double vwStdev)
        {
            HasValue = true;
            Basis = basis;
            Upper = upper;
            Lower = lower;
            VwStdev = vwStdev;
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
        var settings = BabaPlugin.Settings;
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
        var atrList = (IReadOnlyList<AtrResult>)candles.AsQuotes().ToAtr(settings.AtrLength);

        // Rolling SMA of volume over the band Length, used by the optional volume-surge widening below.
        // Prefix sum keeps it O(n); surge itself only looks back a few (VolumeSurgeLength) bars.
        double[]? volAvg = null;
        if (settings.UseVolumeSurge)
        {
            volAvg = new double[n];
            double sum = 0;
            for (int i = 0; i < n; i++)
            {
                sum += (double)candles[i].Volume;
                if (i >= settings.Length)
                    sum -= (double)candles[i - settings.Length].Volume;
                int count = Math.Min(i + 1, settings.Length);
                volAvg[i] = count > 0 ? sum / count : 0;
            }
        }

        for (int i = 0; i < n; i++)
        {
            double? mean = vwmaSrc[i].Vwma;
            double? second = vwmaSq[i].Vwma;
            if (!mean.HasValue || !second.HasValue)
                continue;

            double variance = second.Value - mean.Value * mean.Value;
            double vwStdev = variance > 0 ? Math.Sqrt(variance) : 0;

            // Effective sigma-multiplier: Mult, plus the optional volume-surge widening. surge = max
            // volume over the last VolumeSurgeLength bars / SMA(volume, Length). Normal volume
            // (surge <= threshold) contributes nothing, so the pure VWAP band is unchanged.
            double mult = settings.Mult;
            if (settings.UseVolumeSurge && volAvg![i] > 0)
            {
                double maxVol = 0;
                int back = Math.Min(settings.VolumeSurgeLength, i + 1);
                for (int j = 0; j < back; j++)
                    maxVol = Math.Max(maxVol, (double)candles[i - j].Volume);
                double surge = maxVol / volAvg[i];
                mult += settings.VolumeSurgeFactor * Math.Max(0, surge - settings.VolumeSurgeThreshold);
            }

            double pad = mult * vwStdev + settings.AtrMult * (atrList[i].Atr ?? 0);
            result[i] = new BandValue(mean.Value, mean.Value + pad, mean.Value - pad, vwStdev);
        }
        return result;
    }

    //public static bool IsLowerBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
    //    out double pctDeviation, out double lowerBand)
    //{
    //    pctDeviation = 0;
    //    lowerBand = 0;

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.BabaLower is not double band)
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

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.BabaUpper is not double band)
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

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.BabaLower is not double band)
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

    //    if (!symbolInterval.TryGetCandle(openTime, out MyData? data) || data == null || data.CandleData.BabaUpper is not double band)
    //        return false;

    //    upperBand = band;
    //    pctDeviation = StopLossPercent(data.CandleData, data.Candle);
    //    return true;
    //}

    //private static double StopLossPercent(CryptoData data, CryptoCandle candle)
    //{
    //    if (data.BabaAtrSl is not double atr)
    //        return 0;
    //    return BabaPlugin.Settings.StopLossAtrFactor * (atr / (double)candle.Close * 100);
    //}

    /// <summary>
    /// Symmetric slide ("glijbaan") detection at <paramref name="openTime"/> using the Kaufman efficiency
    /// ratio over SlideWindow bars: efficiency = |net change| / (sum of absolute bar-to-bar moves).
    /// A high efficiency + a real net move = an orderly one-way slide.
    ///   <paramref name="slidingDown"/> = an efficient DOWN move (suppress longs),
    ///   <paramref name="slidingUp"/>   = an efficient UP move   (suppress shorts).
    /// </summary>
    public static void ComputeSlide(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out bool slidingDown, out bool slidingUp)
    {
        slidingDown = false;
        slidingUp = false;

        var settings = BabaPlugin.Settings;
        int window = settings.SlideWindow;
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(window + 2, symbolInterval.Interval.Duration);
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < window)
            return;

        double closeNow = (double)candles[idx].Close;
        double closeThen = (double)candles[idx - window].Close;
        if (closeThen == 0)
            return;

        double change = closeNow - closeThen;          // negative = down over the window
        double path = 0;
        for (int j = idx - window + 1; j <= idx; j++)
            path += Math.Abs((double)candles[j].Close - (double)candles[j - 1].Close);

        double efficiency = path > 0 ? Math.Abs(change) / path : 0;
        double movePct = Math.Abs(change) / closeThen * 100.0;

        bool qualifies = efficiency >= settings.SlideMinEfficiency && movePct >= settings.SlideMinMovePercent;
        slidingDown = qualifies && change < 0;
        slidingUp = qualifies && change > 0;
    }
}
