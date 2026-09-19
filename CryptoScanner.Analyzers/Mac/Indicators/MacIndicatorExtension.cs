using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Mac.Indicators;

/// <summary>
/// The four moving averages that make up the MAC cloud, plus the pivot highs and lows that stand in
/// for its auto-plotted support and resistance.
/// <para>
/// The lines are EMA(20), EMA(40), SMA(50) and SMA(150) on the close, and the four lengths belong
/// together - see MacSettings. They come from the registry, so a length another plugin already
/// asked for is the same hub instead of a second one doing identical work on every candle.
/// </para>
/// <para>
/// The pivots are ours: a small ring buffer of the last left+right+1 candles, checked once per
/// candle, which keeps the level at hand instead of walking the candle history from the signal
/// class on every evaluation.
/// </para>
/// </summary>
public class MacIndicatorExtension : IIndicatorExtension
{
    private EmaHub? _emaFast;
    private EmaHub? _emaSecond;
    private SmaHub? _smaMedium;
    private SmaHub? _smaSlow;
    private int _slopeLookback;

    private int _left;
    private int _right;

    // The RSI levels, when they are switched on: their own hub, their own ring buffer and their own
    // half-window, because an RSI turn is a different event from a price turn and needs more room
    // around it - ten candles either side rather than five.
    private RsiHub? _rsi;
    private bool _useRsiLevels;
    private int _rsiHalfWindow;
    private double _rsiOverbought;
    private double _rsiOversold;
    private double[] _rsiValues = [];
    private decimal[] _rsiHighs = [];
    private decimal[] _rsiLows = [];
    private int _rsiFilled;
    private int _rsiNext;
    private double? _rsiLevelHigh;
    private long _rsiLevelHighIndex;
    private double? _rsiLevelLow;
    private long _rsiLevelLowIndex;

    // The ring buffer of the last _left + _right + 1 candles. _next is the slot the next candle
    // overwrites, which while the buffer is full is also the oldest candle in it.
    private decimal[] _highs = [];
    private decimal[] _lows = [];
    private int _filled;
    private int _next;

    /// <summary>Candles fed to this extension. The newest one has index _candleCount - 1.</summary>
    private long _candleCount;

    private double? _pivotHigh;
    private long _pivotHighIndex;
    private double? _pivotLow;
    private long _pivotLowIndex;


    public void Init(IndicatorRegistry registry)
    {
        MacSettings settings = MacPlugin.Settings;

        // Clamped rather than trusted: a second line that is not slower than the first would give a
        // crossing without meaning, and a slow line shorter than the medium one the same.
        int fast = Math.Max(1, settings.FastEmaLength);
        int second = Math.Max(fast + 1, settings.SecondEmaLength);
        int medium = Math.Max(2, settings.MediumSmaLength);
        int slow = Math.Max(medium + 1, settings.SlowSmaLength);

        _emaFast = registry.Ema(fast);
        _emaSecond = registry.Ema(second);
        _smaMedium = registry.Sma(medium);
        _smaSlow = registry.Sma(slow);
        _slopeLookback = Math.Max(1, settings.SlowLineLookbackCandles);

        _useRsiLevels = settings.UseRsiLevels;
        if (_useRsiLevels)
        {
            _rsi = registry.Rsi(Math.Max(2, settings.RsiLevelLength));
            _rsiHalfWindow = Math.Max(1, settings.RsiLevelPivotCandles);
            _rsiOverbought = (double)settings.RsiLevelOverbought;
            _rsiOversold = (double)settings.RsiLevelOversold;
            int rsiWindow = 2 * _rsiHalfWindow + 1;
            _rsiValues = new double[rsiWindow];
            _rsiHighs = new decimal[rsiWindow];
            _rsiLows = new decimal[rsiWindow];
            _rsiFilled = 0;
            _rsiNext = 0;
            _rsiLevelHigh = null;
            _rsiLevelLow = null;
        }

        _left = Math.Max(1, settings.PivotLeftCandles);
        _right = Math.Max(1, settings.PivotRightCandles);
        int window = _left + _right + 1;
        _highs = new decimal[window];
        _lows = new decimal[window];
        _filled = 0;
        _next = 0;
        _candleCount = 0;
    }


    public void OnCandleAdded(IQuote candle)
    {
        if (_highs.Length == 0)
            return;

        int window = _highs.Length;
        _highs[_next] = candle.High;
        _lows[_next] = candle.Low;
        _next = (_next + 1) % window;
        if (_filled < window)
            _filled++;
        _candleCount++;

        if (_useRsiLevels)
            TrackRsiLevel(candle);

        // A pivot needs its right-hand candles before it can be called one, so the candidate sits
        // _right candles back and is only looked at once the whole window is there.
        if (_filled < window)
            return;

        decimal candidateHigh = ValueBack(_highs, _right);
        decimal candidateLow = ValueBack(_lows, _right);
        bool isHigh = true;
        bool isLow = true;
        for (int back = 0; back < window; back++)
        {
            if (back == _right)
                continue;
            if (isHigh && ValueBack(_highs, back) >= candidateHigh)
                isHigh = false;
            if (isLow && ValueBack(_lows, back) <= candidateLow)
                isLow = false;
            if (!isHigh && !isLow)
                break;
        }

        if (isHigh)
        {
            _pivotHigh = (double)candidateHigh;
            _pivotHighIndex = _candleCount - 1 - _right;
        }
        if (isLow)
        {
            _pivotLow = (double)candidateLow;
            _pivotLowIndex = _candleCount - 1 - _right;
        }
    }


    /// <summary>
    /// The RSI levels: the candle whose RSI is the extreme of its window AND past the threshold.
    /// Its HIGH becomes the resistance and its LOW the support - the price the turn happened at,
    /// not the RSI value itself.
    /// </summary>
    private void TrackRsiLevel(IQuote candle)
    {
        if (_rsi == null || _rsiValues.Length == 0)
            return;
        var results = _rsi.Results;
        if (results.Count == 0 || results[^1].Rsi == null)
            return;

        int window = _rsiValues.Length;
        _rsiValues[_rsiNext] = results[^1].Rsi!.Value;
        _rsiHighs[_rsiNext] = candle.High;
        _rsiLows[_rsiNext] = candle.Low;
        _rsiNext = (_rsiNext + 1) % window;
        if (_rsiFilled < window)
        {
            _rsiFilled++;
            return;
        }

        double candidate = ValueBack(_rsiValues, _rsiHalfWindow);
        bool isHigh = candidate >= _rsiOverbought;
        bool isLow = candidate <= _rsiOversold;
        for (int back = 0; back < window && (isHigh || isLow); back++)
        {
            if (back == _rsiHalfWindow)
                continue;
            double other = ValueBack(_rsiValues, back);
            if (isHigh && other >= candidate)
                isHigh = false;
            if (isLow && other <= candidate)
                isLow = false;
        }

        if (isHigh)
        {
            _rsiLevelHigh = (double)ValueBackRsi(_rsiHighs, _rsiHalfWindow);
            _rsiLevelHighIndex = _candleCount - 1 - _rsiHalfWindow;
        }
        if (isLow)
        {
            _rsiLevelLow = (double)ValueBackRsi(_rsiLows, _rsiHalfWindow);
            _rsiLevelLowIndex = _candleCount - 1 - _rsiHalfWindow;
        }
    }


    /// <summary>
    /// The value <paramref name="back"/> candles before the newest one in the ring buffer, zero
    /// being the newest. Only called while the buffer is full.
    /// </summary>
    private decimal ValueBack(decimal[] buffer, int back)
    {
        int window = buffer.Length;
        int index = (_next - 1 - back + 2 * window) % window;
        return buffer[index];
    }


    /// <summary>The same walk over the RSI ring buffer, which runs on its own index and window.</summary>
    private double ValueBack(double[] buffer, int back)
    {
        int window = buffer.Length;
        int index = (_rsiNext - 1 - back + 2 * window) % window;
        return buffer[index];
    }


    /// <summary>The price ring buffers of the RSI levels, which share the RSI index.</summary>
    private decimal ValueBackRsi(decimal[] buffer, int back)
    {
        int window = buffer.Length;
        int index = (_rsiNext - 1 - back + 2 * window) % window;
        return buffer[index];
    }


    public void FillData(CryptoData data)
    {
        double? fast = _emaFast?.Results.Count > 0 ? _emaFast.Results[^1].Ema : null;
        double? second = _emaSecond?.Results.Count > 0 ? _emaSecond.Results[^1].Ema : null;
        double? medium = _smaMedium?.Results.Count > 0 ? _smaMedium.Results[^1].Sma : null;
        double? slow = _smaSlow?.Results.Count > 0 ? _smaSlow.Results[^1].Sma : null;

        // The slope of the slow line, read straight out of the hub's own results: the value now
        // against the one _slopeLookback candles back. No walk, no state of our own.
        double? slope = null;
        var slowResults = _smaSlow?.Results;
        if (slowResults != null && slowResults.Count > _slopeLookback)
        {
            double? now = slowResults[^1].Sma;
            double? then = slowResults[^(1 + _slopeLookback)].Sma;
            if (now != null && then != null && then.Value != 0)
                slope = 100.0 * (now.Value - then.Value) / Math.Abs(then.Value);
        }

        // Nothing computed yet during warm-up - leave the slot empty instead of attaching an
        // all-null object, so the strategy can tell "not ready" from "ready but zero".
        if (fast == null && second == null && medium == null && slow == null
            && _pivotHigh == null && _pivotLow == null
            && _rsiLevelHigh == null && _rsiLevelLow == null)
            return;

        long newest = _candleCount - 1;
        data.SetPluginData(new MacCandleData
        {
            EmaFast = fast,
            EmaSecond = second,
            SmaMedium = medium,
            SmaSlow = slow,
            SlowSlopePercentage = slope,
            PivotHigh = _pivotHigh,
            PivotHighAge = _pivotHigh == null ? 0 : (int)Math.Min(int.MaxValue, newest - _pivotHighIndex),
            PivotLow = _pivotLow,
            PivotLowAge = _pivotLow == null ? 0 : (int)Math.Min(int.MaxValue, newest - _pivotLowIndex),
            RsiLevelHigh = _rsiLevelHigh,
            RsiLevelHighAge = _rsiLevelHigh == null ? 0 : (int)Math.Min(int.MaxValue, newest - _rsiLevelHighIndex),
            RsiLevelLow = _rsiLevelLow,
            RsiLevelLowAge = _rsiLevelLow == null ? 0 : (int)Math.Min(int.MaxValue, newest - _rsiLevelLowIndex),
        });
    }
}
