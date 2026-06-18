using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Shared calculations for the "Mean Reversion Bands" construction — volume-weighted VWAP bands:
///   basis = VWMA(hlc3, Length)                                   (rolling VWAP, Skender GetVwma)
///   band  = Mult * vwStdev(hlc3, Length) + AtrMult * ATR(AtrLength)
///   upper = basis + band,  lower = basis - band
/// where vwStdev is the volume-weighted standard deviation of hlc3 over the same window
/// (sqrt(E_w[hlc3^2] - E_w[hlc3]^2)). This is NOT Bollinger (no SMA of close, no plain stdev): it was
/// reverse-engineered from the reference chart and matches the green bands to ~pixel level.
/// The chart drawer (AtrRbBands) and the atrrb signal (SignalAtrRbLong/Short) both read these via
/// <see cref="ComputeBands"/>, so the chart and the alert stay in sync — change the parameters in
/// GlobalData.Settings.Signal.AtrRb and both follow. A break is simply a wick or close outside the band
/// (no lowest/highest filter; the signal supersede rule keeps only the latest break). The symmetric
/// slide ("glijbaan") detection lives here too.
/// </summary>
public static class AtrRbBandsHelper
{
    // Number of candles to feed the VWMA/vw-stdev/ATR calculation. Matches the signal pipeline window.
    private const int CalculationCandles = 260;

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
        var settings = GlobalData.Settings.Signal.AtrRb;
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
            srcQuotes.Add(new Quote { Date = c.Date, Close = hlc3, Volume = c.Volume });
            sqQuotes.Add(new Quote { Date = c.Date, Close = hlc3 * hlc3, Volume = c.Volume });
        }

        var vwmaSrc = (List<VwmaResult>)srcQuotes.GetVwma(settings.Length);
        var vwmaSq = (List<VwmaResult>)sqQuotes.GetVwma(settings.Length);
        var atrList = (List<AtrResult>)candles.GetAtr(settings.AtrLength);

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
    /// signal applies (StopLossAtrFactor * ATR%), <paramref name="lowerBand"/> the band value.
    /// </summary>
    public static bool IsLowerBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double pctDeviation, out double lowerBand)
    {
        pctDeviation = 0;
        lowerBand = 0;

        var settings = GlobalData.Settings.Signal.AtrRb;
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles);
        if (candles.Count < settings.Length + 1)
            return false;

        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            return false;

        var bands = ComputeBands(candles);
        var slAtrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        return LowerBandBreakAt(candles, bands, slAtrList, idx, out pctDeviation, out lowerBand);
    }

    /// <summary>
    /// Returns true when the candle at <paramref name="openTime"/> breaks above the upper band — a wick
    /// (High) or the Close sitting over it. See <see cref="IsLowerBandBreak"/>.
    /// </summary>
    public static bool IsUpperBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double pctDeviation, out double upperBand)
    {
        pctDeviation = 0;
        upperBand = 0;

        var settings = GlobalData.Settings.Signal.AtrRb;
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles);
        if (candles.Count < settings.Length + 1)
            return false;

        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            return false;

        var bands = ComputeBands(candles);
        var slAtrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        return UpperBandBreakAt(candles, bands, slAtrList, idx, out pctDeviation, out upperBand);
    }

    /// <summary>
    /// Returns the LOWER band value at <paramref name="openTime"/> without the break condition — used by
    /// the delayed entry to read the band of the candle after the signal. <paramref name="pctDeviation"/>
    /// is the SL distance % (factor * ATR%) at that candle.
    /// </summary>
    public static bool TryGetLowerBand(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double lowerBand, out double pctDeviation)
    {
        (lowerBand, pctDeviation) = (0, 0);
        var settings = GlobalData.Settings.Signal.AtrRb;
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles);
        if (candles.Count < settings.Length + 1)
            return false;
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            return false;

        var bands = ComputeBands(candles);
        var slAtrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        if (!TryBand(bands, idx, out _, out lowerBand))
            return false;
        pctDeviation = StopLossPercent(slAtrList, candles, idx);
        return true;
    }

    /// <summary>Returns the UPPER band value at <paramref name="openTime"/>. See <see cref="TryGetLowerBand"/>.</summary>
    public static bool TryGetUpperBand(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double upperBand, out double pctDeviation)
    {
        (upperBand, pctDeviation) = (0, 0);
        var settings = GlobalData.Settings.Signal.AtrRb;
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles);
        if (candles.Count < settings.Length + 1)
            return false;
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            return false;

        var bands = ComputeBands(candles);
        var slAtrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        if (!TryBand(bands, idx, out upperBand, out _))
            return false;
        pctDeviation = StopLossPercent(slAtrList, candles, idx);
        return true;
    }

    /// <summary>
    /// Reads the pre-computed upper/lower band at <paramref name="idx"/> from a <see cref="ComputeBands"/>
    /// result. Returns false while still in the indicator warm-up (no value yet).
    /// </summary>
    public static bool TryBand(IReadOnlyList<BandValue> bands, int idx, out double upperBand, out double lowerBand)
    {
        upperBand = 0;
        lowerBand = 0;
        if (idx < 0 || idx >= bands.Count || !bands[idx].HasValue)
            return false;

        upperBand = bands[idx].Upper;
        lowerBand = bands[idx].Lower;
        return true;
    }

    /// <summary>
    /// Core lower-band-break test against a candle list with pre-computed Bollinger/ATR. A break is a
    /// wick (Low) OR Close below the lower band. Computing the indicators once and scanning each index is
    /// far cheaper than re-deriving them per candle when a whole window has to be evaluated.
    /// </summary>
    public static bool LowerBandBreakAt(IReadOnlyList<CryptoCandle> candles,
        IReadOnlyList<BandValue> bands, IReadOnlyList<AtrResult> slAtrList, int idx,
        out double pctDeviation, out double lowerBand)
    {
        pctDeviation = 0;
        if (!TryBand(bands, idx, out _, out lowerBand))
            return false;

        if ((double)candles[idx].Low >= lowerBand && (double)candles[idx].Close >= lowerBand)
            return false;

        pctDeviation = StopLossPercent(slAtrList, candles, idx);
        return true;
    }

    /// <summary>Core upper-band-break test. See <see cref="LowerBandBreakAt"/>.</summary>
    public static bool UpperBandBreakAt(IReadOnlyList<CryptoCandle> candles,
        IReadOnlyList<BandValue> bands, IReadOnlyList<AtrResult> slAtrList, int idx,
        out double pctDeviation, out double upperBand)
    {
        pctDeviation = 0;
        if (!TryBand(bands, idx, out upperBand, out _))
            return false;

        if ((double)candles[idx].High <= upperBand && (double)candles[idx].Close <= upperBand)
            return false;

        pctDeviation = StopLossPercent(slAtrList, candles, idx);
        return true;
    }

    /// <summary>
    /// Stop-loss distance the signal/label reports: StopLossAtrFactor * ATR(Length)%. The SL uses the
    /// SLOW ATR over the band Length (not the fast AtrLength used to shape the band), so the percentage
    /// stays stable through a volatile rally — matching the reference chart's break labels (~0.85%)
    /// instead of spiking with the fast ATR.
    /// </summary>
    private static double StopLossPercent(IReadOnlyList<AtrResult> slAtrList, IReadOnlyList<CryptoCandle> candles, int idx)
    {
        double? atr = (idx >= 0 && idx < slAtrList.Count) ? slAtrList[idx].Atr : null;
        if (!atr.HasValue)
            return 0;
        return GlobalData.Settings.Signal.AtrRb.StopLossAtrFactor * (atr.Value / (double)candles[idx].Close * 100);
    }

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

        var settings = GlobalData.Settings.Signal.AtrRb;
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
