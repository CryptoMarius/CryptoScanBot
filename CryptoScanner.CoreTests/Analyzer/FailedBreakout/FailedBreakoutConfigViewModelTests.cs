using CryptoScanner.Analyzers.FailedBreakout;
using CryptoScanner.Analyzers.FailedBreakout.Config;

namespace CryptoScanner.CoreTests.Analyzer.FailedBreakout;

/// <summary>
/// The Avalonia editor behind the failed breakout. It is the one host that cannot be checked by
/// opening it here, so the mapping between its checkboxes and the stored list of zone names is
/// tested directly - the same test the candle-pattern editor gets, and for the same reason: the
/// Photino host builds its checkboxes from the enum, so an editor that stored the names in a
/// different shape would make the two hosts disagree about the very same settings file.
/// </summary>
[TestClass]
public class FailedBreakoutConfigViewModelTests
{
    [TestMethod]
    public void TheStrategyTab_SurvivesALoadAndSaveRoundTrip()
    {
        var model = new StrategyFailedBreakoutSettingsViewModel();
        model.LoadConfig(new FailedBreakoutSettings
        {
            LookbackCandles = 30,
            BreakWithinCandles = 2,
            MinimumBreakPercentage = 0.75m,
            CloseWithinRangePercentage = 25m,
            RequireZone = ["Smc", "Dlz"],
            ZoneTolerancePercentage = 0.25m,
        });

        var settings = new FailedBreakoutSettings();
        model.SaveConfig(settings);

        Assert.AreEqual(30, settings.LookbackCandles);
        Assert.AreEqual(2, settings.BreakWithinCandles);
        Assert.AreEqual(0.75m, settings.MinimumBreakPercentage);
        Assert.AreEqual(25m, settings.CloseWithinRangePercentage);

        // Back in the order the enum declares them, not in the order they went in, so both hosts
        // hand the strategy an identically ordered list.
        CollectionAssert.AreEqual(new[] { "Dlz", "Smc" }, settings.RequireZone.ToArray());
        Assert.AreEqual(0.25m, settings.ZoneTolerancePercentage);
    }


    /// <summary>
    /// The zone names are written in lower case by hand in the settings file and in the emulator
    /// queue ("dlz"), and the strategy reads them case-insensitively. The editor has to do the same,
    /// or opening the configuration screen would silently clear a requirement that was set.
    /// </summary>
    [TestMethod]
    public void ZoneNamesTypedInLowerCase_AreStillTicked()
    {
        var model = new StrategyFailedBreakoutSettingsViewModel();
        model.LoadConfig(new FailedBreakoutSettings { RequireZone = ["dlz", "fvg"] });

        Assert.IsTrue(model.RequireDlz);
        Assert.IsTrue(model.RequireFvg);
        Assert.IsFalse(model.RequireSmc);

        var settings = new FailedBreakoutSettings();
        model.SaveConfig(settings);
        CollectionAssert.AreEqual(new[] { "Dlz", "Fvg" }, settings.RequireZone.ToArray());
    }


    /// <summary>
    /// Nothing ticked has to store an empty list and not a list with an empty name in it: the
    /// strategy treats an unknown name as a hard error, so a stray entry would throw on the first
    /// candle instead of quietly meaning "no requirement".
    /// </summary>
    [TestMethod]
    public void NothingTicked_StoresAnEmptyList()
    {
        var model = new StrategyFailedBreakoutSettingsViewModel();
        model.LoadConfig(new FailedBreakoutSettings { RequireZone = ["dlz"] });
        model.RequireDlz = false;

        var settings = new FailedBreakoutSettings { RequireZone = ["dlz"] };
        model.SaveConfig(settings);

        Assert.AreEqual(0, settings.RequireZone.Count);
    }
}
