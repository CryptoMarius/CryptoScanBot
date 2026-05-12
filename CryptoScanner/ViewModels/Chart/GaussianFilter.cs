using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class GaussianFilter
{
    private const int Period = 25;
    private const int Order = 5;
    private const double FilterDeviations = 1.0;
    private const int FilterPeriod = 10;
    private const int Warmup = 200; // extra bars before minDate for filter warm-up

    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        // Collect candles: warmup window + visible window, oldest first
        CandleTime warmupStart = minDate - Warmup * interval.Duration;
        var allCandles = symbolInterval.CandleList.Values
            .Where(c => c.OpenTime >= warmupStart && c.OpenTime <= maxDate)
            .ToList(); // SortedDictionary already ascending, no OrderBy needed

        if (allCandles.Count < Order + FilterPeriod + 3)
            return;

        double[] closes = allCandles.Select(c => (double)c.Close).ToArray();
        double[] raw = ComputeNPoleGaussian(closes);
        double[] filtered = ApplyStdFilter(raw);

        var seriesLine = new LineSeries
        {
            Title = "Gaussian",
            Color = OxyColor.FromArgb(210, 80, 160, 255),
            StrokeThickness = 1.5,
            Tag = group,
        };

        var seriesLong = new ScatterSeries
        {
            Title = "G long",
            MarkerSize = 5,
            MarkerFill = OxyColors.Lime,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };

        var seriesShort = new ScatterSeries
        {
            Title = "G short",
            MarkerSize = 5,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Diamond,
            Tag = group,
        };

        var seriesPullbackLong = new ScatterSeries
        {
            Title = "G pullback ↑",
            MarkerSize = 6,
            MarkerFill = OxyColor.FromArgb(220, 0, 220, 220),
            MarkerType = MarkerType.Circle,
            Tag = group,
        };

        var seriesPullbackShort = new ScatterSeries
        {
            Title = "G pullback ↓",
            MarkerSize = 6,
            MarkerFill = OxyColor.FromArgb(220, 255, 140, 0),
            MarkerType = MarkerType.Circle,
            Tag = group,
        };

        // Simulate contsw across all bars, emit visible points
        int contsw = 0;
        for (int i = 2; i < filtered.Length; i++)
        {
            double out0 = filtered[i];
            double out1 = filtered[i - 1];
            double out2 = filtered[i - 2];
            var candle = allCandles[i];

            bool pregoLong = out0 > out1 && out1 <= out2;
            bool pregoShort = out0 < out1 && out1 >= out2;

            int prevContsw = contsw;
            if (pregoLong) contsw = 1;
            else if (pregoShort) contsw = -1;

            if (candle.OpenTime < minDate || candle.OpenTime > maxDate)
                continue;

            seriesLine.Points.Add(new DataPoint(candle.OpenTime.Minutes, out0));

            if (pregoLong && prevContsw == -1)
                seriesLong.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, (double)candle.Low * 0.997));

            if (pregoShort && prevContsw == 1)
                seriesShort.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, (double)candle.High * 1.003));

            // Pullback long: confirmed uptrend, filter rising, wick touched line, close above
            if (contsw == 1 && out0 > out1
                && (double)candle.Low <= out0
                && (double)candle.Close > out0)
                seriesPullbackLong.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, out0));

            // Pullback short: confirmed downtrend, filter falling, wick touched line, close below
            if (contsw == -1 && out0 < out1
                && (double)candle.High >= out0
                && (double)candle.Close < out0)
                seriesPullbackShort.Points.Add(new ScatterPoint(candle.OpenTime.Minutes, out0));
        }

        chart.Series.Add(seriesLine);
        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
        chart.Series.Add(seriesPullbackLong);
        chart.Series.Add(seriesPullbackShort);
    }


    private static double[] ComputeNPoleGaussian(double[] src)
    {
        double a = CalculateAlpha(Period, Order);
        double powAN = Math.Pow(a, Order);

        double[] binomial = new double[Order + 1];
        double[] pow1mA = new double[Order + 1];
        for (int r = 0; r <= Order; r++)
        {
            binomial[r] = BinomialCoefficient(Order, r);
            pow1mA[r] = Math.Pow(1.0 - a, r);
        }

        double[] filt = new double[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            filt[i] = src[i] * powAN;
            int sign = 1;
            for (int r = 1; r <= Order; r++)
            {
                double prev = (i - r) >= 0 ? filt[i - r] : 0.0;
                filt[i] += sign * binomial[r] * pow1mA[r] * prev;
                sign *= -1;
            }
        }
        return filt;
    }


    private static double[] ApplyStdFilter(double[] src)
    {
        double[] price = new double[src.Length];
        price[0] = src[0];
        for (int i = 1; i < src.Length; i++)
        {
            int start = Math.Max(0, i - FilterPeriod + 1);
            int count = i - start + 1;

            double mean = 0;
            for (int j = start; j <= i; j++) mean += src[j];
            mean /= count;

            double variance = 0;
            for (int j = start; j <= i; j++) variance += (src[j] - mean) * (src[j] - mean);
            double stdev = count > 0 ? Math.Sqrt(variance / count) : 0.0; // population stdev, matching Pine ta.stdev biased=true

            price[i] = Math.Abs(src[i] - price[i - 1]) < FilterDeviations * stdev ? price[i - 1] : src[i];
        }
        return price;
    }


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
