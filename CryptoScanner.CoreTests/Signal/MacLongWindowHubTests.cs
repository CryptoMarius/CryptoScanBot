using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The MA Cloud on Custom can ask for an SMA longer than the hub's standard cache of 200. Emulator
/// run 1496 (Speed Custom, slow SMA 300) threw "Insufficient cache size for SMA(300)" when the hub
/// was built. The cache now follows IStrategyPlugin.RequiredHistory.
/// </summary>
[DoNotParallelize]
[TestClass]
public class MacLongWindowHubTests : TestBase
{
    [TestMethod]
    public void AHubWithAnSma300IsBuiltWithoutError()
    {
        InitTestSession();
        MacSettings before = MacPlugin.Settings;
        try
        {
            RegisterAndEnablePlugin(new MacPlugin());
            new MacPlugin().SettingsBase = new MacSettings { Speed = MacSpeed.Custom, SlowSmaLength = 300 };

            Assert.AreEqual(310, IntervalIndicatorHub.PluginRequiredHistory(), "slow SMA 300 plus the slope lookback of 10");
            IntervalIndicatorHub hub = new();
            Assert.IsNotNull(hub);
        }
        finally
        {
            new MacPlugin().SettingsBase = before;
        }
    }

    [TestMethod]
    public void TheStandardSpeedStaysInsideTheStandardCache()
    {
        InitTestSession();
        MacSettings before = MacPlugin.Settings;
        try
        {
            RegisterAndEnablePlugin(new MacPlugin());
            new MacPlugin().SettingsBase = new MacSettings();
            Assert.IsTrue(IntervalIndicatorHub.PluginRequiredHistory() <= 200);
        }
        finally
        {
            new MacPlugin().SettingsBase = before;
        }
    }
}
