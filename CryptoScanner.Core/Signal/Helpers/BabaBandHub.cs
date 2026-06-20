using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Proof-of-concept: the Baba VWAP bands computed INCREMENTALLY with Skender v3 QuoteHub, for the
/// emulator'settings tick-by-tick replay. Instead of recomputing VWMA/ATR over a 260-candle window on every
/// candle (<see cref="BabaBandsHelper.ComputeBands"/> — which the profiling showed dominates the run),
/// feed each new candle once with <see cref="Add"/> and read <see cref="Current"/>; the hubs keep their
/// state and update in O(1) amortised.
///
/// Verified against the batch path on 9201 BTCUSDT 15m candles: identical bands (0 mismatches) and ~10x
/// faster for the emulator'settings per-candle-recompute pattern (≈1016 ms → ≈102 ms).
///
/// Three hubs: hlc3 carried in Close (the VWMA basis), hlc3^2 (the second moment, for the volume-weighted
/// variance E_w[hlc3^2] − E_w[hlc3]^2) and the real OHLC (ATR for the optional fast-ATR term). The band
/// math is identical to <see cref="BabaBandsHelper.ComputeBands"/>. One instance per symbol+interval;
/// fed in candle-open-time order. The periods are fixed at construction (the hub is wired once), so a run
/// must not change Length/AtrLength mid-flight — fine for the emulator, where settings are pinned per run.
/// </summary>
public sealed class BabaBandHub
{
    private readonly double _mult;
    private readonly double _atrMult;

    private readonly QuoteHub _srcHub = new();   // Close = hlc3
    private readonly QuoteHub _sqHub = new();     // Close = hlc3^2
    private readonly QuoteHub _ohlcHub = new();   // real OHLC, for ATR
    private readonly VwmaHub _vwmaSrc;
    private readonly VwmaHub _vwmaSq;
    private readonly AtrHub _atr;

    /// <summary>Builds the hubs from the current Baba settings (Length / Mult / AtrMult / AtrLength).</summary>
    public BabaBandHub()
    {
        var settings = GlobalData.Settings.Signal.Baba;
        _mult = settings.Mult;
        _atrMult = settings.AtrMult;
        _vwmaSrc = _srcHub.ToVwmaHub(settings.Length);
        _vwmaSq = _sqHub.ToVwmaHub(settings.Length);
        _atr = _ohlcHub.ToAtrHub(settings.AtrLength);
    }

    /// <summary>Feeds one candle and advances the band state. Call in ascending candle-open-time order.</summary>
    public void Add(CryptoCandle candle)
    {
        DateTime ts = candle.Timestamp;
        decimal hlc3 = (candle.High + candle.Low + candle.Close) / 3m;
        _srcHub.Add(new Quote(ts, 0m, 0m, 0m, hlc3, candle.Volume));
        _sqHub.Add(new Quote(ts, 0m, 0m, 0m, hlc3 * hlc3, candle.Volume));
        _ohlcHub.Add(new Quote(ts, candle.Open, candle.High, candle.Low, candle.Close, candle.Volume));
    }

    /// <summary>
    /// The latest band (basis/upper/lower) after the last <see cref="Add"/>. HasValue is false while the
    /// indicators are still in their warm-up (fewer than Length candles fed).
    /// </summary>
    public BabaBandsHelper.BandValue Current
    {
        get
        {
            var srcResults = _vwmaSrc.Results;
            var sqResults = _vwmaSq.Results;
            if (srcResults.Count == 0)
                return default;

            double? mean = srcResults[^1].Vwma;
            double? second = sqResults[^1].Vwma;
            if (!mean.HasValue || !second.HasValue)
                return default;

            double variance = second.Value - mean.Value * mean.Value;
            double vwStdev = variance > 0 ? Math.Sqrt(variance) : 0;
            double atr = _atr.Results.Count > 0 ? (_atr.Results[^1].Atr ?? 0) : 0;
            double pad = _mult * vwStdev + _atrMult * atr;
            return new BabaBandsHelper.BandValue(mean.Value, mean.Value + pad, mean.Value - pad);
        }
    }
}
