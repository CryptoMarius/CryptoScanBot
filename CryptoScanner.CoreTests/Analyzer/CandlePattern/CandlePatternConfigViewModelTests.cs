using CryptoScanner.Analyzers.CandlePattern;
using CryptoScanner.Analyzers.CandlePattern.Config;
using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.CoreTests.Analyzer.CandlePattern;

/// <summary>
/// The Avalonia editors behind the reversal shapes. They are the one host that cannot be checked by
/// opening it here, so the mapping between the checkboxes and the stored list of names is tested
/// directly instead.
/// <para>
/// The order matters and is not cosmetic: the strategy acts on the FIRST shape in the list that a
/// candle forms, so a list that came out in the order the boxes were ticked would make the two
/// hosts name a different pattern for the same candle.
/// </para>
/// </summary>
[TestClass]
public class CandlePatternConfigViewModelTests
{
    private static CandlePatternListViewModel LoadedWith(params string[] patterns)
    {
        var model = new CandlePatternListViewModel();
        model.LoadConfig([.. patterns]);
        return model;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The shared list of shapes
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TheList_ShowsEveryShapeInTheOrderTheEnumDeclaresThem()
    {
        var model = LoadedWith("Harami");

        CollectionAssert.AreEqual(Enum.GetNames<CryptoCandlePattern>(),
            model.Patterns.Select(p => p.Name).ToArray());
    }


    [TestMethod]
    public void TheList_TicksExactlyTheShapesTheSettingsName()
    {
        var model = LoadedWith("Harami", "Hammer");

        CollectionAssert.AreEquivalent(new[] { "Hammer", "Harami" },
            model.Patterns.Where(p => p.IsChecked).Select(p => p.Name).ToArray());
    }


    /// <summary>
    /// The list can also be typed by hand in the settings file or the emulator queue, where the
    /// scanner parses the names case-insensitively - the editor must not silently untick a shape
    /// that is actually switched on.
    /// </summary>
    [TestMethod]
    public void TheList_RecognisesANameInAnotherCasing()
    {
        var model = LoadedWith("tweezer");

        Assert.IsTrue(model.Patterns.Single(p => p.Name == nameof(CryptoCandlePattern.Tweezer)).IsChecked);
    }


    [TestMethod]
    public void TheList_SavesTheTickedShapesInTheOrderOfTheEnum()
    {
        var model = LoadedWith();
        model.Patterns.Single(p => p.Name == nameof(CryptoCandlePattern.Tweezer)).IsChecked = true;
        model.Patterns.Single(p => p.Name == nameof(CryptoCandlePattern.Hammer)).IsChecked = true;

        // Hammer is declared before Tweezer, so it comes first however the boxes were ticked.
        CollectionAssert.AreEqual(new[] { "Hammer", "Tweezer" }, model.SaveConfig().ToArray());
    }


    [TestMethod]
    public void TheList_WithNothingTicked_SavesAnEmptyList()
    {
        var model = LoadedWith("Engulfing");
        foreach (var pattern in model.Patterns)
            pattern.IsChecked = false;

        Assert.AreEqual(0, model.SaveConfig().Count);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The strategy tab
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TheStrategyTab_SurvivesALoadAndSaveRoundTrip()
    {
        var model = new StrategyCandlePatternSettingsViewModel();
        model.LoadConfig(new CandlePatternStrategySettings
        {
            Patterns = ["Harami"],
            PrecedingCandles = 5,
            PrecedingPercentage = 1.25m,
            Shape = { MinWickPercentage = 55m, TweezerTolerancePercentage = 3m },
            RequireZone = ["Smc", "Dlz"],
            ZoneTolerancePercentage = 0.25m,
        });

        var settings = new CandlePatternStrategySettings();
        model.SaveConfig(settings);

        CollectionAssert.AreEqual(new[] { "Harami" }, settings.Patterns.ToArray());
        Assert.AreEqual(5, settings.PrecedingCandles);
        Assert.AreEqual(1.25m, settings.PrecedingPercentage);
        Assert.AreEqual(55m, settings.Shape.MinWickPercentage);
        Assert.AreEqual(3m, settings.Shape.TweezerTolerancePercentage);

        // Back in the order the enum declares them, not in the order they went in - the same rule
        // the pattern list follows, so both hosts hand the strategy an identically ordered list.
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
        var model = new StrategyCandlePatternSettingsViewModel();
        model.LoadConfig(new CandlePatternStrategySettings { RequireZone = ["dlz", "fvg"] });

        Assert.IsTrue(model.RequireDlz);
        Assert.IsTrue(model.RequireFvg);
        Assert.IsFalse(model.RequireSmc);

        var settings = new CandlePatternStrategySettings();
        model.SaveConfig(settings);
        CollectionAssert.AreEqual(new[] { "Dlz", "Fvg" }, settings.RequireZone.ToArray());
    }
}
