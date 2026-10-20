using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.VwapMr;

/// <summary>
/// The VWAP band of the VWAP mean reversion strategy, one candle at a time:
///   vwap  = sum(hlc3 * volume) / sum(volume), summed from 00:00 UTC of the candle's day
///   upper = vwap + Mult * stdev(close, StdevLength)
///   lower = vwap - Mult * stdev(close, StdevLength)
/// The vwap is a session VWAP: it starts again on every UTC day. The stdev is the plain population
/// standard deviation of the last closes, not a volume-weighted one, and it does NOT restart at
/// midnight.
/// <para>
/// One class for the indicator hub (<see cref="Indicators.VwapMrIndicatorExtension"/>) and the
/// chart (<see cref="ComputeBands"/>), so the line on the chart is the line the signal reads.
/// </para>
/// <para>
/// A day VWAP needs every candle since midnight. The indicator warm-up feeds 260 candles, which on
/// 1m covers only the last 4h20: a day that was already running when the first candle came in is
/// marked incomplete and gets no value, rather than a VWAP of a part of the day. The next day is
/// complete, because the calculator saw it start.
/// </para>
/// </summary>
public sealed class VwapMrBandCalculator
{
    /// <summary>One candle's band values; <see cref="HasValue"/> is false when there is no complete day VWAP or not enough closes yet.</summary>
    public readonly struct BandValue
    {
        public readonly bool HasValue;
        public readonly double Vwap;
        public readonly double Upper;
        public readonly double Lower;

        public BandValue(double vwap, double upper, double lower)
        {
            HasValue = true;
            Vwap = vwap;
            Upper = upper;
            Lower = lower;
        }
    }

    private readonly int _stdevLength;
    private readonly double _mult;

    // Ring buffer of the last closes for the stdev.
    private readonly double[] _closes;
    private int _closeCount;
    private int _closeNext;

    // The running day.
    private long _day = long.MinValue;
    private bool _dayComplete;
    private double _sumPriceVolume;
    private double _sumVolume;

    public VwapMrBandCalculator(int stdevLength, double mult)
    {
        _stdevLength = Math.Max(1, stdevLength);
        _mult = mult;
        _closes = new double[_stdevLength];
    }

    /// <summary>Feeds one candle (ascending open time) and returns its band.</summary>
    public BandValue Add(DateTime timestamp, decimal high, decimal low, decimal close, decimal volume)
    {
        // The UTC day of the candle. CandleTime.ToDateTime is UTC, so the ticks divide into days
        // without a time zone in between.
        long day = timestamp.Ticks / TimeSpan.TicksPerDay;
        if (day != _day)
        {
            // Complete when the calculator saw the previous day end (it was fed before this one),
            // or when this candle opens exactly at midnight. A market with opening hours starts its
            // first day mid-day and its later days on the transition, the same rule.
            _dayComplete = _day != long.MinValue || timestamp.TimeOfDay == TimeSpan.Zero;
            _day = day;
            _sumPriceVolume = 0;
            _sumVolume = 0;
        }

        double hlc3 = (double)(high + low + close) / 3.0;
        double vol = (double)volume;
        _sumPriceVolume += hlc3 * vol;
        _sumVolume += vol;

        _closes[_closeNext] = (double)close;
        _closeNext = (_closeNext + 1) % _stdevLength;
        if (_closeCount < _stdevLength)
            _closeCount++;

        // A flat minute without trades has no volume: it does not move the VWAP. A day that has had
        // no volume at all yet has no VWAP (zero divided by zero).
        if (!_dayComplete || _sumVolume <= 0 || _closeCount < _stdevLength)
            return default;

        double vwap = _sumPriceVolume / _sumVolume;
        double pad = _mult * PopulationStdev();
        return new BandValue(vwap, vwap + pad, vwap - pad);
    }

    private double PopulationStdev()
    {
        double sum = 0;
        for (int i = 0; i < _stdevLength; i++)
            sum += _closes[i];
        double mean = sum / _stdevLength;

        double squares = 0;
        for (int i = 0; i < _stdevLength; i++)
        {
            double d = _closes[i] - mean;
            squares += d * d;
        }
        return Math.Sqrt(squares / _stdevLength);
    }

    /// <summary>
    /// The band for every candle in <paramref name="candles"/>, index-aligned (result[i] belongs to
    /// candles[i]). Used by the chart; the signal reads the same values from the indicator hub.
    /// </summary>
    public static BandValue[] ComputeBands(IReadOnlyList<CryptoCandle> candles)
    {
        var settings = VwapMrPlugin.Settings;
        var calculator = new VwapMrBandCalculator(settings.StdevLength, settings.Mult);
        var result = new BandValue[candles.Count];
        for (int i = 0; i < candles.Count; i++)
        {
            CryptoCandle c = candles[i];
            result[i] = calculator.Add(c.Timestamp, c.High, c.Low, c.Close, c.Volume);
        }
        return result;
    }
}
