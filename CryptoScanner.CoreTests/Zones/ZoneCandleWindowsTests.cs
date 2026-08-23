using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// What one zone calculation remembers about the candles it pulled in. The point of the class is
/// that it remembers WINDOWS: the flag it replaced said "this interval was read" and made
/// ZoneCandleEngine.FetchFrom pull the complete series for a caller that asked for sixty candles.
///
/// The rule these tests hold in place: a window is skipped only when it really was read before,
/// two windows that do not touch never add up to a claim about the gap between them, and the
/// "already in memory" shortcut still covers everything.
/// </summary>
[TestClass]
public class ZoneCandleWindowsTests
{
    private const CryptoIntervalPeriod OneMinute = CryptoIntervalPeriod.interval1m;
    private const CryptoIntervalPeriod OneHour = CryptoIntervalPeriod.interval1h;

    private static CandleTime T(uint minutes) => new(minutes);


    [TestMethod]
    public void UnknownIntervalIsNeverConsideredRead()
    {
        ZoneCandleWindows windows = new();

        Assert.IsFalse(windows.Contains(OneMinute));
        Assert.IsFalse(windows.IsLoaded(OneMinute, T(100), T(160)));
    }


    [TestMethod]
    public void AWindowThatWasReadIsNotReadAgain()
    {
        ZoneCandleWindows windows = new();
        windows.MarkLoaded(OneMinute, T(100), T(160));

        Assert.IsTrue(windows.IsLoaded(OneMinute, T(100), T(160)), "the window itself");
        Assert.IsTrue(windows.IsLoaded(OneMinute, T(120), T(140)), "a window inside it");
        Assert.IsTrue(windows.Contains(OneMinute));
    }


    [TestMethod]
    public void AWindowOutsideWhatWasReadStillHasToBeRead()
    {
        ZoneCandleWindows windows = new();
        windows.MarkLoaded(OneMinute, T(100), T(160));

        Assert.IsFalse(windows.IsLoaded(OneMinute, T(90), T(160)), "starts earlier");
        Assert.IsFalse(windows.IsLoaded(OneMinute, T(100), T(200)), "ends later");
        Assert.IsFalse(windows.IsLoaded(OneMinute, T(500), T(560)), "somewhere else entirely");
    }


    /// <summary>
    /// The zoom windows of two separate pivots do not touch. Reading both may not turn into a claim
    /// about the stretch between them - those candles were never read, and a zone in that gap would
    /// silently be judged on candles that are not in memory.
    /// </summary>
    [TestMethod]
    public void TwoSeparateWindowsSayNothingAboutTheGapBetweenThem()
    {
        ZoneCandleWindows windows = new();
        windows.MarkLoaded(OneMinute, T(100), T(160));
        windows.MarkLoaded(OneMinute, T(500), T(560));

        Assert.IsTrue(windows.IsLoaded(OneMinute, T(100), T(160)));
        Assert.IsTrue(windows.IsLoaded(OneMinute, T(500), T(560)));
        Assert.IsFalse(windows.IsLoaded(OneMinute, T(300), T(320)), "the gap");
        Assert.IsFalse(windows.IsLoaded(OneMinute, T(160), T(500)), "across the gap");
    }


    [TestMethod]
    public void IntervalsAreKeptApart()
    {
        ZoneCandleWindows windows = new();
        windows.MarkLoaded(OneMinute, T(100), T(160));

        Assert.IsFalse(windows.IsLoaded(OneHour, T(100), T(160)));
        Assert.IsFalse(windows.Contains(OneHour));
    }


    [TestMethod]
    public void FullyInMemoryCoversEveryWindow()
    {
        ZoneCandleWindows windows = new();
        windows.MarkAllLoaded(OneMinute);

        Assert.IsTrue(windows.IsLoaded(OneMinute, T(0), T(60)));
        Assert.IsTrue(windows.IsLoaded(OneMinute, T(5_000_000), T(5_000_060)));
        Assert.IsFalse(windows.HasUnsavedChanges(OneMinute), "in memory does not mean unsaved");
    }


    [TestMethod]
    public void ChangedIsSetAndClearedIndependentlyOfTheWindows()
    {
        ZoneCandleWindows windows = new();
        windows.MarkLoaded(OneMinute, T(100), T(160));
        Assert.IsFalse(windows.HasUnsavedChanges(OneMinute), "reading alone changes nothing");

        windows.MarkChanged(OneMinute);
        Assert.IsTrue(windows.HasUnsavedChanges(OneMinute));

        windows.MarkSaved(OneMinute);
        Assert.IsFalse(windows.HasUnsavedChanges(OneMinute));
        Assert.IsTrue(windows.IsLoaded(OneMinute, T(100), T(160)), "saving forgets no window");
    }


    [TestMethod]
    public void ACopyStartsEqualAndThenGoesItsOwnWay()
    {
        ZoneCandleWindows original = new();
        original.MarkLoaded(OneMinute, T(100), T(160));
        original.MarkChanged(OneMinute);

        ZoneCandleWindows copy = new(original);
        Assert.IsTrue(copy.IsLoaded(OneMinute, T(100), T(160)));
        Assert.IsTrue(copy.HasUnsavedChanges(OneMinute));

        copy.MarkLoaded(OneHour, T(0), T(600));
        copy.MarkSaved(OneMinute);

        Assert.IsFalse(original.IsLoaded(OneHour, T(0), T(600)), "the original learned nothing");
        Assert.IsTrue(original.HasUnsavedChanges(OneMinute), "and forgot nothing");
    }


    [TestMethod]
    public void ClearForgetsEverything()
    {
        ZoneCandleWindows windows = new();
        windows.MarkLoaded(OneMinute, T(100), T(160));
        windows.MarkChanged(OneMinute);

        windows.Clear();

        Assert.IsFalse(windows.Contains(OneMinute));
        Assert.IsFalse(windows.IsLoaded(OneMinute, T(100), T(160)));
        Assert.IsFalse(windows.HasUnsavedChanges(OneMinute));
    }
}
