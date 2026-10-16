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


    private static RunRow Row() => new()
    {
        Label = "bbm long 1h",
        Profit = 250m,
        Invested = 10_000m,               // profit% 2,5
        PositionsWon = 6,
        PositionsLost = 4,                // win% 60
        MaxDrawdownPercentage = 12m,
        StartCapital = 1_000m,
        UseAssetManagement = true,        // return% 25
    };


    [TestMethod]
    public void NumericConditionsTestTheColumns()
    {
        RunRow row = Row();
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "bbm return>5 dd<20"));
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "profit>=250 profit%>2,4 win>59.9"));
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "dd>5 dd<20"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "dd>15"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "profit<-100"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "mac return>5"));
    }


    [TestMethod]
    public void SpacesAroundTheOperatorAreAllowed()
    {
        RunRow row = Row();
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "return > 1.0"));
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "bbm  dd <= 12   win >50"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "return > 30"));
    }


    [TestMethod]
    public void ReturnWithoutAssetManagementNeverPasses()
    {
        RunRow row = Row();
        row.UseAssetManagement = false;
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "return>-1000"));
    }


    [TestMethod]
    public void UnfinishedOrUnknownConditionsDoNotFilter()
    {
        RunResultsViewModel.ParseFilter("bbm dd< foo>3", out List<string> unknown);
        CollectionAssert.AreEqual(new[] { "foo" }, unknown);
        Assert.IsTrue(RunResultsViewModel.RowMatches(Row(), "bbm dd<"));
    }
}
