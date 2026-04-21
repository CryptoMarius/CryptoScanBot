using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// GaussianScalp Long — based on the 3-layer scalping strategy:
///   Layer 1 — Trend:     2-pole Gaussian filter (inline calculation, period 40)
///                        Bullish when current filter value > previous filter value
///   Layer 2 — Momentum:  RSI(30) above 50
///   Layer 3 — Validation: MACD histogram (24/52/9) above 0
///   Entry:               Bullish confirmation candle (close > open)
///
/// The Gaussian filter is a low-pass recursive filter that smooths price while
/// reducing lag compared to a simple EMA. Formula for 2-pole:
///   a = exp(-sqrt(2) * π / period)
///   b = 2 * a * cos(sqrt(2) * π / period)
///   c = a^2
///   f[i] = b*f[i-1] - c*f[i-2] + (1 - b + c) * close[i]
/// </summary>
public class SignalGaussianScalpLong : SignalCreateBase
{
    // Number of candles used for the Gaussian filter warm-up
    private const int GaussianPeriod = 40;
    private const int GaussianLookback = GaussianPeriod * 3;

    public override bool IsSignal()
    {
        ExtraText = "";

        // --- Layer 2: RSI(30) > 50 ---
        if (CandleLast.CandleData!.Rsi30 == null)
        {
            ExtraText = "no RSI30 data";
            return false;
        }
        double rsi30 = CandleLast.CandleData.Rsi30.Value;
        if (rsi30 <= 50)
        {
            ExtraText = $"RSI30 {rsi30:N1} <= 50";
            return false;
        }

        // --- Layer 3: MACD(24/52/9) histogram > 0 ---
        if (CandleLast.CandleData.MacdHistogram24 == null)
        {
            ExtraText = "no MACD24 data";
            return false;
        }
        double macdHist = CandleLast.CandleData.MacdHistogram24.Value;
        if (macdHist <= 0)
        {
            ExtraText = $"MACD24 hist {macdHist:N6} <= 0";
            return false;
        }

        // --- Entry: bullish confirmation candle ---
        if (CandleLast.Candle.Close <= CandleLast.Candle.Open)
        {
            ExtraText = "no bullish confirmation candle";
            return false;
        }

        // --- Layer 1: 2-pole Gaussian filter — collect candles oldest → newest ---
        var closes = new List<double>(GaussianLookback);
        MyData? candle = CandleLast;
        for (int i = 0; i < GaussianLookback; i++)
        {
            closes.Add((double)candle!.Candle.Close);
            if (!GetPrevCandle(candle, out candle))
                break;
        }

        if (closes.Count < GaussianPeriod + 2)
        {
            ExtraText = "insufficient history for Gaussian filter";
            return false;
        }

        closes.Reverse(); // oldest first
        double[] f = ComputeGaussian(closes, GaussianPeriod);

        // Bullish: current filter value > previous filter value
        double filterNow = f[^1];
        double filterPrev = f[^2];
        if (filterNow <= filterPrev)
        {
            ExtraText = $"Gaussian bearish ({filterNow:N6} <= {filterPrev:N6})";
            return false;
        }

        ExtraText = $"G↑ RSI30={rsi30:N1} MACD={macdHist:N6}";
        return true;
    }


    /// <summary>
    /// Computes a 2-pole Gaussian low-pass filter over the given close prices.
    /// Returns an array of the same length.
    /// </summary>
    private static double[] ComputeGaussian(List<double> closes, int period)
    {
        double sqrt2Pi = Math.Sqrt(2.0) * Math.PI;
        double a = Math.Exp(-sqrt2Pi / period);
        double b = 2.0 * a * Math.Cos(sqrt2Pi / period);
        double c = a * a;
        double coeff = 1.0 - b + c;

        double[] f = new double[closes.Count];
        f[0] = closes[0];
        f[1] = closes[1];
        for (int i = 2; i < closes.Count; i++)
            f[i] = b * f[i - 1] - c * f[i - 2] + coeff * closes[i];

        return f;
    }
}
