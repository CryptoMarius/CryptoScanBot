using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>The text filter above the emulator's results grid: every word has to be in the label.</summary>
[TestClass]
public class RunResultsLabelFilterTests
{
    private const string Label = "mac Open-marker Speed Fast op 1d, 12h en 8h, wolk minstens 2 procent breed";


    [TestMethod]
    public void EmptyFilterShowsEverything()
    {
        Assert.IsTrue(RunResultsViewModel.LabelMatches(Label, ""));
        Assert.IsTrue(RunResultsViewModel.LabelMatches(Label, "   "));
        Assert.IsTrue(RunResultsViewModel.LabelMatches(null, null));
    }


    [TestMethod]
    public void EveryWordMustBePresentInAnyOrderAndCase()
    {
        Assert.IsTrue(RunResultsViewModel.LabelMatches(Label, "open mac"));
        Assert.IsTrue(RunResultsViewModel.LabelMatches(Label, "WOLK 12h"));
        Assert.IsFalse(RunResultsViewModel.LabelMatches(Label, "open dbr"));
        Assert.IsFalse(RunResultsViewModel.LabelMatches(null, "mac"));
    }
}
