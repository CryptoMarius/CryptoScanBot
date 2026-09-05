using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Dbr.Signal;

/// <summary>
/// Per-candle values of the "Donchian Breakout Reversion" construction, index-aligned with the
/// candle list handed to <see cref="DbrBandsHelper.ComputeBands"/>.
/// </summary>
public struct DbrBandValue
{
    public bool HasBands;       // Donchian warm-up complete for this index
    public double Upper;        // middle + halfRange * (OuterMult / 2.5)
    public double Lower;        // middle - halfRange * (OuterMult / 2.5)
    public double Middle;       // (highestHigh + lowestLow) / 2
    public double BandWidthPct; // (Upper - Middle) / Middle * 100 — the label percentage
    public double? Rsi;         // RSI(RsiLength); only filled when the RSI filter is enabled
    public double? StochK;      // Stochastic-RSI %K; only filled when the stoch filter is enabled
    public double? StochD;      // Stochastic-RSI %D; only filled when the stoch filter is enabled
}

/// <summary>
/// Shared calculations for the "Donchian Breakout Reversion" (DBR) construction: Donchian-based outer
/// bands over the previous BandLength candles, with optional
/// RSI / Stochastic-RSI filters — a port of the Pine script
/// "Donchian Breakout Reversion (DBR) - Ultimate Master Cockpit".
/// The chart drawer and the "dbr" signal algorithm both use these methods so they stay in sync —
/// the chart label and the alert always agree.
/// </summary>
public static class DbrBandsHelper
{
    // Number of candles to feed the calculations. Matches the signal pipeline window and leaves
    // enough warm-up for the Donchian window and the Stochastic-RSI chain.
    private const int CalculationCandles = 260;

    /// <summary>
    /// Computes the DBR bands (and the enabled filter series) for every candle in the list.
    /// The result is index-aligned with <paramref name="candles"/>.
    /// </summary>
    public static DbrBandValue[] ComputeBands(List<CryptoCandle> candles)
    {
        var settings = DbrPlugin.Settings;
        int count = candles.Count;
        var result = new DbrBandValue[count];
        if (count == 0)
            return result;

        IReadOnlyList<IQuote> quotes = candles.AsQuotes();

        // RSI — only computed when the RSI filter is enabled. Uses the standard RSI(14) from general settings.
        var rsiSettings = GlobalData.Settings.General.SettingsRsi;
        IReadOnlyList<RsiResult>? rsiList = null;
        if (settings.UseRsiFilter)
            rsiList = quotes.ToRsi(rsiSettings.Length);

        // Stochastic-RSI — computed manually so it matches the Pine chain exactly:
        // raw = stoch(rsi, length), %K = SMA(raw, K), %D = SMA(%K, D).
        // Uses the standard stoch settings (length 14, K 3, D 3) from general settings.
        var stochSettings = GlobalData.Settings.General.SettingsStoch;
        double?[]? stochK = null;
        double?[]? stochD = null;
        if (settings.RequireStochOsOb)
            ComputeStochRsi(quotes, stochSettings.Length, stochSettings.SmoothingK, stochSettings.SmoothingD, out stochK, out stochD);

        for (int i = 0; i < count; i++)
        {
            ref DbrBandValue value = ref result[i];

            value.Rsi = rsiList?[i].Rsi;
            value.StochK = stochK?[i];
            value.StochD = stochD?[i];

            // Donchian over the PREVIOUS BandLength candles, excluding the current one
            // (Pine: ta.highest(high[1], len) / ta.lowest(low[1], len)).
            if (i < settings.BandLength)
                continue;

            double highestHigh = double.MinValue;
            double lowestLow = double.MaxValue;
            for (int j = i - settings.BandLength; j < i; j++)
            {
                double high = (double)candles[j].High;
                double low = (double)candles[j].Low;
                if (high > highestHigh)
                    highestHigh = high;
                if (low < lowestLow)
                    lowestLow = low;
            }

            double middle = (highestHigh + lowestLow) / 2;
            double halfRange = (highestHigh - lowestLow) / 2;
            double band = halfRange * (settings.OuterMult / 2.5);

            value.HasBands = true;
            value.Middle = middle;
            value.Upper = middle + band;
            value.Lower = middle - band;
            value.BandWidthPct = middle != 0 ? band / middle * 100 : 0;
        }

        return result;
    }

    /// <summary>
    /// Returns true when the candle at <paramref name="openTime"/> fires the DBR long signal:
    /// the Low breaks the lower band, the stacking rule passes and all enabled filters agree.
    /// This mirrors exactly the lower-band label condition drawn on the chart.
    /// <paramref name="bandWidthPct"/> is the same percentage printed as the chart label.
    /// </summary>
    public static bool IsLowerBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double bandWidthPct, out double lowerBand, out string reason)
    {
        return IsBandBreak(symbolInterval, openTime, isLong: true, out bandWidthPct, out lowerBand, out reason);
    }

    /// <summary>
    /// Returns true when the candle at <paramref name="openTime"/> fires the DBR short signal:
    /// the High breaks the upper band, the stacking rule passes and all enabled filters agree.
    /// This mirrors exactly the upper-band label condition drawn on the chart.
    /// <paramref name="bandWidthPct"/> is the same percentage printed as the chart label.
    /// </summary>
    public static bool IsUpperBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        out double bandWidthPct, out double upperBand, out string reason)
    {
        return IsBandBreak(symbolInterval, openTime, isLong: false, out bandWidthPct, out upperBand, out reason);
    }

    private static bool IsBandBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime, bool isLong,
        out double bandWidthPct, out double bandPrice, out string reason)
    {
        bandWidthPct = 0;
        bandPrice = 0;

        var settings = DbrPlugin.Settings;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles, symbolInterval.Interval.Duration);
        if (candles.Count < settings.BandLength + 1)
        {
            reason = "not enough candles";
            return false;
        }

        // Locate the requested (just-closed) candle; fall back to the most recent one.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            idx = candles.Count - 1;

        DbrBandValue[] bands = ComputeBands(candles);
        return isLong
            ? IsLongBreak(candles, bands, idx, out bandWidthPct, out bandPrice, out reason)
            : IsShortBreak(candles, bands, idx, out bandWidthPct, out bandPrice, out reason);
    }

    /// <summary>
    /// The most recent band break at or before <paramref name="openTime"/>, looking back at most
    /// <paramref name="withinCandles"/> candles (the candle at that time included). The bands are
    /// computed once for the whole snapshot, so the window costs a walk and not a recompute per
    /// candle. <paramref name="candlesAgo"/> is 0 when the break is on the candle itself.
    /// <para>
    /// The band break and the stacking rule only - the RSI, stochastic and Bollinger-width filters
    /// of the DBR strategy are NOT replayed here. Those describe the moment of entry; what a
    /// lookback asks is whether the price has been at the band at all.
    /// </para>
    /// </summary>
    public static bool TryFindRecentBreak(CryptoSymbolInterval symbolInterval, CandleTime openTime,
        bool isLong, int withinCandles, out int candlesAgo)
    {
        candlesAgo = 0;

        var settings = DbrPlugin.Settings;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles, symbolInterval.Interval.Duration);
        if (candles.Count < settings.BandLength + 1)
            return false;

        // Locate the requested (just-closed) candle; fall back to the most recent one.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            idx = candles.Count - 1;

        DbrBandValue[] bands = ComputeBands(candles);
        for (int i = 0; i < withinCandles && idx - i >= 0; i++)
        {
            bool broke = isLong
                ? IsLongBreak(candles, bands, idx - i, out _, out _, out _)
                : IsShortBreak(candles, bands, idx - i, out _, out _, out _);
            if (broke)
            {
                candlesAgo = i;
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// Full long-signal check on index <paramref name="idx"/>: lower-band break + stacking rule.
    /// Also used by the chart drawer for the break labels.
    /// </summary>
    public static bool IsLongBreak(List<CryptoCandle> candles, DbrBandValue[] bands, int idx,
        out double bandWidthPct, out double bandPrice, out string reason)
    {
        bandWidthPct = 0;
        bandPrice = 0;

        if (idx < 0 || idx >= bands.Length || !bands[idx].HasBands)
        {
            reason = "bands warming up";
            return false;
        }

        var settings = DbrPlugin.Settings;
        ref DbrBandValue value = ref bands[idx];
        bandWidthPct = value.BandWidthPct;
        bandPrice = value.Lower;

        if ((double)candles[idx].Low >= value.Lower)
        {
            reason = "no lower band break";
            return false;
        }

        // Stacking rule (Pine): fire on the FIRST break candle of a run; while the previous candle
        // also broke the band a new signal needs a lower Low (only when stacking is allowed).
        bool rawPrev = idx > 0 && bands[idx - 1].HasBands && (double)candles[idx - 1].Low < bands[idx - 1].Lower;
        if (rawPrev && !(settings.AllowStack && candles[idx].Low < candles[idx - 1].Low))
        {
            reason = "already broken on previous candle";
            return false;
        }

        reason = "";
        return true;
    }

    /// <summary>
    /// Full short-signal check on index <paramref name="idx"/>: upper-band break + stacking rule.
    /// Also used by the chart drawer for the break labels.
    /// </summary>
    public static bool IsShortBreak(List<CryptoCandle> candles, DbrBandValue[] bands, int idx,
        out double bandWidthPct, out double bandPrice, out string reason)
    {
        bandWidthPct = 0;
        bandPrice = 0;

        if (idx < 0 || idx >= bands.Length || !bands[idx].HasBands)
        {
            reason = "bands warming up";
            return false;
        }

        var settings = DbrPlugin.Settings;
        ref DbrBandValue value = ref bands[idx];
        bandWidthPct = value.BandWidthPct;
        bandPrice = value.Upper;

        if ((double)candles[idx].High <= value.Upper)
        {
            reason = "no upper band break";
            return false;
        }

        // Stacking rule (Pine): fire on the FIRST break candle of a run; while the previous candle
        // also broke the band a new signal needs a higher High (only when stacking is allowed).
        bool rawPrev = idx > 0 && bands[idx - 1].HasBands && (double)candles[idx - 1].High > bands[idx - 1].Upper;
        if (rawPrev && !(settings.AllowStack && candles[idx].High > candles[idx - 1].High))
        {
            reason = "already broken on previous candle";
            return false;
        }

        reason = "";
        return true;
    }

    /// <summary>
    /// Stochastic-RSI exactly as the Pine script chains it:
    ///   rsi  = ta.rsi(close, length)
    ///   raw  = ta.stoch(rsi, rsi, rsi, length)
    ///   %K   = ta.sma(raw, kLength)
    ///   %D   = ta.sma(%K, dLength)
    /// </summary>
    private static void ComputeStochRsi(IReadOnlyList<IQuote> quotes, int length, int kLength, int dLength,
        out double?[] stochK, out double?[] stochD)
    {
        IReadOnlyList<RsiResult> rsiList = quotes.ToRsi(length);
        int count = rsiList.Count;

        // Raw stochastic of the RSI series over the same length.
        var raw = new double?[count];
        for (int i = 0; i < count; i++)
        {
            double lowest = double.MaxValue;
            double highest = double.MinValue;
            bool complete = i >= length - 1;
            for (int j = i - length + 1; complete && j <= i; j++)
            {
                if (j < 0 || !rsiList[j].Rsi.HasValue)
                {
                    complete = false;
                    break;
                }
                double rsi = rsiList[j].Rsi!.Value;
                if (rsi < lowest)
                    lowest = rsi;
                if (rsi > highest)
                    highest = rsi;
            }
            if (complete && rsiList[i].Rsi.HasValue)
            {
                double range = highest - lowest;
                raw[i] = range > 0 ? (rsiList[i].Rsi!.Value - lowest) / range * 100 : 0;
            }
        }

        stochK = Sma(raw, kLength);
        stochD = Sma(stochK, dLength);
    }

    // Simple moving average over a nullable series; null while the window is incomplete.
    private static double?[] Sma(double?[] values, int length)
    {
        var result = new double?[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            if (i < length - 1)
                continue;
            double sum = 0;
            bool complete = true;
            for (int j = i - length + 1; j <= i; j++)
            {
                if (!values[j].HasValue)
                {
                    complete = false;
                    break;
                }
                sum += values[j]!.Value;
            }
            if (complete)
                result[i] = sum / length;
        }
        return result;
    }
}
