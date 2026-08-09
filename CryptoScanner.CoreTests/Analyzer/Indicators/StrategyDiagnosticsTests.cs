using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.CoreTests.Analyzer.Indicators;

/// <summary>
/// Verifies the early-warning path: an enabled strategy that can never signal must be reported, and
/// a healthy configuration must stay silent (a diagnostic that cries wolf gets ignored).
/// </summary>
[TestClass]
[DoNotParallelize]
public class StrategyDiagnosticsTests : TestBase
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        InitTestSession();
        RegisterAndEnablePlugin(new VbsPlugin());
    }

    /// <summary>Run Report() and collect everything it wrote to the log tab.</summary>
    private static List<string> Capture()
    {
        List<string> lines = [];
        void Handler(string text) => lines.Add(text);

        GlobalData.LogToLogTabEvent += Handler;
        try
        {
            StrategyDiagnostics.Report();
        }
        finally
        {
            GlobalData.LogToLogTabEvent -= Handler;
        }
        return lines;
    }

    [TestMethod]
    public void EnabledButUnregisteredStrategy_IsReported()
    {
        const string bogus = "strategy-that-does-not-exist";
        GlobalData.Settings.Signal.Long.Strategy.Add(bogus);
        try
        {
            var lines = Capture();
            Assert.IsTrue(lines.Any(l => l.Contains(bogus)),
                $"Expected a warning naming \"{bogus}\", got: {string.Join(" | ", lines)}");
        }
        finally
        {
            GlobalData.Settings.Signal.Long.Strategy.Remove(bogus);
        }
    }

    [TestMethod]
    public void CasingMismatch_IsReported()
    {
        // SignalExecute.Prepare uses List<string>.Contains, so "VBS" does not enable "vbs".
        string wrongCase = VbsPlugin.StrategyInternal.ToUpper();
        GlobalData.Settings.Signal.Long.Strategy.Add(wrongCase);
        try
        {
            var lines = Capture();
            Assert.IsTrue(lines.Any(l => l.Contains(wrongCase)),
                $"Expected a warning naming \"{wrongCase}\", got: {string.Join(" | ", lines)}");
        }
        finally
        {
            GlobalData.Settings.Signal.Long.Strategy.Remove(wrongCase);
        }
    }

    [TestMethod]
    public void HealthyConfiguration_IsSilent()
    {
        // SettingsBasic enables sbm1/sbm2/sbm3/stobb/storsi, and this test process only registers
        // VbsPlugin — which the diagnostic correctly reports. Narrow the enabled list to what is
        // actually registered so this test measures silence, not that default.
        var savedLong = GlobalData.Settings.Signal.Long.Strategy.ToList();
        var savedShort = GlobalData.Settings.Signal.Short.Strategy.ToList();
        try
        {
            GlobalData.Settings.Signal.Long.Strategy.Clear();
            GlobalData.Settings.Signal.Short.Strategy.Clear();
            EnableStrategy(VbsPlugin.StrategyInternal);

            var lines = Capture();
            Assert.AreEqual(0, lines.Count,
                $"Expected no warnings for a healthy configuration, got: {string.Join(" | ", lines)}");
        }
        finally
        {
            GlobalData.Settings.Signal.Long.Strategy.Clear();
            GlobalData.Settings.Signal.Long.Strategy.AddRange(savedLong);
            GlobalData.Settings.Signal.Short.Strategy.Clear();
            GlobalData.Settings.Signal.Short.Strategy.AddRange(savedShort);
        }
    }

    [TestMethod]
    public void ConfigurationBump_InvalidatesExistingHubs()
    {
        var hub = new IntervalIndicatorHub();
        int before = hub.ConfigVersion;

        IndicatorConfiguration.Bump();

        Assert.AreNotEqual(IndicatorConfiguration.Version, before,
            "Bump must move the version so IndicatorData discards hubs built under the old settings");
        Assert.AreEqual(before, hub.ConfigVersion, "An existing hub keeps the version it was built under");
        Assert.AreEqual(IndicatorConfiguration.Version, new IntervalIndicatorHub().ConfigVersion,
            "A hub built after the bump must carry the new version");
    }
}
