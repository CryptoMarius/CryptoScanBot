using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.AtrRb.Signal;

/// <summary>
/// Shared parameters and calculations for the "AtrRb Bands &amp; Ribbon" construction
/// (a Keltner-style EMA basis with ATR based bands). The chart drawer and the "settings"
/// signal algorithm both read these constants so they stay in sync — change them here
/// and both the chart and the alert follow.
/// </summary>
public static class AtrRbBandsHelper
{
    // The band parameters (Len / OuterMult / BreakLookback) now live in
    // AtrRbPlugin.Settings so the user can tune them; the chart drawer reads the same
    // settings, so chart and alert stay in sync.

    // Number of candles to feed the EMA/ATR calculation. Matches the signal pipeline window.
    private const int CalculationCandles = 260;

    /// <summary>
    /// Returns true when the candle at <paramref name="openTime"/> breaks below the macro lower
    /// band (EMA - ATR * OuterMult) and is the lowest Low within the trailing BreakLookback window.
    /// This mirrors exactly the lower-band label condition drawn on the chart.
    /// <paramref name="pctDeviation"/> is the percentage the Low sits below the basis,
    /// the same number printed as the chart label.
    /// </summary>
    public static bool IsLowerBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double pctDeviation, out double lowerBand)
    {
        pctDeviation = 0;
        lowerBand = 0;

        var settings = AtrRbPlugin.Settings;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles, symbolInterval.Interval.Duration);
        if (candles.Count < settings.Length + settings.BreakLookback)
            return false;

        // Locate the requested (just-closed) candle; fall back to the most recent one.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            idx = candles.Count - 1;
        if (idx < settings.BreakLookback - 1)
            return false;

        // EMA(Len) basis and ATR(Len), computed exactly like the chart drawer.
        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        IReadOnlyList<EmaResult> emaList = quotes.ToEma(settings.Length);
        IReadOnlyList<AtrResult> atrList = quotes.ToAtr(settings.Length);

        return IsBreakAt(candles, emaList, atrList, idx, isLong: true, out pctDeviation, out lowerBand);
    }

    /// <summary>
    /// Returns true when the candle at <paramref name="openTime"/> breaks above the macro upper
    /// band (EMA + ATR * OuterMult) and is the highest High within the trailing BreakLookback window.
    /// This mirrors exactly the upper-band label condition drawn on the chart.
    /// <paramref name="pctDeviation"/> is the percentage the High sits above the basis,
    /// the same number printed as the chart label.
    /// </summary>
    public static bool IsUpperBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double pctDeviation, out double upperBand)
    {
        pctDeviation = 0;
        upperBand = 0;

        var settings = AtrRbPlugin.Settings;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles, symbolInterval.Interval.Duration);
        if (candles.Count < settings.Length + settings.BreakLookback)
            return false;

        // Locate the requested (just-closed) candle; fall back to the most recent one.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            idx = candles.Count - 1;
        if (idx < settings.BreakLookback - 1)
            return false;

        // EMA(Len) basis and ATR(Len), computed exactly like the chart drawer.
        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        IReadOnlyList<EmaResult> emaList = quotes.ToEma(settings.Length);
        IReadOnlyList<AtrResult> atrList = quotes.ToAtr(settings.Length);

        return IsBreakAt(candles, emaList, atrList, idx, isLong: false, out pctDeviation, out upperBand);
    }


    /// <summary>
    /// The band break on ONE index of an already computed EMA/ATR pair: the Low under the macro
    /// lower band (a long) or the High above the macro upper band (a short), and the most extreme
    /// value within the trailing BreakLookback window - the ta.lowest/ta.highest filter of the Pine
    /// script. Split out of the two methods above so a caller that wants to test several indices
    /// pays for the EMA and the ATR once instead of once per candle.
    /// </summary>
    public static bool IsBreakAt(List<CryptoCandle> candles, IReadOnlyList<EmaResult> emaList,
        IReadOnlyList<AtrResult> atrList, int idx, bool isLong, out double pctDeviation, out double bandPrice)
    {
        pctDeviation = 0;
        bandPrice = 0;

        var settings = AtrRbPlugin.Settings;
        if (idx < settings.BreakLookback - 1 || idx >= candles.Count)
            return false;

        double? basis = emaList[idx].Ema;
        double? atr = atrList[idx].Atr;
        if (!basis.HasValue || !atr.HasValue || basis.Value == 0)
            return false;

        if (isLong)
        {
            bandPrice = basis.Value - atr.Value * settings.OuterMult;

            double low = (double)candles[idx].Low;
            if (low >= bandPrice)
                return false;

            // Only fire on the lowest Low within the trailing window (matches ta.lowest filter).
            for (int j = idx - settings.BreakLookback + 1; j < idx; j++)
            {
                if ((double)candles[j].Low < low)
                    return false;
            }
        }
        else
        {
            bandPrice = basis.Value + atr.Value * settings.OuterMult;

            double high = (double)candles[idx].High;
            if (high <= bandPrice)
                return false;

            // Only fire on the highest High within the trailing window (matches ta.highest filter).
            for (int j = idx - settings.BreakLookback + 1; j < idx; j++)
            {
                if ((double)candles[j].High > high)
                    return false;
            }
        }

        //pctDeviation = (basis.Value - low) / basis.Value * 100;
        //pctDeviation = atr.Value / (double)candles[idx].Close * 100;
        pctDeviation = settings.StopLossAtrFactor * (atr.Value / (double)candles[idx].Close * 100);
        return true;
    }


    /// <summary>
    /// The most recent band break at or before <paramref name="openTime"/>, looking back at most
    /// <paramref name="withinCandles"/> candles (the candle at that time included). The EMA and the
    /// ATR are computed once for the whole snapshot, so the window costs a walk and not a recompute
    /// per candle. <paramref name="candlesAgo"/> is 0 when the break is on the candle itself.
    /// <para>
    /// This is the band break only - the RSI, stochastic and Bollinger-width filters of the AtrRb
    /// strategy are NOT replayed here. Those describe the moment of entry; what a lookback asks is
    /// whether the price has been at the band at all.
    /// </para>
    /// </summary>
    public static bool TryFindRecentBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        bool isLong, int withinCandles, out int candlesAgo)
    {
        candlesAgo = 0;

        var settings = AtrRbPlugin.Settings;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles, symbolInterval.Interval.Duration);
        if (candles.Count < settings.Length + settings.BreakLookback)
            return false;

        // Locate the requested (just-closed) candle; fall back to the most recent one.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            idx = candles.Count - 1;

        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        IReadOnlyList<EmaResult> emaList = quotes.ToEma(settings.Length);
        IReadOnlyList<AtrResult> atrList = quotes.ToAtr(settings.Length);

        for (int i = 0; i < withinCandles && idx - i >= 0; i++)
        {
            if (IsBreakAt(candles, emaList, atrList, idx - i, isLong, out _, out _))
            {
                candlesAgo = i;
                return true;
            }
        }
        return false;
    }
}
