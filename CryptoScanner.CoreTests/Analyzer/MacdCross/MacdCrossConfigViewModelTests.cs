using CryptoScanner.Analyzers.MacdCross;
using CryptoScanner.Analyzers.MacdCross.Config;

namespace CryptoScanner.CoreTests.Analyzer.MacdCross;

/// <summary>
/// The Avalonia editor behind the MACD crossover. It is the one host that cannot be checked by
/// opening it here, so the mapping between its fields and the settings object is tested directly.
/// The Photino host reads the settings class by reflection, so an editor that dropped a property
/// would make the two hosts disagree about the very same settings file.
/// </summary>
[TestClass]
public class MacdCrossConfigViewModelTests
{
    [TestMethod]
    public void TheStrategyTab_SurvivesALoadAndSaveRoundTrip()
    {
        var model = new StrategyMacdCrossSettingsViewModel();
        model.LoadConfig(new MacdCrossSettings
        {
            ConfirmationCandles = 2,
            MinimumDistancePercentage = 0.05m,
            RequireCrossBeyondZeroLine = true,
            AdxMinimum = 25m,
            AdxRecentlyBelow = 20m,
            AdxRecentlyWithinCandles = 6,
            RelativeVolumeMinimum = 2m,
            RelativeVolumeCandles = 2,
            RelativeVolumeAverageCandles = 40,
            ExitOnCrossBack = false,
            ExitConfirmationCandles = 1,
        });

        var settings = new MacdCrossSettings();
        model.SaveConfig(settings);

        Assert.AreEqual(2, settings.ConfirmationCandles);
        Assert.AreEqual(0.05m, settings.MinimumDistancePercentage);
        Assert.IsTrue(settings.RequireCrossBeyondZeroLine);
        Assert.AreEqual(25m, settings.AdxMinimum);
        Assert.AreEqual(20m, settings.AdxRecentlyBelow);
        Assert.AreEqual(6, settings.AdxRecentlyWithinCandles);
        Assert.AreEqual(2m, settings.RelativeVolumeMinimum);
        Assert.AreEqual(2, settings.RelativeVolumeCandles);
        Assert.AreEqual(40, settings.RelativeVolumeAverageCandles);
        Assert.IsFalse(settings.ExitOnCrossBack);
        Assert.AreEqual(1, settings.ExitConfirmationCandles);
    }


    /// <summary>
    /// The defaults are the bare idea: fire on the cross, leave on the cross back, no filters. A
    /// fresh editor has to show exactly that, or the first run made from the screen measures
    /// something else than the first run made from the queue.
    /// </summary>
    [TestMethod]
    public void AFreshEditor_ShowsTheBareRule()
    {
        var model = new StrategyMacdCrossSettingsViewModel();
        var fresh = new MacdCrossSettings();

        Assert.AreEqual(fresh.ConfirmationCandles, model.ConfirmationCandles);
        Assert.AreEqual(fresh.MinimumDistancePercentage, model.MinimumDistancePercentage);
        Assert.AreEqual(fresh.RequireCrossBeyondZeroLine, model.RequireCrossBeyondZeroLine);
        Assert.AreEqual(fresh.AdxMinimum, model.AdxMinimum);
        Assert.AreEqual(fresh.AdxRecentlyBelow, model.AdxRecentlyBelow);
        Assert.AreEqual(fresh.AdxRecentlyWithinCandles, model.AdxRecentlyWithinCandles);
        Assert.AreEqual(fresh.RelativeVolumeMinimum, model.RelativeVolumeMinimum);
        Assert.AreEqual(fresh.RelativeVolumeCandles, model.RelativeVolumeCandles);
        Assert.AreEqual(fresh.RelativeVolumeAverageCandles, model.RelativeVolumeAverageCandles);
        Assert.AreEqual(fresh.ExitOnCrossBack, model.ExitOnCrossBack);
        Assert.AreEqual(fresh.ExitConfirmationCandles, model.ExitConfirmationCandles);

        // Every filter off: the bare rule.
        Assert.AreEqual(0, fresh.ConfirmationCandles);
        Assert.AreEqual(0m, fresh.MinimumDistancePercentage);
        Assert.IsFalse(fresh.RequireCrossBeyondZeroLine);
        Assert.AreEqual(0m, fresh.AdxMinimum);
        Assert.AreEqual(0m, fresh.AdxRecentlyBelow);
        Assert.AreEqual(0m, fresh.RelativeVolumeMinimum);
        Assert.IsTrue(fresh.ExitOnCrossBack);
        Assert.AreEqual(0, fresh.ExitConfirmationCandles);
    }
}
