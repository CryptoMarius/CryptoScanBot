using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.ImpulseBox;

/// <summary>
/// The kind of event a box produces on a candle.
/// </summary>
public enum ImpulseBoxEventKind
{
    /// <summary>The first close outside the box goes AGAINST the impulse.</summary>
    Counter,

    /// <summary>The first close outside the box went WITH the impulse, and a close came back inside
    /// within the fake break window. The trade goes against the impulse.</summary>
    FakeBreak,

    /// <summary>The first close outside the box goes WITH the impulse.</summary>
    Continuation,
}


/// <summary>
/// One event on one candle: which kind, which side it trades, and the box it came from.
/// </summary>
public sealed class ImpulseBoxEvent
{
    public ImpulseBoxEventKind Kind { get; init; }

    /// <summary>True when the event is a long entry, false for a short.</summary>
    public bool IsLong { get; init; }

    /// <summary>+1 for an impulse up (box above it), -1 for an impulse down.</summary>
    public int Direction { get; init; }

    public double BoxHigh { get; init; }
    public double BoxLow { get; init; }

    /// <summary>The body low (impulse up) or body high (impulse down) the impulse started from.</summary>
    public double ImpulseStart { get; init; }
}


/// <summary>
/// A box as the chart draws it: from its first candle to the candle it broke or expired on.
/// </summary>
public sealed class ImpulseBoxSpan
{
    public int Direction { get; init; }
    public int FirstIndex { get; init; }
    public int ValidIndex { get; init; }
    public int LastIndex { get; set; }
    public double High { get; init; }
    public double Low { get; init; }
    public double ImpulseStart { get; init; }
}


/// <summary>
/// Finds an impulse followed by a balance box, candle by candle, for both directions at once.
/// One class for the indicator hub (<see cref="Indicators.ImpulseBoxIndicatorExtension"/>) and the
/// chart overlay (<see cref="Compute"/>), so both see exactly the same boxes.
/// <para>
/// Per direction (described for an impulse up; down is the exact mirror):
/// <list type="number">
/// <item>Impulse: the candle closes at the highest close of the last <c>ImpulseMaxCandles</c>
/// candles. The impulse starts at the lowest body low (min(open, close)) of that window, the
/// earliest one on a tie. Its height (close - start) is at least <c>ImpulseAtrMultiplier</c> x
/// ATR(14), and the closes from the start onwards never fall back more than
/// <c>ImpulseMaxPullbackPercentage</c> of the height below their running high.</item>
/// <item>Box: the candles after the impulse, built from bodies only (open and close); wicks do not
/// count. A close above the impulse high measures the impulse again ending there and restarts the
/// box; when that is no valid impulse the pattern is dropped. A box taller than
/// <c>BoxMaxHeightPercentage</c> of the impulse height is dropped. After <c>BoxCandles</c> box candles
/// the box is valid and frozen.</item>
/// <item>Break: the first close outside the frozen box, within <c>BoxExpiryCandles</c> candles after it
/// became valid. Against the impulse is a <see cref="ImpulseBoxEventKind.Counter"/>, with the impulse a
/// <see cref="ImpulseBoxEventKind.Continuation"/>; after a continuation a close back inside the box
/// within <c>FakeBreakWindowCandles</c> candles is a <see cref="ImpulseBoxEventKind.FakeBreak"/>.</item>
/// </list>
/// After a break the search starts again on the next candle; after an expiry the candle after the
/// last one checked is skipped as well. The same rules were measured outside the scanner first, see
/// ImpulseBox.md.
/// </para>
/// </summary>
public sealed class ImpulseBoxDetector
{
    public const int AtrLength = 14;

    private enum Phase { Search, Forming, Valid }

    private sealed class DirectionState
    {
        public int Direction;
        public Phase Phase = Phase.Search;
        public double Start, Top, Height;
        public double BoxHigh, BoxLow;
        public bool HasBox;
        public int Count;
        public int FirstIndex;
        public int ValidIndex;
        public ImpulseBoxSpan? Span;

        // A continuation that can still turn into a fake break
        public bool FakePending;
        public int FakeUntil;
        public double FakeBoxHigh, FakeBoxLow, FakeStart;
    }

    private readonly double _atrMultiplier;
    private readonly int _impulseMax;
    private readonly double _pullback;
    private readonly int _boxCandles;
    private readonly double _boxMaxHeight;
    private readonly int _boxExpiry;
    private readonly int _fakeWindow;

    private readonly double[] _open;
    private readonly double[] _close;
    private readonly double[] _atr;
    private readonly int _capacity;

    private int _index = -1;
    private double _previousClose;
    private double _atrValue;
    private double _trSum;

    private readonly DirectionState _up = new() { Direction = 1 };
    private readonly DirectionState _down = new() { Direction = -1 };

    /// <summary>Boxes that became valid, kept only when the chart asks for them.</summary>
    private readonly List<ImpulseBoxSpan>? _spans;

    public ImpulseBoxDetector(ImpulseBoxSettings settings, bool collectSpans = false)
        : this(settings.ImpulseAtrMultiplier, settings.ImpulseMaxCandles, settings.ImpulseMaxPullbackPercentage,
            settings.BoxCandles, settings.BoxMaxHeightPercentage, settings.BoxExpiryCandles,
            settings.FakeBreakWindowCandles, collectSpans)
    {
    }

    public ImpulseBoxDetector(double atrMultiplier, int impulseMaxCandles, double pullbackPercentage,
        int boxCandles, double boxMaxHeightPercentage, int boxExpiryCandles, int fakeBreakWindowCandles,
        bool collectSpans = false)
    {
        _atrMultiplier = atrMultiplier;
        _impulseMax = Math.Max(1, impulseMaxCandles);
        _pullback = pullbackPercentage / 100.0;
        _boxCandles = Math.Max(1, boxCandles);
        _boxMaxHeight = boxMaxHeightPercentage / 100.0;
        _boxExpiry = Math.Max(1, boxExpiryCandles);
        _fakeWindow = Math.Max(0, fakeBreakWindowCandles);
        _capacity = _impulseMax + 1;
        _open = new double[_capacity];
        _close = new double[_capacity];
        _atr = new double[_capacity];
        _spans = collectSpans ? [] : null;
    }

    /// <summary>True once the ATR has its first value.</summary>
    public bool IsReady => _index >= AtrLength - 1;

    /// <summary>A short description of where each direction is, for the signal text.</summary>
    public string Status => $"up: {Describe(_up)}, down: {Describe(_down)}";

    public IReadOnlyList<ImpulseBoxSpan> Spans => _spans ?? (IReadOnlyList<ImpulseBoxSpan>)[];

    private string Describe(DirectionState s) => s.Phase switch
    {
        Phase.Forming => $"box {s.Count}/{_boxCandles}",
        Phase.Valid => "box ready",
        _ => "no impulse",
    };


    /// <summary>
    /// Feeds one candle (in ascending time order) and returns the events it produced, or null.
    /// </summary>
    public List<ImpulseBoxEvent>? Add(double open, double high, double low, double close)
    {
        _index++;
        int slot = _index % _capacity;
        _open[slot] = open;
        _close[slot] = close;

        // ATR(14), Wilder: the first value is the mean of the first 14 true ranges
        double tr = _index == 0
            ? high - low
            : Math.Max(high - low, Math.Max(Math.Abs(high - _previousClose), Math.Abs(low - _previousClose)));
        _previousClose = close;
        if (_index < AtrLength)
        {
            _trSum += tr;
            _atrValue = _index == AtrLength - 1 ? _trSum / AtrLength : double.NaN;
        }
        else
            _atrValue = (_atrValue * (AtrLength - 1) + tr) / AtrLength;
        _atr[slot] = _atrValue;

        List<ImpulseBoxEvent>? events = null;
        Step(_up, open, close, ref events);
        Step(_down, open, close, ref events);
        return events;
    }


    private void Step(DirectionState s, double open, double close, ref List<ImpulseBoxEvent>? events)
    {
        int d = s.Direction;

        // A pending fake break is checked on every candle, apart from the search
        if (s.FakePending)
        {
            if (_index > s.FakeUntil)
                s.FakePending = false;
            else
            {
                bool backInside = d == 1 ? close < s.FakeBoxHigh : close > s.FakeBoxLow;
                if (backInside)
                {
                    s.FakePending = false;
                    (events ??= []).Add(new ImpulseBoxEvent
                    {
                        Kind = ImpulseBoxEventKind.FakeBreak, IsLong = d == -1, Direction = d,
                        BoxHigh = s.FakeBoxHigh, BoxLow = s.FakeBoxLow, ImpulseStart = s.FakeStart,
                    });
                }
            }
        }

        switch (s.Phase)
        {
            case Phase.Search:
                Search(s);
                return;

            case Phase.Forming:
            {
                bool beyond = d == 1 ? close > s.Top : close < s.Top;
                if (beyond)
                {
                    // The impulse goes on: measure it again ending here, so it stays a compact move
                    if (!TryImpulse(d, out double start, out double top))
                    {
                        s.Phase = Phase.Search;
                        return;
                    }
                    s.Start = start;
                    s.Top = top;
                    s.Height = Math.Abs(top - start);
                    s.HasBox = false;
                    s.Count = 0;
                    return;
                }

                double bodyHigh = Math.Max(open, close);
                double bodyLow = Math.Min(open, close);
                if (!s.HasBox)
                {
                    s.BoxHigh = bodyHigh;
                    s.BoxLow = bodyLow;
                    s.HasBox = true;
                    s.FirstIndex = _index;
                }
                else
                {
                    s.BoxHigh = Math.Max(s.BoxHigh, bodyHigh);
                    s.BoxLow = Math.Min(s.BoxLow, bodyLow);
                }

                if (s.BoxHigh - s.BoxLow > _boxMaxHeight * s.Height)
                {
                    // No balance; this candle may start a new impulse itself
                    s.Phase = Phase.Search;
                    Search(s);
                    return;
                }

                s.Count++;
                if (s.Count >= _boxCandles)
                {
                    s.Phase = Phase.Valid;
                    s.ValidIndex = _index;
                    if (_spans != null)
                    {
                        s.Span = new ImpulseBoxSpan
                        {
                            Direction = d, FirstIndex = s.FirstIndex, ValidIndex = _index, LastIndex = _index,
                            High = s.BoxHigh, Low = s.BoxLow, ImpulseStart = s.Start,
                        };
                        _spans.Add(s.Span);
                    }
                }
                return;
            }

            case Phase.Valid:
            {
                if (_index > s.ValidIndex + _boxExpiry)
                {
                    // Expired without a break; this candle is not looked at for a new impulse
                    s.Phase = Phase.Search;
                    if (s.Span != null)
                        s.Span.LastIndex = _index - 1;
                    s.Span = null;
                    return;
                }
                if (s.Span != null)
                    s.Span.LastIndex = _index;

                int breakDirection = close > s.BoxHigh ? 1 : close < s.BoxLow ? -1 : 0;
                if (breakDirection == 0)
                    return;

                if (breakDirection == d)
                {
                    (events ??= []).Add(new ImpulseBoxEvent
                    {
                        Kind = ImpulseBoxEventKind.Continuation, IsLong = d == 1, Direction = d,
                        BoxHigh = s.BoxHigh, BoxLow = s.BoxLow, ImpulseStart = s.Start,
                    });
                    s.FakePending = _fakeWindow > 0;
                    s.FakeUntil = _index + _fakeWindow;
                    s.FakeBoxHigh = s.BoxHigh;
                    s.FakeBoxLow = s.BoxLow;
                    s.FakeStart = s.Start;
                }
                else
                {
                    (events ??= []).Add(new ImpulseBoxEvent
                    {
                        Kind = ImpulseBoxEventKind.Counter, IsLong = d == -1, Direction = d,
                        BoxHigh = s.BoxHigh, BoxLow = s.BoxLow, ImpulseStart = s.Start,
                    });
                }

                // The search starts again on the next candle
                s.Phase = Phase.Search;
                s.Span = null;
                return;
            }
        }
    }


    private void Search(DirectionState s)
    {
        if (!TryImpulse(s.Direction, out double start, out double top))
            return;
        s.Phase = Phase.Forming;
        s.Start = start;
        s.Top = top;
        s.Height = Math.Abs(top - start);
        s.HasBox = false;
        s.Count = 0;
    }


    /// <summary>
    /// Is the current candle the end of a valid impulse in direction <paramref name="d"/>?
    /// </summary>
    private bool TryImpulse(int d, out double start, out double top)
    {
        start = 0;
        top = 0;
        if (_index < _impulseMax || !IsReady)
            return false;
        double atr = _atr[_index % _capacity];
        if (double.IsNaN(atr) || atr <= 0)
            return false;

        double close = _close[_index % _capacity];
        int startIndex = -1;
        double startValue = 0;
        for (int x = _index - _impulseMax; x < _index; x++)
        {
            int slot = x % _capacity;
            double c = _close[slot];
            if (d == 1 ? c >= close : c <= close)
                return false;
            double body = d == 1 ? Math.Min(_open[slot], c) : Math.Max(_open[slot], c);
            if (startIndex < 0 || (d == 1 ? body < startValue : body > startValue))
            {
                startIndex = x;
                startValue = body;
            }
        }

        double height = d == 1 ? close - startValue : startValue - close;
        if (height < _atrMultiplier * atr)
            return false;

        // No big pullbacks: the closes from the start to now stay near their running best
        double best = _close[startIndex % _capacity];
        for (int x = startIndex; x <= _index; x++)
        {
            double c = _close[x % _capacity];
            if (d == 1)
            {
                best = Math.Max(best, c);
                if (best - c > _pullback * height)
                    return false;
            }
            else
            {
                best = Math.Min(best, c);
                if (c - best > _pullback * height)
                    return false;
            }
        }

        start = startValue;
        top = close;
        return true;
    }


    /// <summary>
    /// The whole list at once, for the chart: the events per candle index and the boxes.
    /// </summary>
    public static (List<ImpulseBoxEvent>?[] Events, IReadOnlyList<ImpulseBoxSpan> Spans) Compute(
        IReadOnlyList<CryptoCandle> candles, ImpulseBoxSettings settings)
    {
        var detector = new ImpulseBoxDetector(settings, collectSpans: true);
        var events = new List<ImpulseBoxEvent>?[candles.Count];
        for (int i = 0; i < candles.Count; i++)
        {
            CryptoCandle c = candles[i];
            events[i] = detector.Add((double)c.Open, (double)c.High, (double)c.Low, (double)c.Close);
        }
        return (events, detector.Spans);
    }
}
