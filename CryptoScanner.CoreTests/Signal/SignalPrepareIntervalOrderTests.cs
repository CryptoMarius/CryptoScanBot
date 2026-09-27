using CryptoScanner.Analyzers.Dlz;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The buckets SignalPrepare walks are keyed on the interval name but have to be ordered by
/// duration, shortest first. Ordered by name the walk was 15m, 1d, 1h, 1m, 2h, 30m, 4h, 5m, and the
/// Lux value of the 5m candle that closed on the same tick was never there yet when 15m, 30m and 1h
/// asked for it - so every one of them fell back to the full recalculation (open point 89).
/// <para>
/// The DLZ bucket is the one that can be read back from outside; it is filled through the same
/// <c>Add</c> as the indicator bucket, so it proves the order for all of them.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class SignalPrepareIntervalOrderTests : TestBase
{
    private List<string> _dlzIntervals = [];
    private List<string> _longStrategies = [];

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        _dlzIntervals = [.. GlobalData.Settings.Signal.ZonesDlz.IntervalList];
        _longStrategies = [.. GlobalData.Settings.Signal.Long.Strategy];
        // A configured zone kind that no enabled strategy reads is dropped since open point 32, so
        // the bucket under test needs a registered AND enabled reader.
        RegisterAndEnablePlugin(new DlzPlugin());
    }

    [TestCleanup]
    public void Restore()
    {
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = _dlzIntervals;
        GlobalData.Settings.Signal.Long.Strategy = _longStrategies;
        SignalPrepare.Prepare();
    }


    [TestMethod]
    public void TheIntervalsAreWalkedShortestFirst_NotAlphabetically()
    {
        // Alphabetically this reads 15m, 1h, 1m, 30m, 5m.
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = ["1h", "5m", "15m", "1m", "30m"];

        SignalPrepare.Prepare();

        CollectionAssert.AreEqual(
            new List<string> { "1m", "5m", "15m", "30m", "1h" },
            SignalPrepare.EffectiveDlzIntervals.ToList());
    }
}
