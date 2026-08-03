using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.Nwe.Signal;

// Nadaraya-Watson Envelope [LuxAlgo]

public class NweIndicator
{
    public int Length { get; }
    public double Bandwidth { get; }
    public decimal Multiplier { get; }
    public bool SmoothRepainting { get; }

    // Precomputed Gaussian kernel weights: w[d] = exp(-d^2 / (2 * h^2))
    // Only distances up to _effectiveRange contribute meaningfully (beyond ~4*bandwidth the weight is < 0.04%).
    private readonly double[] _weights;
    private readonly int _effectiveRange;

    public class NweResult
    {
        public CandleTime OpenTime { get; set; }
        public decimal? Center { get; set; }
        public decimal? Upper { get; set; }
        public decimal? Lower { get; set; }
    }

    public NweIndicator(int length = 500, double bandwidth = 8.0, decimal multiplier = 3m, bool smoothRepainting = true)
    {
        Length = length;
        Bandwidth = bandwidth;
        Multiplier = multiplier;
        SmoothRepainting = smoothRepainting;

        double h2 = bandwidth * bandwidth * 2.0;
        _effectiveRange = Math.Max(1, Math.Min(length, (int)(4.0 * bandwidth) + 1));
        _weights = new double[_effectiveRange];
        for (int d = 0; d < _effectiveRange; d++)
            _weights[d] = Math.Exp(-(double)(d * d) / h2);
    }

    public List<NweResult> Calculate(CryptoCandleList candles)
    {
        // Take a thread-safe snapshot first — enumerating SortedDictionary.Keys directly while
        // the kline ticker is adding candles concurrently throws ArgumentException / IndexOutOfRangeException
        // (see the note on CryptoCandleList.GetSnapshot for why .ToList() on the base type is unsafe).
        var snapshot = candles.GetSnapshot();
        int n = snapshot.Count;
        var openTimes = new CandleTime[n];
        var dCloses = new double[n];
        for (int i = 0; i < n; i++)
        {
            openTimes[i] = snapshot[i].Key;
            dCloses[i] = (double)snapshot[i].Value.Close;
        }

        return CalculateCore(dCloses, openTimes, n);
    }

    internal List<NweResult> CalculateCore(double[] dCloses, CandleTime[] openTimes, int n)
    {
        var results = new List<NweResult>(n);
        for (int k = 0; k < n; k++)
        {
            results.Add(new NweResult { OpenTime = openTimes[k] });
        }

        if (n < 1)
            return results;

        double mult = (double)Multiplier;

        if (SmoothRepainting)
        {
            int window = Math.Min(Length, n);
            if (window < 1)
                return results;

            double sumAbs = 0.0;
            var centers = new double[window]; // index 0 = newest to window-1: oldest
            for (int i = 0; i < window; i++) // i=0: newest
            {
                double sum = 0.0;
                double sumw = 0.0;
                int jStart = Math.Max(0, i - _effectiveRange + 1);
                int jEnd = Math.Min(window, i + _effectiveRange);
                for (int j = jStart; j < jEnd; j++)
                {
                    int d = i >= j ? i - j : j - i;
                    double wt = _weights[d];
                    sum += dCloses[n - 1 - j] * wt;
                    sumw += wt;
                }
                double y = sum / sumw;
                centers[i] = y;
                sumAbs += Math.Abs(dCloses[n - 1 - i] - y);
            }

            int divider = Math.Max(1, Math.Min(Length - 1, n - 1)); // = Windows - 1?
            double sae = sumAbs / divider * mult;

            // Mapping reverse
            for (int i = 0; i < window; i++)
            {
                int idx = n - 1 - i;
                results[idx].Center = (decimal)centers[i];
                results[idx].Upper = (decimal)(centers[i] + sae);
                results[idx].Lower = (decimal)(centers[i] - sae);
            }
        }
        else
        {
            // Non-repainting (endpoint estimation)
            for (int k = 0; k < n; k++)
            {
                int effectiveLen = Math.Min(Math.Min(Length, k + 1), _effectiveRange);
                double sum = 0.0;
                double partialDen = 0.0;
                for (int i = 0; i < effectiveLen; i++)
                {
                    sum += dCloses[k - i] * _weights[i];
                    partialDen += _weights[i];
                }
                if (partialDen > 0)
                {
                    results[k].Center = (decimal)(sum / partialDen);
                }
            }

            int maeLen = Math.Min(Length - 1, n);
            if (maeLen > 0)
            {
                double sumAbs = 0.0;
                int startK = n - maeLen;
                for (int k = startK; k < n; k++)
                {
                    if (results[k].Center.HasValue)
                        sumAbs += Math.Abs(dCloses[k] - (double)results[k].Center!.Value);
                }
                double mae = sumAbs / maeLen * mult;
                for (int k = 0; k < n; k++)
                {
                    if (results[k].Center.HasValue)
                    {
                        double c = (double)results[k].Center.Value;
                        results[k].Upper = (decimal)(c + mae);
                        results[k].Lower = (decimal)(c - mae);
                    }
                }
            }
        }

        return results;
    }
}
