using CryptoScanner.Core.Model;

// TODO duplicate of CryptoScanner.Core.Signal.Indicators.NweIndicator, but without the dependency on CryptoCandleList (which is in Core, not Chart)
namespace CryptoScanner.Chart.ViewModels.Chart;

// Nadaraya-Watson Envelope [LuxAlgo]

public class NweIndicator
{
    public int Length { get; }
    public double Bandwidth { get; }
    public decimal Multiplier { get; }
    public bool SmoothRepainting { get; }

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
    }

    public List<NweResult> Calculate(CryptoCandleList candles)
    {
        // Take a thread-safe snapshot first — enumerating SortedDictionary.Keys directly while
        // the kline ticker is adding candles concurrently throws ArgumentException / IndexOutOfRangeException
        // (see the note on CryptoCandleList.GetSnapshot for why .ToList() on the base type is unsafe).
        var snapshot = candles.GetSnapshot();
        int n = snapshot.Count;
        var openTimes = new List<CandleTime>(n);
        var closes = new List<decimal>(n);
        for (int i = 0; i < n; i++)
        {
            openTimes.Add(snapshot[i].Key);
            closes.Add(snapshot[i].Value.Close);
        }

        var results = new List<NweResult>(n);
        for (int k = 0; k < n; k++)
        {
            results.Add(new NweResult { OpenTime = openTimes[k] });
        }

        if (n < 1)
            return results;

        if (SmoothRepainting)
        {
            int window = Math.Min(Length, n);
            if (window < 1)
                return results;

            decimal sumAbs = 0m;
            var nwe = new List<decimal>(); // index 0 = newest to window-1: oldest
            for (int i = 0; i < window; i++) // i=0: newest
            {
                decimal sum = 0m;
                double sumw = 0.0;
                for (int j = 0; j < window; j++)
                {
                    double x = i - j;
                    double arg = Math.Pow(x, 2) / (Bandwidth * Bandwidth * 2.0);
                    double w = Math.Exp(-arg);
                    decimal srcj = closes[n - 1 - j];
                    sum += srcj * (decimal)w;
                    sumw += w;
                }
                decimal y2 = sum / (decimal)sumw;
                decimal srci = closes[n - 1 - i];
                sumAbs += Math.Abs(srci - y2);
                nwe.Add(y2);
            }

            int divider = Math.Min(Length - 1, n - 1); // = Windows - 1?
            decimal sae = sumAbs / divider * Multiplier;

            // Mapping reverse
            for (int i = 0; i < window; i++)
            {
                var center = nwe[i];
                int idx = n - 1 - i;
                results[idx].Center = center;
                results[idx].Upper = center + sae;
                results[idx].Lower = center - sae;
            }
            sae = sumAbs / divider * Multiplier;
        }
        else
        {
            // Non-repainting (endpoint estimation)
            var weights = new double[Length];
            for (int i = 0; i < Length; i++)
            {
                double arg = Math.Pow(i, 2) / (Bandwidth * Bandwidth * 2.0);
                weights[i] = Math.Exp(-arg);
            }

            for (int k = 0; k < n; k++)
            {
                int effectiveLen = Math.Min(Length, k + 1);
                decimal sum = 0m;
                double partialDen = 0.0;
                for (int i = 0; i < effectiveLen; i++)
                {
                    double w = weights[i];
                    decimal c = closes[k - i];
                    sum += c * (decimal)w;
                    partialDen += w;
                }
                if (partialDen > 0)
                {
                    results[k].Center = sum / (decimal)partialDen;
                }
            }

            var residuals = new List<decimal>();
            for (int k = 0; k < n; k++)
            {
                if (results[k].Center.HasValue)
                {
                    residuals.Add(Math.Abs(closes[k] - results[k].Center!.Value));
                }
            }

            int available = residuals.Count;
            int maeLen = Math.Min(Length - 1, available);
            if (maeLen > 0)
            {
                decimal sumAbs = 0m;
                for (int i = 0; i < maeLen; i++)
                {
                    sumAbs += residuals[available - 1 - i];
                }
                decimal mae = sumAbs / maeLen * Multiplier;
                for (int k = 0; k < n; k++)
                {
                    if (results[k].Center.HasValue)
                    {
                        results[k].Upper = results[k].Center + mae;
                        results[k].Lower = results[k].Center - mae;
                    }
                }
            }
        }

        return results;
    }
}
