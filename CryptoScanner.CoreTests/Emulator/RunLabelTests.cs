using CryptoScanner.Emulator.Engine;
using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The name a run is stored and shown under. Small, but it is the only handle the results grid and
/// every report have on which run is which - and it produced "storsi storsi 3 limiet" for months
/// because the queue label named the strategy and the code put it in front again.
/// </summary>
[TestClass]
public class RunLabelTests
{
    [TestMethod]
    public void Label_ThatDoesNotNameTheAlgorithm_GetsItInFront()
    {
        Assert.AreEqual("storsi 3 limiet, index >= 3.5",
            MainWindowViewModel.BuildRunLabel("storsi", "3 limiet, index >= 3.5", null));
    }


    [TestMethod]
    public void Label_ThatAlreadyNamesTheAlgorithm_IsLeftAlone()
    {
        Assert.AreEqual("storsi 3 limiet, index >= 3.5",
            MainWindowViewModel.BuildRunLabel("storsi", "storsi 3 limiet, index >= 3.5", null));
    }


    [TestMethod]
    public void Label_MatchIsCaseInsensitive()
    {
        Assert.AreEqual("Storsi referentie",
            MainWindowViewModel.BuildRunLabel("storsi", "Storsi referentie", null));
    }


    [TestMethod]
    public void Label_ThatIsExactlyTheAlgorithmName_IsLeftAlone()
    {
        Assert.AreEqual("dlz", MainWindowViewModel.BuildRunLabel("dlz", "dlz", null));
    }


    /// <summary>
    /// The word boundary: a "dlz" entry must not read a label about "dlz.near" as already naming it,
    /// because then two different strategies end up under labels that cannot be told apart.
    /// </summary>
    [TestMethod]
    public void Label_OfADifferentStrategyWithTheSamePrefix_StillGetsThePrefix()
    {
        Assert.AreEqual("dlz dlz.near referentie",
            MainWindowViewModel.BuildRunLabel("dlz", "dlz.near referentie", null));

        // ...while the entry that really is dlz.near keeps its own label.
        Assert.AreEqual("dlz.near referentie",
            MainWindowViewModel.BuildRunLabel("dlz.near", "dlz.near referentie", null));
    }


    [TestMethod]
    public void Label_ThatMerelyStartsWithTheSameLetters_GetsThePrefix()
    {
        Assert.AreEqual("stobb stobbelen over de drempel",
            MainWindowViewModel.BuildRunLabel("stobb", "stobbelen over de drempel", null));
    }


    [TestMethod]
    public void Label_GetsTheBaseIntervalAppendedWhenTheEntryChoseOne()
    {
        Assert.AreEqual("dlz referentie [1m]",
            MainWindowViewModel.BuildRunLabel("dlz", "dlz referentie", "1m"));
        Assert.AreEqual("dlz dlz referentie [1m]".Replace("dlz dlz", "dlz"),
            MainWindowViewModel.BuildRunLabel("dlz", "dlz referentie", "1m"));
    }


    [TestMethod]
    public void Label_WithoutABaseIntervalGetsNoBrackets()
    {
        Assert.AreEqual("dlz referentie",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", null));
        Assert.AreEqual("dlz referentie",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", "   "));
    }


    [TestMethod]
    public void Label_GetsTheStartCapitalAppendedWhenTheEntryChoseOne()
    {
        Assert.AreEqual("dlz referentie [start 5000]",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", null, 5000m));
    }


    [TestMethod]
    public void Label_ShowsBaseIntervalAndStartCapitalTogether()
    {
        Assert.AreEqual("dlz referentie [5m] [start 25000]",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", "5m", 25000m));
    }


    [TestMethod]
    public void Label_GetsThePeriodAppendedWhenTheEntryChoseOne()
    {
        Assert.AreEqual("dlz referentie [2026-01-01..2026-04-30]",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", null, null, "2026-01-01..2026-04-30"));
        Assert.AreEqual("dlz referentie [5m] [start 25000] [2026-05-01..2026-08-31]",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", "5m", 25000m, "2026-05-01..2026-08-31"));
    }


    /// <summary>
    /// The period only reaches the label when the entry chose one: a queue of forty entries on the
    /// run configuration's window must not get forty identical suffixes.
    /// </summary>
    [TestMethod]
    public void Period_ComesFromTheRunConfigUnlessTheEntrySaysOtherwise()
    {
        DateTime configFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime configTo = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime april = new(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);
        DateTime may = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.AreEqual((configFrom, configTo, false),
            MainWindowViewModel.ResolvePeriod(null, null, configFrom, configTo));

        // One date on its own is enough: the other comes from the configuration.
        Assert.AreEqual((configFrom, april, true),
            MainWindowViewModel.ResolvePeriod(null, april, configFrom, configTo));
        Assert.AreEqual((may, configTo, true),
            MainWindowViewModel.ResolvePeriod(may, null, configFrom, configTo));
        Assert.AreEqual((may, april, true),
            MainWindowViewModel.ResolvePeriod(may, april, configFrom, configTo));

        Assert.AreEqual("2026-01-01..2026-04-30", MainWindowViewModel.FormatPeriod(configFrom, april));
    }


    /// <summary>
    /// The dates are typed by hand in the queue file, in the short form the run configuration uses.
    /// What is worth a test is that they bind at all - a nullable DateTime behind a converter
    /// written for the non-nullable one - and that they come back as UTC, because the replay
    /// window is aligned in UTC and a local-time date would shift it by two hours in summer.
    /// </summary>
    [TestMethod]
    public void Period_BindsFromTheQueueFile()
    {
        var opties = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var eersteHelft = System.Text.Json.JsonSerializer.Deserialize<EmulatorQueueEntry>(
            """{"Label":"MA1","FromDate":"2026-01-01","ToDate":"2026-04-30"}""", opties);
        var alleenEinde = System.Text.Json.JsonSerializer.Deserialize<EmulatorQueueEntry>(
            """{"Label":"MA2","ToDate":"2026-04-30T00:00:00Z"}""", opties);
        var weggelaten = System.Text.Json.JsonSerializer.Deserialize<EmulatorQueueEntry>(
            """{"Label":"CA0"}""", opties);

        Assert.AreEqual(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), eersteHelft!.FromDate);
        Assert.AreEqual(new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc), eersteHelft.ToDate);
        Assert.AreEqual(DateTimeKind.Utc, eersteHelft.FromDate!.Value.Kind);

        Assert.IsNull(alleenEinde!.FromDate);
        Assert.AreEqual(new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc), alleenEinde.ToDate);

        Assert.IsNull(weggelaten!.FromDate, "omitted has to fall back to the run configuration");
        Assert.IsNull(weggelaten.ToDate);

        // And back out in the short form, so a saved queue stays readable by hand.
        string json = System.Text.Json.JsonSerializer.Serialize(eersteHelft);
        StringAssert.Contains(json, "\"FromDate\":\"2026-01-01\"");
        StringAssert.Contains(json, "\"ToDate\":\"2026-04-30\"");
    }


    [TestMethod]
    public void Label_WithoutAStartCapitalGetsNoBrackets()
    {
        Assert.AreEqual("dlz referentie",
            MainWindowViewModel.BuildRunLabel("dlz", "referentie", null, null));
    }


    /// <summary>
    /// The queue file is edited by hand, so the fallback rule matters: only a positive amount counts
    /// as a choice. An omitted digit or a stray minus must not start a run with no money - since the
    /// balances really constrain trading, such a run makes zero trades and reads as a strategy that
    /// never signals.
    /// </summary>
    [TestMethod]
    public void StartCapital_OnlyAPositiveAmountOverridesTheRunConfig()
    {
        Assert.AreEqual((5000m, true), MainWindowViewModel.ResolveStartCapital(5000m, 10000m));
        Assert.AreEqual((10000m, false), MainWindowViewModel.ResolveStartCapital(null, 10000m));
        Assert.AreEqual((10000m, false), MainWindowViewModel.ResolveStartCapital(0m, 10000m));
        Assert.AreEqual((10000m, false), MainWindowViewModel.ResolveStartCapital(-5000m, 10000m));
    }


    /// <summary>
    /// A queue entry can switch the paper balances off for its own run. It reads as a plain nullable
    /// bool, so what is worth a test is that the JSON actually reaches the property: the file is
    /// edited by hand, and a field that silently does not bind produces a run that looks like a
    /// measurement while being a copy of its own reference. That is exactly what putting it in
    /// TradingOverrides did - ApplyRunOverrides assigned the value back from the run configuration.
    /// </summary>
    [TestMethod]
    public void UseAssetManagement_BindsFromTheQueueFile()
    {
        var opties = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var uit = System.Text.Json.JsonSerializer.Deserialize<EmulatorQueueEntry>(
            """{"Label":"KA2","UseAssetManagement":false}""", opties);
        var aan = System.Text.Json.JsonSerializer.Deserialize<EmulatorQueueEntry>(
            """{"Label":"KA3","UseAssetManagement":true}""", opties);
        var weggelaten = System.Text.Json.JsonSerializer.Deserialize<EmulatorQueueEntry>(
            """{"Label":"CA0"}""", opties);

        Assert.AreEqual(false, uit!.UseAssetManagement);
        Assert.AreEqual(true, aan!.UseAssetManagement);
        Assert.IsNull(weggelaten!.UseAssetManagement, "omitted has to fall back to the run configuration");

        // The fallback the queue loop applies, spelled out: only an entry that says something wins.
        Assert.IsFalse(uit.UseAssetManagement ?? true);
        Assert.IsTrue(weggelaten.UseAssetManagement ?? true);
        Assert.IsFalse(weggelaten.UseAssetManagement ?? false);
    }
}
