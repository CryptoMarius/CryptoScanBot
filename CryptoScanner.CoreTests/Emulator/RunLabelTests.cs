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
}
