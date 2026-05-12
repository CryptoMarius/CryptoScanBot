namespace CryptoScanner.Core.Signal.Gaussian;

/// <summary>
/// Shared implementation of the STD-Filtered N-Pole Gaussian Filter [Loxx].
///
/// Ported from Pine Script v5 by Loxx:
///   https://www.tradingview.com/script/...
///
/// Default settings (matching TradingView defaults):
///   Period       = 25
///   Order        = 5  (N poles)
///   Filter dev   = 1.0 standard deviations
///   Filter period= 10 bars
///   Filter target= Gaussian output (not price)
///
/// Alpha formula (Loxx):
///   w = 2π / period
///   b = (1 - cos(w)) / (1.414^(2/poles) - 1)
///   a = -b + sqrt(b² + 2b)
///
/// N-pole filter per bar:
///   filt[0] = src * a^order
///            + Σ (sign * C(order,r) * (1-a)^r * filt[-r])   for r = 1..order
///   sign alternates: +1, -1, +1, ...
///
/// STD filter (applied to Gaussian output):
///   Only update output if |out - prevOut| >= filterDev * stdev(out, filterPeriod)
///
/// Signal conditions (goLong / goShort):
///   pregoLong  = out > prevOut  AND prevOut <= prevPrevOut   (just turned up)
///   pregoShort = out &lt; prevOut  AND prevOut >= prevPrevOut   (just turned down)
///   contsw     = last confirmed direction (+1 long / -1 short), persists
///   goLong     = pregoLong  AND contsw was -1  (first bullish flip after bearish period)
///   goShort    = pregoShort AND contsw was +1  (first bearish flip after bullish period)
/// </summary>
public abstract class SignalGaussianScalpBase : SignalCreateBase
{
    // Default settings — match Loxx TradingView defaults
    protected const int GaussianPeriod = 25;
    protected const int GaussianOrder = 5;
    protected const double FilterDeviations = 1.0;
    protected const int FilterPeriod = 10;

    // Enough candles for filter warm-up + STD calculation
    private const int Lookback = 200;


    /// <summary>
    /// Collects candle history, computes the filtered Gaussian output,
    /// runs the contsw simulation and returns goLong / goShort for the current bar.
    /// </summary>
    protected bool ComputeSignal(out bool goLong, out bool goShort)
    {
        goLong = false;
        goShort = false;

        if (!TryComputeFiltered(out double[] filtered))
            return false;

        int contsw = 0;
        for (int i = 2; i < filtered.Length; i++)
        {
            double out0 = filtered[i];
            double out1 = filtered[i - 1];
            double out2 = filtered[i - 2];

            bool pregoLong = out0 > out1 && out1 <= out2;
            bool pregoShort = out0 < out1 && out1 >= out2;

            int prevContsw = contsw;
            if (pregoLong) contsw = 1;
            else if (pregoShort) contsw = -1;

            if (i == filtered.Length - 1)
            {
                goLong = pregoLong && prevContsw == -1;
                goShort = pregoShort && prevContsw == 1;
            }
        }

        return true;
    }


    /// <summary>
    /// Returns the current Gaussian filter value, the previous value, and the running
    /// contsw trend state (+1 uptrend / -1 downtrend / 0 undecided) at the current bar.
    /// Used by pullback signals that trade bounces off the filter line.
    /// </summary>
    protected bool ComputeGaussianState(out double filteredLast, out double filteredPrev, out int contswLast)
    {
        filteredLast = 0;
        filteredPrev = 0;
        contswLast = 0;

        if (!TryComputeFiltered(out double[] filtered))
            return false;

        filteredLast = filtered[^1];
        filteredPrev = filtered[^2];

        int contsw = 0;
        for (int i = 2; i < filtered.Length; i++)
        {
            double out0 = filtered[i];
            double out1 = filtered[i - 1];
            double out2 = filtered[i - 2];
            bool pregoLong = out0 > out1 && out1 <= out2;
            bool pregoShort = out0 < out1 && out1 >= out2;
            if (pregoLong) contsw = 1;
            else if (pregoShort) contsw = -1;
        }
        contswLast = contsw;

        return true;
    }


    private bool TryComputeFiltered(out double[] filtered)
    {
        filtered = [];

        var closes = new List<double>(Lookback);
        MyData? candle = CandleLast;
        for (int i = 0; i < Lookback; i++)
        {
            closes.Add((double)candle!.Candle.Close);
            if (!GetPrevCandle(candle, out candle))
                break;
        }

        if (closes.Count < GaussianOrder + FilterPeriod + 3)
            return false;

        closes.Reverse(); // oldest first

        double[] raw = ComputeNPoleGaussian(closes);
        filtered = ApplyStdFilter(raw, FilterPeriod, FilterDeviations);
        return true;
    }


    /// <summary>
    /// N-pole Gaussian filter using Loxx's binomial coefficient approach.
    /// filt[i] = src[i] * a^order  +  Σ sign * C(order,r) * (1-a)^r * filt[i-r]
    /// </summary>
    private static double[] ComputeNPoleGaussian(List<double> src)
    {
        double a = CalculateAlpha(GaussianPeriod, GaussianOrder);

        // Precompute binomial coefficients and powers
        double[] binomial = new double[GaussianOrder + 1];
        double[] pow1mA = new double[GaussianOrder + 1];
        double powAN = Math.Pow(a, GaussianOrder);
        for (int r = 0; r <= GaussianOrder; r++)
        {
            binomial[r] = BinomialCoefficient(GaussianOrder, r);
            pow1mA[r] = Math.Pow(1.0 - a, r);
        }

        double[] filt = new double[src.Count];
        for (int i = 0; i < src.Count; i++)
        {
            filt[i] = src[i] * powAN;
            int sign = 1;
            for (int r = 1; r <= GaussianOrder; r++)
            {
                // nz(filt[r]) = 0 when index < 0 (Pine behaviour)
                double prev = (i - r) >= 0 ? filt[i - r] : 0.0;
                filt[i] += sign * binomial[r] * pow1mA[r] * prev;
                sign *= -1;
            }
        }
        return filt;
    }


    /// <summary>
    /// STD filter: only update the output if the change exceeds filterDev * stdev(src, len).
    /// Equivalent to Pine's _filt() function.
    /// </summary>
    private static double[] ApplyStdFilter(double[] src, int len, double filterDev)
    {
        double[] price = new double[src.Length];
        price[0] = src[0];
        for (int i = 1; i < src.Length; i++)
        {
            int start = Math.Max(0, i - len + 1);
            int count = i - start + 1;

            // Rolling mean
            double mean = 0;
            for (int j = start; j <= i; j++) mean += src[j];
            mean /= count;

            // Rolling stdev (sample)
            double variance = 0;
            for (int j = start; j <= i; j++) variance += (src[j] - mean) * (src[j] - mean);
            double stdev = count > 0 ? Math.Sqrt(variance / count) : 0.0; // population stdev (n), matching Pine ta.stdev biased=true default

            double filtdev = filterDev * stdev;
            price[i] = Math.Abs(src[i] - price[i - 1]) < filtdev ? price[i - 1] : src[i];
        }
        return price;
    }


    /// <summary>
    /// Alpha calculation from Loxx's Pine script.
    /// w = 2π/period;  b = (1-cos(w)) / (1.414^(2/poles) - 1);  a = -b + sqrt(b² + 2b)
    /// </summary>
    private static double CalculateAlpha(int period, int poles)
    {
        double w = 2.0 * Math.PI / period;
        double b = (1.0 - Math.Cos(w)) / (Math.Pow(1.414, 2.0 / poles) - 1.0);
        return -b + Math.Sqrt(b * b + 2.0 * b);
    }


    private static double Factorial(int n)
    {
        double result = 1;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }

    private static double BinomialCoefficient(int n, int r)
    {
        if (r == 0 || r == n) return 1;
        return Factorial(n) / (Factorial(n - r) * Factorial(r));
    }
}
