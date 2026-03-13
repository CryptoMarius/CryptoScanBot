using CryptoScanner.Core.Model;

using MathNet.Numerics.IntegralTransforms;

using System.Numerics;

namespace CryptoScanner.Core.Signal.Experiment;

public class HarmonicComponent
{
    /// <summary>Periode in aantal candles (bijv. 24 = dagcyclus bij 1h candles)</summary>
    public double PeriodInCandles { get; init; }

    /// <summary>Amplitude: hoe sterk deze cyclus aanwezig is in de prijs</summary>
    public double Amplitude { get; init; }

    /// <summary>Fase in radialen (0..2π): waar in de cyclus bevinden we ons nu?</summary>
    public double PhaseRadians { get; init; }

    /// <summary>Verwachte prijs op moment T=0 (huidig) op basis van deze component</summary>
    public double CurrentCycleValue { get; init; }
}

public class RollingFftAnalyzer
{
    private readonly int _windowSize;
    private readonly int _topComponents;

    /// <param name="windowSize">
    /// Number of candles per FFT window. Must be a power of 2: 64, 128, 256.
    /// A larger window detects longer cycles but reacts more slowly to recent market changes.
    /// </param>
    /// <param name="topComponents">Number of dominant harmonic components to return</param>
    public RollingFftAnalyzer(int windowSize = 128, int topComponents = 3)
    {
        if ((windowSize & (windowSize - 1)) != 0)
            throw new ArgumentException("windowSize must be a power of 2 (64, 128, 256...)");

        _windowSize = windowSize;
        _topComponents = topComponents;
    }

    /// <summary>
    /// Analyzes the most recent N candles and returns the dominant harmonic components.
    /// Call this every time a new candle is added.
    /// </summary>
    public IReadOnlyList<HarmonicComponent> Analyze(List<CryptoCandle> candles)
    {
        if (candles.Count < _windowSize)
            return [];

        // Step 1: compute trend parameters directly on the rightmost window of the candle list
        ComputeTrendParameters(candles, out double slope, out double intercept);

        // Step 2: build Complex[] in a single pass: detrend + Hann window + conversion combined
        Complex[] complexSignal = BuildComplexSignal(candles, slope, intercept);

        // Step 3: perform FFT in-place
        Fourier.Forward(complexSignal, FourierOptions.Matlab);

        // Step 4: extract dominant frequencies from the positive half of the spectrum
        // Bin 0 is the DC component (the mean) and is skipped
        int halfLength = _windowSize / 2;

        var components = new List<(double Amplitude, int BinIndex, double Phase)>(halfLength);

        for (int i = 1; i < halfLength; i++)
        {
            double amplitude = complexSignal[i].Magnitude * 2.0 / _windowSize;
            double phase = Math.Atan2(complexSignal[i].Imaginary, complexSignal[i].Real);
            components.Add((amplitude, i, phase));
        }

        return components
            .OrderByDescending(c => c.Amplitude)
            .Take(_topComponents)
            .Select(c =>
            {
                double periodInCandles = (double)_windowSize / c.BinIndex;

                // Current position within the cycle: how far the price deviates from equilibrium
                double currentCycleValue = c.Amplitude * Math.Cos(c.Phase);

                return new HarmonicComponent
                {
                    PeriodInCandles = periodInCandles,
                    Amplitude = c.Amplitude,
                    PhaseRadians = c.Phase,
                    CurrentCycleValue = currentCycleValue
                };
            })
            .ToList();
    }

    /// <summary>
    /// Computes an oscillation score based on the dominant harmonic components.
    /// Indicates how far the current price deviates from its harmonic equilibrium.
    ///
    /// Score is roughly between -1.0 and +1.0:
    ///   +1.0 = price is at the top of the cycle (overbought hint)
    ///   -1.0 = price is at the bottom of the cycle (oversold hint)
    ///    0.0 = price is near equilibrium
    /// </summary>
    public double ComputeOscillationScore(List<CryptoCandle> candles)
    {
        IReadOnlyList<HarmonicComponent> components = Analyze(candles);
        if (components.Count == 0)
            return 0.0;

        double totalWeight = components.Sum(c => c.Amplitude);
        if (totalWeight == 0)
            return 0.0;

        // Weighted average of normalized cycle values across all dominant components
        double score = components.Sum(c => (c.CurrentCycleValue / c.Amplitude) * c.Amplitude);
        return score / totalWeight;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Computes the slope and intercept of the linear trend via ordinary least squares.
    /// Iterates the rightmost _windowSize candles directly without an intermediate array.
    /// </summary>
    private void ComputeTrendParameters(List<CryptoCandle> candles, out double slope, out double intercept)
    {
        double xMean = (_windowSize - 1) / 2.0;

        // Compute yMean over the rightmost window
        double ySum = 0.0;
        int offset = candles.Count - _windowSize;
        for (int i = 0; i < _windowSize; i++)
            ySum += (double)candles[offset + i].Close;
        double yMean = ySum / _windowSize;

        double numerator = 0.0;
        double denominator = 0.0;
        for (int i = 0; i < _windowSize; i++)
        {
            double dx = i - xMean;
            numerator += dx * ((double)candles[offset + i].Close - yMean);
            denominator += dx * dx;
        }

        slope = denominator != 0 ? numerator / denominator : 0.0;
        intercept = yMean - slope * xMean;
    }

    /// <summary>
    /// Builds the complex signal in a single pass over the rightmost window of the candle list.
    /// Detrending, Hann windowing and conversion to Complex[] are combined
    /// so the list is traversed exactly once with no intermediate arrays.
    /// </summary>
    private Complex[] BuildComplexSignal(List<CryptoCandle> candles, double slope, double intercept)
    {
        var signal = new Complex[_windowSize];
        int offset = candles.Count - _windowSize;

        for (int i = 0; i < _windowSize; i++)
        {
            // Remove the linear trend so the FFT only sees the oscillation
            double detrended = (double)candles[offset + i].Close - (slope * i + intercept);

            // Hann window reduces spectral leakage at the edges of the window
            double hann = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (_windowSize - 1)));

            signal[i] = new Complex(detrended * hann, 0);
        }

        return signal;
    }
}


//// Aanmaken (eenmalig per coin, of gedeeld)
//var fftAnalyzer = new RollingFftAnalyzer(windowSize: 128, topComponents: 3);

//    // Bij elke nieuwe candle:
//    var closes = coin.Candles.Select(c => c.Close).ToList();

//    var harmonics = fftAnalyzer.Analyze(closes);
//foreach (var h in harmonics)
//{
//    // PeriodInCandles bij 1h-candles: 24 = dagcyclus, 168 = weekcyclus
//    Console.WriteLine($"Cyclus: {h.PeriodInCandles:F1} candles | " +
//                      $"Amplitude: {h.Amplitude:F4} | " +
//                      $"Fase: {h.PhaseRadians:F2} rad");
//}

//// Score voor mean-reversion signaal (-1 = oversold, +1 = overbought tov cyclus)
//double score = fftAnalyzer.ComputeOscillationScore(closes);
//if (score > 0.7)
//    Console.WriteLine("⚠️ Prijs aan bovenkant harmonische cyclus → mogelijke reversal");
//else if (score < -0.7)
//    Console.WriteLine("⚠️ Prijs aan onderkant harmonische cyclus → mogelijke bounce");