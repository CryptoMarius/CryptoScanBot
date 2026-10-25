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
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "bbm return%>5 drawdown%<20"));
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "profit>=250 profit%>2,4 win%>59.9"));
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "drawdown%>5 drawdown%<20"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "drawdown%>15"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "profit<-100"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "mac return%>5"));
    }


    [TestMethod]
    public void SpacesAroundTheOperatorAreAllowed()
    {
        RunRow row = Row();
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "return% > 1.0"));
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "bbm  drawdown% <= 12   win% >50"));
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "return% > 30"));
    }


    [TestMethod]
    public void ReturnWithoutAssetManagementNeverPasses()
    {
        RunRow row = Row();
        row.UseAssetManagement = false;
        Assert.IsFalse(RunResultsViewModel.RowMatches(row, "return%>-1000"));
    }


    [TestMethod]
    public void UnfinishedOrUnknownConditionsDoNotFilter()
    {
        RunResultsViewModel.ParseFilter("bbm drawdown%< foo>3", out List<string> unknown);
        CollectionAssert.AreEqual(new[] { "foo" }, unknown);
        Assert.IsTrue(RunResultsViewModel.RowMatches(Row(), "bbm drawdown%<"));
    }


    [TestMethod]
    public void FilterNameIsTheHeaderWithoutSpacesAndDots()
    {
        Assert.AreEqual("recoveryfactor", RunResultsViewModel.FilterName("Recovery factor"));
        Assert.AreEqual("totalpnl", RunResultsViewModel.FilterName("Total PnL"));
        Assert.AreEqual("peakcap", RunResultsViewModel.FilterName("Peak cap."));
        Assert.AreEqual("win%", RunResultsViewModel.FilterName("Win %"));

        RunRow row = Row();
        Assert.IsTrue(RunResultsViewModel.RowMatches(row, "Win%>=60 PEAKCAP.>=0"), "hoofdletters en de punt van de kop maken niet uit");
    }


    [TestMethod]
    public void InventedShortNamesAreGone()
    {
        // Replaced on 25-10-2026 by the header names; only counts stays, it has no column.
        RunResultsViewModel.ParseFilter("rf>2 dd<20 w10>0 total>0 win>50 return>5 long>0 counts=1", out List<string> unknown);
        CollectionAssert.AreEqual(new[] { "rf", "dd", "w10", "total", "win", "return", "long" }, unknown);
    }


    /// <summary>
    /// Every number column of the grid can be filtered on by the name of its header, so a new column
    /// cannot be added without one. Reads the column headers from the view itself.
    /// </summary>
    [TestMethod]
    public void EveryNumberColumnHasItsHeaderAsFilterName()
    {
        string? view = FindView();
        if (view == null)
        {
            Assert.Inconclusive("RunResultsView.axaml not found from the test folder.");
            return;
        }

        // Text columns: the label is the word filter, the rest are dates, names and durations of the run itself.
        string[] textColumns = ["Started", "Build", "Label", "Period", "Finished", "Duration", "Result"];
        List<string> missing = [];
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
            File.ReadAllText(view), "<DataGrid(?:Text|Template)Column Header=\"([^\"]+)\""))
        {
            string header = match.Groups[1].Value;
            if (!textColumns.Contains(header) && !RunResultsViewModel.FilterFields.ContainsKey(RunResultsViewModel.FilterName(header)))
                missing.Add(header);
        }
        CollectionAssert.AreEqual(new string[0], missing, "kolommen zonder filternaam: " + string.Join(", ", missing));
    }

    private static string? FindView()
    {
        for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            string candidate = Path.Combine(folder.FullName, "CryptoScanner.Emulator", "Views", "RunResultsView.axaml");
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
