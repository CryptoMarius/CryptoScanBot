using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Nwe;

/// <summary>
/// Combined NWE×BB + AtrRb mean-reversal detector. Fires when BOTH sub-signals occur on the SAME side
/// within <see cref="WindowCandles"/> candles of each other — order does not matter (A→B or B→A) — and
/// the signal is placed on the SECOND of the two (the candle that completes the pair).
///
/// The idea: the AtrRb macro band hit marks an over-extension (mean-reversal setup), and the NWE×BB
/// crossover marks the Bollinger band curling back inside the Nadaraya-Watson envelope. Requiring both
/// close together is a stronger reversal confirmation than either alone.
///
/// PERFORMANCE: the repainting NWE is O(N²) (kernel-weighted, with exp/pow per point). The AtrRb side
/// is only O(N) (EMA/ATR). Since a combined signal REQUIRES an AtrRb band hit in the window, the AtrRb
/// computation is used as a cheap gate: the expensive NWE is only computed when an AtrRb break is
/// actually nearby (rare). See <see cref="FiresAt"/> (live strategy) and the early-out in
/// <see cref="Detect"/> (chart). Order follows the "cheap conditions first" rule.
///
/// Pure list-based, so the chart overlay and the live strategy (<see cref="SignalNweBbAtrRb"/>) share
/// exactly the same detection. Reuses <see cref="NweBbDetector"/> and <see cref="AtrRbBandsHelper"/>.
/// </summary>
public static class NweBbAtrRbDetector
{
    /// <summary>Co-occurrence window: the two sub-signals must fall within this many candles.</summary>
    public const int WindowCandles = 5;

    /// <summary>
    /// How many trailing candles the live strategy feeds the detector per evaluation. The walk-forward
    /// chart overlay uses the same value so it reproduces the strategy's per-candle window exactly.
    /// </summary>
    public const int StrategyLookback = 350;

    /// <summary>A detected combined marker: the (second) candle's open time, the side, and its close price.</summary>
    public readonly record struct Marker(CandleTime OpenTime, CryptoTradeSide Side, decimal Price);

    private static readonly CryptoTradeSide[] Sides = [CryptoTradeSide.Long, CryptoTradeSide.Short];


    /// <summary>
    /// Fast single-candle test for the live strategy: does the combined signal fire on candle
    /// <paramref name="index"/> for <paramref name="side"/>?
    /// </summary>
    public static bool FiresAt(IReadOnlyList<CryptoCandle> candles, int index, CryptoTradeSide side)
    {
        var (fireLong, fireShort) = FiresSidesAt(candles, index);
        return side == CryptoTradeSide.Long ? fireLong : fireShort;
    }

    /// <summary>
    /// Both-sides single-candle test: does the combined signal fire on candle <paramref name="index"/>
    /// for long and/or short? Computes the cheap AtrRb side first and bails (skipping the O(N²) NWE)
    /// unless an AtrRb break sits within the window. Computing both sides in one pass lets the
    /// walk-forward chart overlay evaluate a candle without paying for the indicators twice.
    /// </summary>
    public static (bool Long, bool Short) FiresSidesAt(IReadOnlyList<CryptoCandle> candles, int index)
    {
        if (candles == null || index < 0 || index >= candles.Count)
            return (false, false);

        int from = Math.Max(0, index - WindowCandles);

        // ---- Cheap gate: AtrRb (EMA/ATR, O(N)) ----
        var settings = GlobalData.Settings.Signal.AtrRb;
        var emaList = (List<EmaResult>)candles.GetEma(settings.Length);
        var atrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        var bbPct = ComputeBbPct(candles);

        bool atrRecentL = false, atrNowL = false, atrRecentS = false, atrNowS = false;
        for (int i = from; i <= index; i++)
        {
            var s = AtrSideAt(candles, emaList, atrList, bbPct, i);
            if (s == CryptoTradeSide.Long) { atrRecentL = true; if (i == index) atrNowL = true; }
            else if (s == CryptoTradeSide.Short) { atrRecentS = true; if (i == index) atrNowS = true; }
        }

        // A combined signal always needs an AtrRb break in the window. No break → no signal, and we
        // never pay for the expensive NWE. AtrRb breaks are rare, so this is the common (cheap) path.
        if (!atrRecentL && !atrRecentS)
            return (false, false);

        // ---- Expensive part (only reached when an AtrRb break is nearby): NWE×BB ----
        var nweByTime = new Dictionary<CandleTime, CryptoTradeSide>();
        foreach (var m in NweBbDetector.Detect(candles))
            nweByTime[m.OpenTime] = m.Side;

        bool nweRecentL = false, nweNowL = false, nweRecentS = false, nweNowS = false;
        for (int i = from; i <= index; i++)
        {
            if (nweByTime.TryGetValue(candles[i].OpenTime, out var s))
            {
                if (s == CryptoTradeSide.Long) { nweRecentL = true; if (i == index) nweNowL = true; }
                else if (s == CryptoTradeSide.Short) { nweRecentS = true; if (i == index) nweNowS = true; }
            }
        }

        // Fire on the second: one sub-signal is on this candle, the other within the window.
        bool fireLong = (nweNowL && atrRecentL) || (atrNowL && nweRecentL);
        bool fireShort = (nweNowS && atrRecentS) || (atrNowS && nweRecentS);
        return (fireLong, fireShort);
    }

    /// <summary>
    /// Walk-forward scan: evaluates each candle in [<paramref name="minDate"/>, <paramref name="maxDate"/>]
    /// EXACTLY as the live strategy would — over a trailing window of <paramref name="lookback"/> candles
    /// ending at that candle (so the repainting NWE uses no "future" beyond the evaluated candle). This is
    /// the faithful (but heavier) alternative to <see cref="Detect"/>, which recomputes the NWE once over
    /// the whole visible window and therefore shifts/invents markers. Use this for the chart overlay when
    /// it must match the strategy's actual signals.
    /// </summary>
    public static List<Marker> DetectWalkForward(IReadOnlyList<CryptoCandle> candles, int lookback,
        CandleTime minDate, CandleTime maxDate)
    {
        var result = new List<Marker>();
        if (candles == null || candles.Count == 0)
            return result;
        if (lookback < 1)
            lookback = StrategyLookback;

        for (int c = 0; c < candles.Count; c++)
        {
            CandleTime openTime = candles[c].OpenTime;
            if (openTime < minDate || openTime > maxDate)
                continue; // only mark visible candles; the warmup prefix is just window context

            // Trailing window ending at this candle — the same slice the live strategy feeds the detector.
            int from = Math.Max(0, c - lookback + 1);
            int count = c - from + 1;
            var slice = new List<CryptoCandle>(count);
            for (int i = 0; i < count; i++)
                slice.Add(candles[from + i]);

            var (fireLong, fireShort) = FiresSidesAt(slice, slice.Count - 1);
            if (fireLong)
                result.Add(new Marker(openTime, CryptoTradeSide.Long, candles[c].Close));
            if (fireShort)
                result.Add(new Marker(openTime, CryptoTradeSide.Short, candles[c].Close));
        }

        return result;
    }

    /// <summary>
    /// Full scan over a candle list — returns every combined marker. Used by the chart overlay (run
    /// once per refresh). Early-outs without touching the NWE when the window has no AtrRb breaks at all.
    /// </summary>
    public static List<Marker> Detect(IReadOnlyList<CryptoCandle> candles)
    {
        var result = new List<Marker>();
        if (candles == null || candles.Count == 0)
            return result;

        // ---- AtrRb side per index (cheap, O(N)) ----
        var settings = GlobalData.Settings.Signal.AtrRb;
        var emaList = (List<EmaResult>)candles.GetEma(settings.Length);
        var atrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        var bbPct = ComputeBbPct(candles);

        var atrSide = new CryptoTradeSide?[candles.Count];
        bool anyAtr = false;
        for (int i = 0; i < candles.Count; i++)
        {
            atrSide[i] = AtrSideAt(candles, emaList, atrList, bbPct, i);
            if (atrSide[i] != null)
                anyAtr = true;
        }

        // No AtrRb break anywhere → no combined signal possible. Skip the O(N²) NWE entirely.
        if (!anyAtr)
            return result;

        // ---- NWE×BB side per index (expensive; only when at least one AtrRb break exists) ----
        var nweByTime = new Dictionary<CandleTime, CryptoTradeSide>();
        foreach (var m in NweBbDetector.Detect(candles))
            nweByTime[m.OpenTime] = m.Side;

        var nweSide = new CryptoTradeSide?[candles.Count];
        for (int i = 0; i < candles.Count; i++)
            if (nweByTime.TryGetValue(candles[i].OpenTime, out var s))
                nweSide[i] = s;

        for (int i = 0; i < candles.Count; i++)
        {
            foreach (var side in Sides)
            {
                bool nweNow = nweSide[i] == side;
                bool atrNow = atrSide[i] == side;
                if (!nweNow && !atrNow)
                    continue; // neither sub-signal of this side fires on this (second) candle

                int from = Math.Max(0, i - WindowCandles);
                bool nweRecent = false, atrRecent = false;
                for (int j = from; j <= i; j++)
                {
                    if (nweSide[j] == side) nweRecent = true;
                    if (atrSide[j] == side) atrRecent = true;
                }

                if ((nweNow && atrRecent) || (atrNow && nweRecent))
                    result.Add(new Marker(candles[i].OpenTime, side, candles[i].Close));
            }
        }

        return result;
    }

    /// <summary>
    /// AtrRb side at one index: long on a lower-band break, short on an upper-band break. Applies the
    /// SAME BB-width gate (BBMinPercentage/BBMaxPercentage) as the standalone AtrRb signal
    /// (SignalAtrRbLong/Short) and the chart's AtrRb labels, so the combo only pairs breaks that are
    /// real AtrRb events. Without this the detector fired on extra (out-of-width) breaks, which made the
    /// paired AtrRb sit far from the nearest VISIBLE (labelled) one.
    /// </summary>
    private static CryptoTradeSide? AtrSideAt(IReadOnlyList<CryptoCandle> candles,
        IReadOnlyList<EmaResult> emaList, IReadOnlyList<AtrResult> atrList, double?[] bbPct, int idx)
    {
        var atrrb = GlobalData.Settings.Signal.AtrRb;
        if (!bbPct[idx].HasValue || !BbWidthOk(bbPct[idx]!.Value, atrrb.BBMinPercentage, atrrb.BBMaxPercentage))
            return null;

        if (AtrRbBandsHelper.LowerBandBreakAt(candles, emaList, atrList, idx, out _, out _))
            return CryptoTradeSide.Long;
        if (AtrRbBandsHelper.UpperBandBreakAt(candles, emaList, atrList, idx, out _, out _))
            return CryptoTradeSide.Short;
        return null;
    }

    /// <summary>
    /// Bollinger-band width percentage (100·(upper/lower−1)) per candle index, computed with the same
    /// BB settings the indicator cache / chart use. Used for the AtrRb BB-width gate.
    /// </summary>
    private static double?[] ComputeBbPct(IReadOnlyList<CryptoCandle> candles)
    {
        var bbList = (List<BollingerBandsResult>)candles.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        var bbPct = new double?[candles.Count];
        for (int i = 0; i < candles.Count && i < bbList.Count; i++)
        {
            var bb = bbList[i];
            if (bb.UpperBand.HasValue && bb.LowerBand.HasValue && bb.LowerBand.Value != 0)
                bbPct[i] = 100.0 * (bb.UpperBand.Value / bb.LowerBand.Value - 1.0);
        }
        return bbPct;
    }

    /// <summary>
    /// Mirrors BollingerBandsHelper.CheckBollingerBandsWidth: a bound of 0 disables that side, so the
    /// width must be &gt; min (when min &gt; 0) and &lt; max (when max &gt; 0).
    /// </summary>
    private static bool BbWidthOk(double bbPct, double min, double max)
    {
        if (min > 0 && bbPct <= min)
            return false;
        if (max > 0 && bbPct >= max)
            return false;
        return true;
    }
}
