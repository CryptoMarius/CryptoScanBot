using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Baba;

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
        public readonly double Basis;   // VWMA(hlc3, Length)
        public readonly double Upper;   // basis + Mult * vwStdev + AtrMult * ATR
        public readonly double Lower;   // basis - Mult * vwStdev - AtrMult * ATR

        public BandValue(double basis, double upper, double lower)
        {
            HasValue = true;
            Basis = basis;
            Upper = upper;
            Lower = lower;
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
        var settings = GlobalData.Settings.Signal.Baba;
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

        for (int i = 0; i < n; i++)
        {
            double? mean = vwmaSrc[i].Vwma;
            double? second = vwmaSq[i].Vwma;
            if (!mean.HasValue || !second.HasValue)
                continue;

            double variance = second.Value - mean.Value * mean.Value;
            double vwStdev = variance > 0 ? Math.Sqrt(variance) : 0;
            double pad = settings.Mult * vwStdev + settings.AtrMult * (atrList[i].Atr ?? 0);
            result[i] = new BandValue(mean.Value, mean.Value + pad, mean.Value - pad);
        }
        return result;
    }

    /// <summary>
    /// Returns true when the candle at <paramref name="openTime"/> breaks below the lower band — a wick
    /// (Low) or the Close sitting under it. <paramref name="pctDeviation"/> is the stop-loss distance the
    /// signal applies (StopLossAtrFactor * ATR%), <paramref name="lowerBand"/> the band value. Reads the
    /// band IndicatorEngine already computed for this candle (CryptoSymbolInterval.Data) instead of
    /// recomputing the VWMA/ATR window — so a long AND a short check on the same candle share one calculation.
    /// </summary>
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

    ///// <summary>
    ///// Returns true when the candle at <paramref name="openTime"/> breaks above the upper band — a wick
    ///// (High) or the Close sitting over it. See <see cref="IsLowerBandBreak"/>.
    ///// </summary>
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

    ///// <summary>
    ///// Returns the LOWER band value at <paramref name="openTime"/> without the break condition — used by
    ///// the delayed entry to read the band of the candle after the signal. <paramref name="pctDeviation"/>
    ///// is the SL distance % (factor * ATR%) at that candle.
    ///// </summary>
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

    ///// <summary>Returns the UPPER band value at <paramref name="openTime"/>. See <see cref="TryGetLowerBand"/>.</summary>
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

    ///// <summary>
    ///// Stop-loss distance the signal/label reports: StopLossAtrFactor * ATR(Length)%, from the precomputed
    ///// SLOW ATR over the band Length (CryptoData.BabaAtrSl — not the fast AtrLength used to shape the
    ///// band), so the percentage stays stable through a volatile rally — matching the reference chart's
    ///// break labels (~0.85%) instead of spiking with the fast ATR.
    ///// </summary>
    //private static double StopLossPercent(CryptoData data, CryptoCandle candle)
    //{
    //    if (data.BabaAtrSl is not double atr)
    //        return 0;
    //    return GlobalData.Settings.Signal.Baba.StopLossAtrFactor * (atr / (double)candle.Close * 100);
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

        var settings = GlobalData.Settings.Signal.Baba;
        int window = settings.SlideWindow;
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(window + 2);
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
