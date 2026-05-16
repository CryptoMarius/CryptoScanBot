using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Indicators;

// WaveTrend Oscillator [LazyBear] — short title "WT_LB".
//
// Pine source (TradingView, by LazyBear):
//     ap   = hlc3
//     esa  = ema(ap, n1)
//     d    = ema(abs(ap - esa), n1)
//     ci   = (ap - esa) / (0.015 * d)
//     wt1  = ema(ci, n2)
//     wt2  = sma(wt1, 4)
public class WaveTrendIndicator
{
    public int ChannelLength { get; }   // n1, default 10
    public int AverageLength { get; }   // n2, default 21

    public class WtResult
    {
        public CandleTime OpenTime { get; set; }
        public double? Wt1 { get; set; }   // TCI (fast line, green)
        public double? Wt2 { get; set; }   // SMA(Wt1, 4) (slow line, red)
    }

    public WaveTrendIndicator(int channelLength = 10, int averageLength = 21)
    {
        ChannelLength = channelLength;
        AverageLength = averageLength;
    }

    public List<WtResult> Calculate(CryptoCandleList candles)
    {
        // Thread-safe snapshot — same reasoning as NweIndicator (see CryptoCandleList.GetSnapshot).
        var snapshot = candles.GetSnapshot();
        int n = snapshot.Count;

        var results = new List<WtResult>(n);
        for (int i = 0; i < n; i++)
            results.Add(new WtResult { OpenTime = snapshot[i].Key });

        if (n == 0)
            return results;

        double k1 = 2.0 / (ChannelLength + 1);
        double k2 = 2.0 / (AverageLength + 1);

        // ESA = EMA(hlc3, n1)
        var hlc3 = new double[n];
        var esa = new double[n];
        for (int i = 0; i < n; i++)
        {
            var c = snapshot[i].Value;
            hlc3[i] = ((double)c.High + (double)c.Low + (double)c.Close) / 3.0;
            esa[i] = i == 0 ? hlc3[i] : hlc3[i] * k1 + esa[i - 1] * (1 - k1);
        }

        // D = EMA(|hlc3 - esa|, n1)
        var d = new double[n];
        for (int i = 0; i < n; i++)
        {
            double absDiff = Math.Abs(hlc3[i] - esa[i]);
            d[i] = i == 0 ? absDiff : absDiff * k1 + d[i - 1] * (1 - k1);
        }

        // WT1 = EMA(CI, n2)
        var wt1 = new double[n];
        for (int i = 0; i < n; i++)
        {
            double denom = 0.015 * d[i];
            double ci = denom == 0 ? 0 : (hlc3[i] - esa[i]) / denom;
            wt1[i] = i == 0 ? ci : ci * k2 + wt1[i - 1] * (1 - k2);
        }

        // WT2 = SMA(WT1, 4). The first three bars don't have a 4-bar window yet — leave Wt2 null.
        for (int i = 0; i < n; i++)
        {
            results[i].Wt1 = wt1[i];
            if (i >= 3)
                results[i].Wt2 = (wt1[i] + wt1[i - 1] + wt1[i - 2] + wt1[i - 3]) / 4.0;
        }

        return results;
    }
}
