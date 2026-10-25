using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// A zone interval list that no enabled strategy reads is dropped in the emulator (open point 32),
/// but the live scanner calculates it anyway, so the zones are ready before the chart is opened.
/// </summary>
[DoNotParallelize]
[TestClass]
public class SignalPrepareZonesWithoutReaderTests : TestBase
{
    private bool _emulatorMode;
    private List<string> _dlzIntervals = [];
    private List<string> _fvgIntervals = [];
    private List<string> _longStrategies = [];
    private List<string> _shortStrategies = [];

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        _emulatorMode = GlobalData.IsEmulatorMode;
        _dlzIntervals = [.. GlobalData.Settings.Signal.ZonesDlz.IntervalList];
        _fvgIntervals = [.. GlobalData.Settings.Signal.ZonesFvg.IntervalList];
        _longStrategies = [.. GlobalData.Settings.Signal.Long.Strategy];
        _shortStrategies = [.. GlobalData.Settings.Signal.Short.Strategy];

        // Nothing enabled, so nothing reads the zones.
        GlobalData.Settings.Signal.Long.Strategy = [];
        GlobalData.Settings.Signal.Short.Strategy = [];
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = ["1h"];
        GlobalData.Settings.Signal.ZonesFvg.IntervalList = ["15m"];
    }

    [TestCleanup]
    public void Restore()
    {
        GlobalData.IsEmulatorMode = _emulatorMode;
        GlobalData.Settings.Signal.ZonesDlz.IntervalList = _dlzIntervals;
        GlobalData.Settings.Signal.ZonesFvg.IntervalList = _fvgIntervals;
        GlobalData.Settings.Signal.Long.Strategy = _longStrategies;
        GlobalData.Settings.Signal.Short.Strategy = _shortStrategies;
        SignalPrepare.Prepare();
    }


    [TestMethod]
    public void Emulator_DropsZonesNobodyReads()
    {
        GlobalData.IsEmulatorMode = true;

        SignalPrepare.Prepare();

        Assert.AreEqual(0, SignalPrepare.EffectiveDlzIntervals.Count());
        Assert.IsFalse(SignalPrepare.ZoneFvgActive());
    }

    [TestMethod]
    public void LiveScanner_CalculatesZonesNobodyReads()
    {
        GlobalData.IsEmulatorMode = false;

        SignalPrepare.Prepare();

        CollectionAssert.AreEqual(new List<string> { "1h" }, SignalPrepare.EffectiveDlzIntervals.ToList());
        Assert.IsTrue(SignalPrepare.ZoneFvgActive());
    }
}
