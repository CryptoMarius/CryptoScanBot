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

    /// <summary>A detected combined marker: the (second) candle's open time, the side, and its close price.</summary>
    public readonly record struct Marker(CandleTime OpenTime, CryptoTradeSide Side, decimal Price);

    private static readonly CryptoTradeSide[] Sides = [CryptoTradeSide.Long, CryptoTradeSide.Short];


    /// <summary>
    /// Fast single-candle test for the live strategy: does the combined signal fire on candle
    /// <paramref name="index"/> for <paramref name="side"/>? Computes the cheap AtrRb side first and
    /// bails (skipping the O(N²) NWE) unless an AtrRb break of the same side sits within the window.
    /// </summary>
    public static bool FiresAt(IReadOnlyList<CryptoCandle> candles, int index, CryptoTradeSide side)
    {
        if (index < 0 || index >= candles.Count)
            return false;

        int from = Math.Max(0, index - WindowCandles);

        // ---- Cheap gate: AtrRb (EMA/ATR, O(N)) ----
        var settings = GlobalData.Settings.Signal.AtrRb;
        var emaList = (List<EmaResult>)candles.GetEma(settings.Length);
        var atrList = (List<AtrResult>)candles.GetAtr(settings.Length);

        bool atrRecent = false, atrNow = false;
        for (int i = from; i <= index; i++)
        {
            if (AtrSideAt(candles, emaList, atrList, i) == side)
            {
                atrRecent = true;
                if (i == index)
                    atrNow = true;
            }
        }

        // A combined signal of this side always needs an AtrRb break of this side in the window. No
        // break → no signal, and we never pay for the expensive NWE. AtrRb breaks are rare, so this is
        // the common (cheap) path.
        if (!atrRecent)
            return false;

        // ---- Expensive part (only reached when an AtrRb break is nearby): NWE×BB ----
        var nweByTime = new Dictionary<CandleTime, CryptoTradeSide>();
        foreach (var m in NweBbDetector.Detect(candles))
            nweByTime[m.OpenTime] = m.Side;

        bool nweRecent = false, nweNow = false;
        for (int i = from; i <= index; i++)
        {
            if (nweByTime.TryGetValue(candles[i].OpenTime, out var s) && s == side)
            {
                nweRecent = true;
                if (i == index)
                    nweNow = true;
            }
        }

        // Fire on the second: one sub-signal is on this candle, the other within the window.
        return (nweNow && atrRecent) || (atrNow && nweRecent);
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

        var atrSide = new CryptoTradeSide?[candles.Count];
        bool anyAtr = false;
        for (int i = 0; i < candles.Count; i++)
        {
            atrSide[i] = AtrSideAt(candles, emaList, atrList, i);
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

    /// <summary>AtrRb side at one index: long on a lower-band break, short on an upper-band break.</summary>
    private static CryptoTradeSide? AtrSideAt(IReadOnlyList<CryptoCandle> candles,
        IReadOnlyList<EmaResult> emaList, IReadOnlyList<AtrResult> atrList, int idx)
    {
        if (AtrRbBandsHelper.LowerBandBreakAt(candles, emaList, atrList, idx, out _, out _))
            return CryptoTradeSide.Long;
        if (AtrRbBandsHelper.UpperBandBreakAt(candles, emaList, atrList, idx, out _, out _))
            return CryptoTradeSide.Short;
        return null;
    }
}
