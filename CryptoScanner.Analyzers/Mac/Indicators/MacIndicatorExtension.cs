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
    // crosses on. What IS kept is the level as it stood BEFORE this candle, because a level set on
    // the candle in hand would be broken by that same candle - the low of a candle is always under
    // its own close.
    private RsiHub? _rsi;
    private bool _useRsiLevels;
    private double _supportCross;
    private double _resistanceCross;
    private double? _previousRsi;
    private double? _rsiLevelHigh;
    private long _rsiLevelHighIndex;
    private double? _rsiLevelLow;
    private long _rsiLevelLowIndex;
    private double? _publishedHigh;
    private long _publishedHighIndex;
    private double? _publishedLow;
    private long _publishedLowIndex;

    // The run beyond a level, per side. A run is the unbroken stretch of candles whose close
    // stands beyond the level; inside it the reference marks the candles that make a new extreme,
    // and only for runs that started with the price already clear of the trend. Both the running
    // extreme and the verdict on the run are kept here, so the strategy only has to read a number.
    private const int BreakRangeCandles = 14;       // candles in the average candle size
    private const int BreakVolumeCandles = 20;      // candles in the average volume
    private const double BreakVolumeShare = 1.0;    // the candle needs at least the average
    private const double BreakCloudThick = 4.1;     // the four lines at most this many candle sizes apart
    private const double BreakCloudClear = 0.75;    // and the close at least this far past their far edge
    private readonly decimal[] _breakRanges = new decimal[BreakRangeCandles];
    private readonly decimal[] _breakVolumes = new decimal[BreakVolumeCandles];
    private long _breakSeen;
    private bool _runUpOpen;
    private bool _runDownOpen;
    private bool _runUpAllowed;
    private bool _runDownAllowed;
    private double _runUpReach;
    private double _runDownReach;
    private int _runUpRank;
    private int _runDownRank;
    private int _markUpRank;
    private int _markDownRank;

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
            _rsiLevelHigh = _rsiLevelLow = _publishedHigh = _publishedLow = null;
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
        _runUpOpen = _runDownOpen = false;
        _runUpAllowed = _runDownAllowed = false;
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

        // After the levels, never before: what a run is measured against is the level as it stood
        // BEFORE this candle, which is exactly what TrackRsiLevel has just published.
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
    /// </summary>
    private void TrackRsiLevel(IQuote candle)
    {
        if (_rsi == null)
            return;
        var results = _rsi.Results;
        if (results.Count == 0 || results[^1].Rsi == null)
            return;

        // What the strategy is allowed to see on this candle is the level as it stood BEFORE it.
        _publishedHigh = _rsiLevelHigh;
        _publishedHighIndex = _rsiLevelHighIndex;
        _publishedLow = _rsiLevelLow;
        _publishedLowIndex = _rsiLevelLowIndex;

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
    /// The runs beyond a level, one per side, and where inside such a run a break marker belongs.
    /// <para>
    /// Two layers, both settled here. A run EARNS marks when the four lines stand close together at
    /// its first candle and the close is already clear of them - at most <see cref="BreakCloudThick"/>
    /// candle sizes from the highest line to the lowest, and at least <see cref="BreakCloudClear"/>
    /// past the edge - and that verdict is then held for the whole run. Inside a run the marks go on
    /// the candles that make a new extreme of it, counted from one.
    /// </para>
    /// <para>
    /// Both numbers come from measurement against the reference indicator, not from taste: over
    /// four coins on five minute and daily candles this reading agrees with two to three times as
    /// many of its break markers as breaking on the crossing candle does, on both sides and on
    /// both intervals. It is a better approximation, not the rule itself - see Mac.md.
    /// </para>
    /// </summary>
    private void TrackBreakRuns(IQuote candle)
    {
        _breakRanges[(int)(_breakSeen % BreakRangeCandles)] = candle.High - candle.Low;
        _breakVolumes[(int)(_breakSeen % BreakVolumeCandles)] = candle.Volume;
        _breakSeen++;

        double close = (double)candle.Close;
        _markUpRank = TrackOneRun(close, true, ref _runUpOpen, ref _runUpAllowed,
            ref _runUpReach, ref _runUpRank);
        _markDownRank = TrackOneRun(close, false, ref _runDownOpen, ref _runDownAllowed,
            ref _runDownReach, ref _runDownRank);
    }


    /// <summary>One side of <see cref="TrackBreakRuns"/>. Returns this candle's rank, or zero.</summary>
    private int TrackOneRun(double close, bool up, ref bool open, ref bool allowed,
        ref double reach, ref int rank)
    {
        double? level = up ? _publishedHigh : _publishedLow;
        bool beyond = level != null && (up ? close > level.Value : close < level.Value);
        if (!beyond)
        {
            open = false;
            allowed = false;
            return 0;
        }
        if (!open)
        {
            open = true;
            rank = 0;
            reach = up ? double.MinValue : double.MaxValue;
            allowed = RunIsWorthMarking(close, up);
        }
        if (up ? close <= reach : close >= reach)
            return 0;
        reach = close;
        rank++;

        // The candle also has to carry volume. Measured over six coins on fifteen minute candles
        // the stretches the reference marks run a median of 2.4 to 3.1 times the twenty candle
        // average against 1.6 for the ones it leaves alone, while the two distances that pick the
        // stretch barely separate them at all. Asking for the average alone lifts the agreement on
        // both the fifteen minute set it was found on AND the five minute set it was not - 31 to
        // 34 and 15 to 16 - which is why it is a rule and not a curve through the noise.
        //
        // The rank keeps counting: this candle IS the next new extreme of its run whether it is
        // marked or not, so a quiet one does not hand its number to the candle after it.
        if (!EnoughVolume())
            return 0;

        // And the SECOND line has to stand on the break's side of the slow line. A break away from
        // the trend is one the reference does not mark, and this is the sharpest way it says so: of
        // every break marker it draws on eleven coins over four timeframes, not one has the second
        // line on the wrong side - 203 of 203 on fifteen minutes, 140 of 140 on five, and all of
        // them on four hours and daily. Of the candles it does NOT mark, a fifth do.
        //
        // What it buys, against the rule without it: 62 false marks fewer for one marker given up,
        // and it pulls the same way on all four sets - 195 false becomes 172, 189 becomes 177, 105
        // becomes 83 and 49 becomes 44.
        if (!SecondLineWithTheBreak(up))
            return 0;
        return rank;
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


    /// <summary>Whether the newest candle carries at least the average volume of the last twenty.</summary>
    private bool EnoughVolume()
    {
        if (_breakSeen < BreakVolumeCandles)
            return false;
        decimal total = 0m;
        foreach (decimal volume in _breakVolumes)
            total += volume;
        if (total <= 0m)
            return true;                    // no volume anywhere: do not let it block the mark
        decimal average = total / BreakVolumeCandles;
        decimal newest = _breakVolumes[(int)((_breakSeen - 1) % BreakVolumeCandles)];
        return (double)newest >= BreakVolumeShare * (double)average;
    }


    /// <summary>
    /// Whether a run that starts on this candle deserves marks at all: were the four lines close
    /// together, and does the close already stand clear of them?
    /// <para>
    /// This is the part of the break that decides WHICH stretches get marked, and it has been
    /// searched hard. Of 990 stretches in view over eleven coins and four timeframes the reference
    /// marks 273, so three quarters have to be turned away. Three candidates were measured over
    /// all of them, each time fitted on the fifteen minute set and judged on the four sets that
    /// took no part in the fitting - and then the other way round as a check:
    /// </para>
    /// <list type="bullet">
    /// <item>the distance from the LEVEL to the slow line, which this replaces: 0.535 and 0.413;</item>
    /// <item>the thickness of the cloud with the close clear of it: 0.548 and 0.437;</item>
    /// <item>every number we can measure, weighted by a fitted model: 0.511 and 0.395.</item>
    /// </list>
    /// <para>
    /// Those are harmonic means of hit rate and false rate. The middle line is what stands here. It
    /// is worth noticing that the last one is BELOW the first: a model with eighteen numbers and a
    /// free hand does no better than the rule, which says the reference's own rule is not hiding in
    /// anything we measure. See Mac.md.
    /// </para>
    /// <para>
    /// Both searches landed on the same pair - 4.0 and 4.25 for the thickness, 0.75 for the clear -
    /// and the middle of the two is what is used, so neither set got its own optimum. Against the
    /// distance band it replaces, over all five sets: 297 markers on the exact candle becomes 307,
    /// the missed ones 212 become 202, and the false ones 480 become 462.
    /// </para>
    /// </summary>
    private bool RunIsWorthMarking(double close, bool up)
    {
        if (_breakSeen < BreakRangeCandles)
            return false;
        decimal total = 0m;
        foreach (decimal range in _breakRanges)
            total += range;
        double span = (double)total / BreakRangeCandles;
        if (span <= 0.0)
            return false;

        double? fast = _emaFast?.Results.Count > 0 ? _emaFast.Results[^1].Ema : null;
        double? second = _emaSecond?.Results.Count > 0 ? _emaSecond.Results[^1].Ema : null;
        double? medium = _smaMedium?.Results.Count > 0 ? _smaMedium.Results[^1].Sma : null;
        double? slow = _smaSlow?.Results.Count > 0 ? _smaSlow.Results[^1].Sma : null;
        if (fast == null || second == null || medium == null || slow == null)
            return false;

        double top = Math.Max(Math.Max(fast.Value, second.Value), Math.Max(medium.Value, slow.Value));
        double bottom = Math.Min(Math.Min(fast.Value, second.Value), Math.Min(medium.Value, slow.Value));
        if ((top - bottom) / span >= BreakCloudThick)
            return false;

        double edge = up ? top : bottom;
        double sign = up ? 1.0 : -1.0;
        return sign * (close - edge) / span > BreakCloudClear;
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
            && _publishedHigh == null && _publishedLow == null)
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
            RsiLevelHigh = _publishedHigh,
            RsiLevelHighAge = _publishedHigh == null ? 0 : (int)Math.Min(int.MaxValue, newest - _publishedHighIndex),
            RsiLevelLow = _publishedLow,
            RsiLevelLowAge = _publishedLow == null ? 0 : (int)Math.Min(int.MaxValue, newest - _publishedLowIndex),
            BreakoutRank = _markUpRank,
            BreakoutRunAllowed = _runUpAllowed,
            BreakdownRank = _markDownRank,
            BreakdownRunAllowed = _runDownAllowed,
        });
    }
}
