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

    // The RSI levels, when they are switched on. No ring buffer this time: a crossing only needs
    // the RSI of the previous candle, and the level is the low or the high of the candle it
    // crosses on. The level is LIVE on that candle - see TrackRsiLevel for what the reference's own
    // plot values say about that, and for why the guard that used to hold it back a candle was
    // guarding against something that cannot happen.
    private RsiHub? _rsi;
    private bool _useRsiLevels;
    private double _supportCross;
    private double _resistanceCross;
    private double? _previousRsi;
    private double? _rsiLevelHigh;
    private long _rsiLevelHighIndex;
    private double? _rsiLevelLow;
    private long _rsiLevelLowIndex;

    // The run beyond a level, per side. A run is the unbroken stretch of candles whose close
    // stands beyond the level AND whose wick betters the last hundred candles. The counter that
    // decides how many of them are marked does not belong to the stretch but to the POSITION: it
    // restarts at the entry - the fast line crossing the second - and lets three through. See
    // TrackBreakRuns for the measurement behind every one of those words.
    private const int BreakRangeCandles = 14;       // candles in the average candle size
    private const int BreakExtremeCandles = 100;    // the wick has to better this many candles
    private const int BreakBarsSinceOpen = 5;       // and the entry has to be this many candles old
    private readonly decimal[] _breakRanges = new decimal[BreakRangeCandles];
    private readonly decimal[] _breakHighs = new decimal[BreakExtremeCandles];
    private readonly decimal[] _breakLows = new decimal[BreakExtremeCandles];
    private long _breakSeen;
    private long _sinceOpenUp = long.MaxValue;
    private long _sinceOpenDown = long.MaxValue;
    private int _runUpRank;
    private int _runDownRank;
    private int _markUpRank;
    private int _markDownRank;
    private bool _fastWasAbove;
    private bool _fastSideKnown;

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
        var lengths = settings.Lines();
        int fast = Math.Max(1, lengths.Fast);
        int second = Math.Max(fast + 1, lengths.Second);
        int medium = Math.Max(2, lengths.Medium);
        int slow = Math.Max(medium + 1, lengths.Slow);

        _emaFast = registry.Ema(fast);
        _emaSecond = registry.Ema(second);
        _smaMedium = registry.Sma(medium);
        _smaSlow = registry.Sma(slow);
        _slopeLookback = Math.Max(1, settings.SlowLineLookbackCandles);

        _useRsiLevels = settings.UseRsiLevels;
        if (_useRsiLevels)
        {
            _rsi = registry.Rsi(Math.Max(2, settings.RsiLevelLength));
            _supportCross = (double)settings.RsiLevelSupportCross;
            _resistanceCross = (double)settings.RsiLevelResistanceCross;
            _previousRsi = null;
            _rsiLevelHigh = _rsiLevelLow = null;
        }

        _left = Math.Max(1, settings.PivotLeftCandles);
        _right = Math.Max(1, settings.PivotRightCandles);
        int window = _left + _right + 1;
        _highs = new decimal[window];
        _lows = new decimal[window];
        _filled = 0;
        _next = 0;
        _candleCount = 0;
        _breakSeen = 0;
        _sinceOpenUp = _sinceOpenDown = long.MaxValue;
        _fastSideKnown = false;
        _markUpRank = _markDownRank = 0;
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

        // After the levels, never before: a run is measured against the level as it stands on THIS
        // candle, which is what TrackRsiLevel has just worked out.
        TrackBreakRuns(candle);

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
    /// The RSI levels. A support is the LOW of the candle on which the RSI crosses back up through
    /// its lower bound, a resistance the HIGH of the candle on which it crosses back down through
    /// the upper one. One of each is carried forward until the next crossing replaces it.
    /// <para>
    /// The level is live on the candle that SETS it, and that is not a guess: the reference draws
    /// its Support and Resistance as plots, and a plot hands over its numbers. Held against ours
    /// over 71 changes of both levels on bitcoin at fifteen minutes, a level that waits a candle
    /// matches on 55 and 59 of the 71 and a level that does not wait matches on 71 and 71 - every
    /// difference sitting on the very candle the level moved.
    /// </para>
    /// <para>
    /// Until 24 September 2026 the level was held back one candle, so that a candle could not break
    /// a level it had just set. That cannot happen anyway: a resistance IS the high of its candle
    /// and a close never exceeds its own high, so the guard only ever cost a candle. For the break
    /// markers it measures as a draw - 305 on the exact candle either way - and it is out because
    /// the reference's own numbers say what the right answer is.
    /// </para>
    /// </summary>
    private void TrackRsiLevel(IQuote candle)
    {
        if (_rsi == null)
            return;
        var results = _rsi.Results;
        if (results.Count == 0 || results[^1].Rsi == null)
            return;

        double now = results[^1].Rsi!.Value;
        if (_previousRsi != null)
        {
            if (_previousRsi.Value < _supportCross && now >= _supportCross)
            {
                _rsiLevelLow = (double)candle.Low;
                _rsiLevelLowIndex = _candleCount - 1;
            }
            if (_previousRsi.Value > _resistanceCross && now <= _resistanceCross)
            {
                _rsiLevelHigh = (double)candle.High;
                _rsiLevelHighIndex = _candleCount - 1;
            }
        }
        _previousRsi = now;
    }


    /// <summary>
    /// The break markers, per side. A candle is a candidate when its CLOSE stands beyond the level
    /// and its WICK betters the highest high (lowest low) of the hundred candles before it; of the
    /// candidates, the first three since the position opened are marked.
    /// <para>
    /// Every part of that is measured against the reference's own plot values on eleven coins over
    /// five timeframes, 747 markers outside the warm-up.
    /// </para>
    /// <list type="bullet">
    /// <item><b>The wick, not the close.</b> All 747 better the hundred candles before them on the
    /// wick. On the close only 85% do, and that is what held the whole reconstruction back.</item>
    /// <item><b>Exactly a hundred.</b> 747 of 747 at 100 and 743 at 101 - an edge that sharp is a
    /// literal number in the source, not a fitted one. It is not one of the line lengths, and it
    /// does not move when the indicator's Speed input does.</item>
    /// <item><b>The counter belongs to the position.</b> Anchored on the stretch it reaches 93% of
    /// the markers at 43% precision; anchored on the entry, 89% at 81%. Three per position: two
    /// gives 64%, four gives 96% at 75%.</item>
    /// <item><b>And the entry has to be five candles old.</b> The reference skips candidates right
    /// after the cross. 89% at 81% becomes 91% at 87%.</item>
    /// <item><b>Volume is NOT a condition.</b> It looked like the strongest number of all until the
    /// hundred candle extreme was in - it was standing in for it. Asking for the average costs six
    /// points of agreement, asking a third more costs fourteen.</item>
    /// </list>
    /// <para>
    /// Together: 683 of 747 on the exact candle with 98 false, against 487 with 608 false for the
    /// stretch reading it replaces. Held apart, the fifteen minute set gives 91% and 87% and the
    /// four sets that took no part in any choice give 92% and 88% - the out of sample half is the
    /// better one, so nothing here is fitted to noise. See Mac.md.
    /// </para>
    /// </summary>
    private void TrackBreakRuns(IQuote candle)
    {
        _breakRanges[(int)(_breakSeen % BreakRangeCandles)] = candle.High - candle.Low;

        // The entry the counter hangs on, and the hundred candle window, are both read BEFORE this
        // candle goes into the buffers - so "the hundred candles before this one" really is before.
        TrackTheEntry();

        _markUpRank = TrackOneSide(candle, true, ref _sinceOpenUp, ref _runUpRank);
        _markDownRank = TrackOneSide(candle, false, ref _sinceOpenDown, ref _runDownRank);

        _breakHighs[(int)(_breakSeen % BreakExtremeCandles)] = candle.High;
        _breakLows[(int)(_breakSeen % BreakExtremeCandles)] = candle.Low;
        _breakSeen++;
    }


    /// <summary>The fast line crossing the second one, which is what restarts the counter.</summary>
    private void TrackTheEntry()
    {
        double? fast = _emaFast?.Results.Count > 0 ? _emaFast.Results[^1].Ema : null;
        double? second = _emaSecond?.Results.Count > 0 ? _emaSecond.Results[^1].Ema : null;
        if (fast == null || second == null)
            return;

        if (_sinceOpenUp != long.MaxValue)
            _sinceOpenUp++;
        if (_sinceOpenDown != long.MaxValue)
            _sinceOpenDown++;

        bool above = fast.Value > second.Value;
        if (_fastSideKnown && above != _fastWasAbove)
        {
            if (above)
            {
                _sinceOpenUp = 0;
                _runUpRank = 0;
            }
            else
            {
                _sinceOpenDown = 0;
                _runDownRank = 0;
            }
        }
        _fastWasAbove = above;
        _fastSideKnown = true;
    }


    /// <summary>One side of <see cref="TrackBreakRuns"/>. Returns this candle's number, or zero.</summary>
    private int TrackOneSide(IQuote candle, bool up, ref long sinceOpen, ref int rank)
    {
        double? level = up ? _rsiLevelHigh : _rsiLevelLow;
        double close = (double)candle.Close;
        if (level == null || (up ? close <= level.Value : close >= level.Value))
            return 0;

        if (!WickBettersTheHundred(candle, up))
            return 0;

        // The cloud still has to point the way of the break. Both of these hold on every one of the
        // 747 markers; the fast line against the second is nearly implied by the counter's anchor
        // and is kept because it says out loud which side of that cross we are on.
        if (!SecondLineWithTheBreak(up))
            return 0;
        if (!FastLineWithTheBreak(up))
            return 0;

        // Too soon after the entry the reference draws nothing.
        if (sinceOpen < BreakBarsSinceOpen)
            return 0;

        rank++;
        return rank;
    }


    /// <summary>
    /// Whether this candle's wick betters every one of the <see cref="BreakExtremeCandles"/> before
    /// it. The candle itself is not in the buffers yet, so the whole buffer IS its history.
    /// </summary>
    private bool WickBettersTheHundred(IQuote candle, bool up)
    {
        if (_breakSeen < BreakExtremeCandles)
            return false;
        decimal[] buffer = up ? _breakHighs : _breakLows;
        decimal best = buffer[0];
        for (int i = 1; i < buffer.Length; i++)
        {
            if (up ? buffer[i] > best : buffer[i] < best)
                best = buffer[i];
        }
        return up ? candle.High > best : candle.Low < best;
    }


    /// <summary>Whether the fast line stands on the side of the second one the break is going.</summary>
    private bool FastLineWithTheBreak(bool up)
    {
        double? fast = _emaFast?.Results.Count > 0 ? _emaFast.Results[^1].Ema : null;
        double? second = _emaSecond?.Results.Count > 0 ? _emaSecond.Results[^1].Ema : null;
        if (fast == null || second == null)
            return false;
        return up ? fast.Value > second.Value : fast.Value < second.Value;
    }


    /// <summary>
    /// Whether the second line stands on the side of the slow line the break is going.
    /// <para>
    /// The fast line against the slow one says the same thing and says it more weakly: it is free -
    /// no marker given up at all - but it removes 26 false marks where this removes 62. Both were
    /// measured; this one is in.
    /// </para>
    /// </summary>
    private bool SecondLineWithTheBreak(bool up)
    {
        double? second = _emaSecond?.Results.Count > 0 ? _emaSecond.Results[^1].Ema : null;
        double? slow = _smaSlow?.Results.Count > 0 ? _smaSlow.Results[^1].Sma : null;
        if (second == null || slow == null)
            return false;
        return up ? second.Value > slow.Value : second.Value < slow.Value;
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
            BreakoutRank = _markUpRank,
            BreakoutRunAllowed = _sinceOpenUp >= BreakBarsSinceOpen,
            BreakdownRank = _markDownRank,
            BreakdownRunAllowed = _sinceOpenDown >= BreakBarsSinceOpen,
        });
    }
}
