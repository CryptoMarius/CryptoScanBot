using CryptoScanner.Analyzers.PsarCross;
using CryptoScanner.Analyzers.PsarCross.Config;

namespace CryptoScanner.CoreTests.Analyzer.PsarCross;

/// <summary>
/// The Avalonia editor behind the parabolic SAR cross. The Photino host reads the settings class by
/// reflection, so an editor that dropped a property would make the two hosts disagree about the very
/// same settings file.
/// </summary>
[TestClass]
public class PsarCrossConfigViewModelTests
{
    [TestMethod]
    public void TheStrategyTab_SurvivesALoadAndSaveRoundTrip()
    {
        var model = new StrategyPsarCrossSettingsViewModel();
        model.LoadConfig(new PsarCrossSettings
        {
            ConfirmationCandles = 2,
            MinimumDistancePercentage = 0.5m,
            RelativeVolumeMinimum = 2m,
            RelativeVolumeCandles = 2,
            RelativeVolumeAverageCandles = 40,
            ExitOnFlipBack = false,
            ExitConfirmationCandles = 1,
        });

        var settings = new PsarCrossSettings();
        model.SaveConfig(settings);

        Assert.AreEqual(2, settings.ConfirmationCandles);
        Assert.AreEqual(0.5m, settings.MinimumDistancePercentage);
        Assert.AreEqual(2m, settings.RelativeVolumeMinimum);
        Assert.AreEqual(2, settings.RelativeVolumeCandles);
        Assert.AreEqual(40, settings.RelativeVolumeAverageCandles);
        Assert.IsFalse(settings.ExitOnFlipBack);
        Assert.AreEqual(1, settings.ExitConfirmationCandles);
    }


    /// <summary>The defaults are the bare idea: fire on the flip, leave on the flip back, no filters.</summary>
    [TestMethod]
    public void AFreshEditor_ShowsTheBareRule()
    {
        var model = new StrategyPsarCrossSettingsViewModel();
        var fresh = new PsarCrossSettings();

        Assert.AreEqual(fresh.ConfirmationCandles, model.ConfirmationCandles);
        Assert.AreEqual(fresh.MinimumDistancePercentage, model.MinimumDistancePercentage);
        Assert.AreEqual(fresh.RelativeVolumeMinimum, model.RelativeVolumeMinimum);
        Assert.AreEqual(fresh.RelativeVolumeCandles, model.RelativeVolumeCandles);
        Assert.AreEqual(fresh.RelativeVolumeAverageCandles, model.RelativeVolumeAverageCandles);
        Assert.AreEqual(fresh.ExitOnFlipBack, model.ExitOnFlipBack);
        Assert.AreEqual(fresh.ExitConfirmationCandles, model.ExitConfirmationCandles);

        Assert.AreEqual(0, fresh.ConfirmationCandles);
        Assert.AreEqual(0m, fresh.MinimumDistancePercentage);
        Assert.AreEqual(0m, fresh.RelativeVolumeMinimum);
        Assert.IsTrue(fresh.ExitOnFlipBack);
        Assert.AreEqual(0, fresh.ExitConfirmationCandles);
        Assert.AreEqual(0, fresh.IntervalList.Count);
    }
}
