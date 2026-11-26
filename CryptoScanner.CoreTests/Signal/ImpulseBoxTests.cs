using CryptoScanner.Analyzers.ImpulseBox;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// Impulse box: an impulse of at least K x ATR(14), a box of bodies after it, and the first close
/// outside the box. The detector is fed candles worked out by hand, with a short impulse window and
/// a short box so the cases stay readable.
/// <para>
/// Every case starts with 20 flat candles (open = close = 100, high 100.5, low 99.5): true range 1,
/// so the ATR is 1 when the impulse begins. Then four candles up to a close of 105: the impulse
/// starts at the body low 100 and is 5 high, against an ATR of about 1.2 (well under 5 / 3).
/// </para>
/// </summary>
[TestClass]
public class ImpulseBoxTests
{
    private static ImpulseBoxDetector NewDetector(int fakeWindow = 2)
        => new(atrMultiplier: 3.0, impulseMaxCandles: 4, pullbackPercentage: 38.2,
            boxCandles: 3, boxMaxHeightPercentage: 50, boxExpiryCandles: 10, fakeBreakWindowCandles: fakeWindow);

    private static List<ImpulseBoxEvent> Feed(ImpulseBoxDetector detector, params (double o, double h, double l, double c)[] candles)
    {
        var all = new List<ImpulseBoxEvent>();
        foreach (var (o, h, l, c) in candles)
        {
            var events = detector.Add(o, h, l, c);
            if (events != null)
                all.AddRange(events);
        }
        return all;
    }

    /// <summary>20 flat candles and an impulse up from 100 to 105.</summary>
    private static ImpulseBoxDetector WithImpulseUp(int fakeWindow = 2)
    {
        var detector = NewDetector(fakeWindow);
        for (int i = 0; i < 20; i++)
            detector.Add(100, 100.5, 99.5, 100);
        var events = Feed(detector,
            (100, 101.2, 99.9, 101),
            (101, 102.7, 100.9, 102.5),
            (102.5, 104.2, 102.4, 104),
            (104, 105.2, 103.9, 105));
        Assert.AreEqual(0, events.Count, "the impulse itself is no event");
        return detector;
    }

    /// <summary>A box of three candles with bodies between 104.0 and 104.8 (0.8 high, at most 2.5).</summary>
    private static readonly (double, double, double, double)[] Box =
    [
        (104.5, 104.9, 103.8, 104.0),
        (104.0, 104.9, 103.9, 104.6),
        (104.6, 104.9, 104.1, 104.8),
    ];


    [TestMethod]
    public void ACloseBelowTheBoxAfterAnImpulseUpIsACounterShort()
    {
        var detector = WithImpulseUp();
        Assert.AreEqual(0, Feed(detector, Box).Count, "a box that is still forming or just became valid is no event");

        var events = Feed(detector, (104.1, 104.2, 103.4, 103.5));
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(ImpulseBoxEventKind.Counter, events[0].Kind);
        Assert.IsFalse(events[0].IsLong);
        Assert.AreEqual(1, events[0].Direction);
        Assert.AreEqual(104.0, events[0].BoxLow, 1e-9);
        Assert.AreEqual(104.8, events[0].BoxHigh, 1e-9);
        Assert.AreEqual(100.0, events[0].ImpulseStart, 1e-9);
    }


    [TestMethod]
    public void ABreakWithTheImpulseThatClosesBackInsideIsAFakeBreakShort()
    {
        var detector = WithImpulseUp();
        Feed(detector, Box);

        var breakOut = Feed(detector, (104.8, 105.5, 104.7, 105.3));
        Assert.AreEqual(1, breakOut.Count);
        Assert.AreEqual(ImpulseBoxEventKind.Continuation, breakOut[0].Kind);
        Assert.IsTrue(breakOut[0].IsLong);

        var back = Feed(detector, (105.3, 105.4, 104.3, 104.5));
        Assert.AreEqual(1, back.Count);
        Assert.AreEqual(ImpulseBoxEventKind.FakeBreak, back[0].Kind);
        Assert.IsFalse(back[0].IsLong, "the fake break trades against the impulse");
    }


    [TestMethod]
    public void AFakeBreakOnlyCountsInsideTheWindow()
    {
        var detector = WithImpulseUp(fakeWindow: 1);
        Feed(detector, Box);
        Feed(detector, (104.8, 105.5, 104.7, 105.3));   // continuation
        Feed(detector, (105.3, 105.6, 105.2, 105.4));   // still outside, window used up

        var late = Feed(detector, (105.4, 105.4, 104.3, 104.5));
        Assert.IsFalse(late.Any(e => e.Kind == ImpulseBoxEventKind.FakeBreak));
    }


    [TestMethod]
    public void WicksDoNotCountForTheBox()
    {
        var detector = WithImpulseUp();
        // The same bodies, but the first box candle has a wick down to 101
        Feed(detector, (104.5, 104.9, 101.0, 104.0), Box[1], Box[2]);

        // A close of 103.9 is below the body low 104.0, so it breaks the box despite the wick at 101
        var events = Feed(detector, (104.1, 104.2, 103.8, 103.9));
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(ImpulseBoxEventKind.Counter, events[0].Kind);
    }


    [TestMethod]
    public void ABoxTallerThanHalfTheImpulseIsNoBox()
    {
        var detector = WithImpulseUp();
        // Bodies from 102.0 to 104.8: 2.8 high, more than half of the impulse of 5
        var events = Feed(detector,
            (104.5, 104.9, 101.9, 102.0),
            (102.0, 104.9, 101.9, 104.6),
            (104.6, 104.9, 104.1, 104.8),
            (104.1, 104.2, 101.0, 101.5));
        Assert.IsFalse(events.Any(e => e.Direction == 1), "no box, so no event of the impulse up");
    }


    [TestMethod]
    public void ANewHighCloseWhileTheBoxFormsMeasuresTheImpulseAgain()
    {
        var detector = WithImpulseUp();
        // One box candle, then a close of 105.6: the impulse now ends there and the box starts over
        Feed(detector, Box[0], (104.0, 105.8, 103.9, 105.6));
        // Three box candles with bodies between 104.9 and 105.5
        Feed(detector, (105.5, 105.7, 104.8, 105.0), (105.0, 105.6, 104.8, 105.4), (105.4, 105.6, 104.8, 104.9));

        // A close of 104.95 would have broken the old box (high 104.8) upwards, but is inside the new one
        var inside = Feed(detector, (104.9, 105.1, 104.85, 104.95));
        Assert.AreEqual(0, inside.Count);
        var events = Feed(detector, (104.9, 105.0, 104.3, 104.4));
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(ImpulseBoxEventKind.Counter, events[0].Kind);
        Assert.AreEqual(104.9, events[0].BoxLow, 1e-9);
    }


    [TestMethod]
    public void TheDefaultsAreTheReferenceSettingOfTheDryRun()
    {
        var settings = new ImpulseBoxSettings();
        Assert.AreEqual(3.0, settings.ImpulseAtrMultiplier);
        Assert.AreEqual(16, settings.ImpulseMaxCandles);
        Assert.AreEqual(24, settings.BoxCandles);
        Assert.AreEqual(50.0, settings.BoxMaxHeightPercentage);
        Assert.IsTrue(settings.TradeCounter);
        Assert.IsTrue(settings.TradeFakeBreak);
        Assert.IsFalse(settings.TradeContinuation);
        CollectionAssert.AreEqual(new List<string> { "15m" }, settings.IntervalList);
    }
}
