using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// GaussianScalp Short — mirror of SignalGaussianScalpLong.
///   Layer 1 — Trend:     2-pole Gaussian filter (inline calculation, period 40)
///                        Bearish when current filter value &lt; previous filter value
///   Layer 2 — Momentum:  RSI(30) below 50
///   Layer 3 — Validation: MACD histogram (24/52/9) below 0
///   Entry:               Bearish confirmation candle (close &lt; open)
/// </summary>
public class SignalGaussianScalpShort : SignalCreateBase
{
    private const int GaussianPeriod = 40;
    private const int GaussianLookback = GaussianPeriod * 3;

    public override bool IsSignal()
    {
        ExtraText = "";

        // --- Layer 2: RSI(30) < 50 ---
        if (CandleLast.CandleData!.Rsi30 == null)
        {
            ExtraText = "no RSI30 data";
            return false;
        }
        double rsi30 = CandleLast.CandleData.Rsi30.Value;
        if (rsi30 >= 50)
        {
            ExtraText = $"RSI30 {rsi30:N1} >= 50";
            return false;
        }

        // --- Layer 3: MACD(24/52/9) histogram < 0 ---
        if (CandleLast.CandleData.MacdHistogram24 == null)
        {
            ExtraText = "no MACD24 data";
            return false;
        }
        double macdHist = CandleLast.CandleData.MacdHistogram24.Value;
        if (macdHist >= 0)
        {
            ExtraText = $"MACD24 hist {macdHist:N6} >= 0";
            return false;
        }

        // --- Entry: bearish confirmation candle ---
        if (CandleLast.Candle.Close >= CandleLast.Candle.Open)
        {
            ExtraText = "no bearish confirmation candle";
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

        // Bearish: current filter value < previous filter value
        double filterNow = f[^1];
        double filterPrev = f[^2];
        if (filterNow >= filterPrev)
        {
            ExtraText = $"Gaussian bullish ({filterNow:N6} >= {filterPrev:N6})";
            return false;
        }

        ExtraText = $"G↓ RSI30={rsi30:N1} MACD={macdHist:N6}";
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
