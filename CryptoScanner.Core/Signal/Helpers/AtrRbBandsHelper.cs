using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// Shared parameters and calculations for the "AtrRb Bands &amp; Ribbon" construction
/// (a Keltner-style EMA basis with ATR based bands). The chart drawer and the "settings"
/// signal algorithm both read these constants so they stay in sync — change them here
/// and both the chart and the alert follow.
/// </summary>
public static class AtrRbBandsHelper
{
    // The band parameters (Len / OuterMult / InnerMult / BreakLookback) now live in
    // GlobalData.Settings.Signal.AtrRb so the user can tune them; the chart drawer reads the same
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

        var settings = GlobalData.Settings.Signal.AtrRb;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles);
        if (candles.Count < settings.Length + settings.BreakLookback)
            return false;

        // Locate the requested (just-closed) candle. If it is not in the snapshot, DON'T fire — the old
        // fallback to candles.Count-1 (the previous candle) made the band come from the wrong candle,
        // while the signal class read high/low/close from CandleLast, so the entry landed on the
        // previous candle's band.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            return false;
        if (idx < settings.BreakLookback - 1)
            return false;

        // EMA(Len) basis and ATR(Len), computed exactly like the chart drawer.
        List<EmaResult> emaList = (List<EmaResult>)candles.GetEma(settings.Length);
        List<AtrResult> atrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        double? basis = emaList[idx].Ema;
        double? atr = atrList[idx].Atr;
        if (!basis.HasValue || !atr.HasValue || basis.Value == 0)
            return false;

        lowerBand = basis.Value - atr.Value * settings.OuterMult;

        double low = (double)candles[idx].Low;
        if (low >= lowerBand)
            return false;

        // Only fire on the lowest Low within the trailing window (matches ta.lowest filter).
        for (int j = idx - settings.BreakLookback + 1; j < idx; j++)
        {
            if ((double)candles[j].Low < low)
                return false;
        }

        //pctDeviation = (basis.Value - low) / basis.Value * 100;
        //pctDeviation = atr.Value / (double)candles[idx].Close * 100;
        pctDeviation = settings.StopLossAtrFactor * (atr.Value / (double)candles[idx].Close * 100);
        return true;
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

        var settings = GlobalData.Settings.Signal.AtrRb;

        // Thread-safe ascending snapshot of the most recent candles.
        List<CryptoCandle> candles = symbolInterval.CandleList.GetLastNValues(CalculationCandles);
        if (candles.Count < settings.Length + settings.BreakLookback)
            return false;

        // Locate the requested (just-closed) candle. If it is not in the snapshot, DON'T fire — the old
        // fallback to candles.Count-1 (the previous candle) made the band come from the wrong candle,
        // while the signal class read high/low/close from CandleLast, so the entry landed on the
        // previous candle's band.
        int idx = candles.FindIndex(c => c.OpenTime == openTime);
        if (idx < 0)
            return false;
        if (idx < settings.BreakLookback - 1)
            return false;

        // EMA(Len) basis and ATR(Len), computed exactly like the chart drawer.
        List<EmaResult> emaList = (List<EmaResult>)candles.GetEma(settings.Length);
        List<AtrResult> atrList = (List<AtrResult>)candles.GetAtr(settings.Length);
        double? basis = emaList[idx].Ema;
        double? atr = atrList[idx].Atr;
        if (!basis.HasValue || !atr.HasValue || basis.Value == 0)
            return false;

        upperBand = basis.Value + atr.Value * settings.OuterMult;

        double high = (double)candles[idx].High;
        if (high <= upperBand)
            return false;

        // Only fire on the highest High within the trailing window (matches ta.highest filter).
        for (int j = idx - settings.BreakLookback + 1; j < idx; j++)
        {
            if ((double)candles[j].High > high)
                return false;
        }

        //pctDeviation = (high - basis.Value) / basis.Value * 100;
        pctDeviation = settings.StopLossAtrFactor * (atr.Value / (double)candles[idx].Close * 100);
        return true;
    }
}
