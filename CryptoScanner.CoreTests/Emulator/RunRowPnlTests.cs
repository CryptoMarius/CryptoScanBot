using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The unrealised and total PnL of a run come from its equity curve: the change over the run minus
/// the realised profit is what the still-open positions are worth at the end.
/// </summary>
[TestClass]
public class RunRowPnlTests
{
    [TestMethod]
    public void UnrealizedPnl_IsEquityChangeMinusRealisedProfit()
    {
        // Run 269 of Session2: every closed position won, the open ones are under water
        RunRow row = new() { Profit = 10123.50m, EquityStart = 100000m, EquityEnd = 103610.19m };

        Assert.AreEqual(-6513.31m, row.UnrealizedPnl);
        Assert.AreEqual(3610.19m, row.TotalPnl);
        Assert.AreEqual((-6513.31m).ToString("N2"), row.UnrealizedPnlText);
    }

    [TestMethod]
    public void WithoutEquityCurve_BothAreEmpty()
    {
        RunRow row = new() { Profit = 50m };

        Assert.IsNull(row.UnrealizedPnl);
        Assert.IsNull(row.TotalPnl);
        Assert.AreEqual("—", row.TotalPnlText);
    }

    [TestMethod]
    public void Filter_KnowsTheNewFields()
    {
        RunRow row = new() { Profit = 100m, EquityStart = 1000m, EquityEnd = 1050m };

        Assert.AreEqual(-50m, RunResultsViewModel.FilterFields["unrealized"](row));
        Assert.AreEqual(50m, RunResultsViewModel.FilterFields["total"](row));
    }

    [TestMethod]
    public void TotalPnlPerSide_IsRealisedPlusOpenOfThatSide()
    {
        // Longs won on paper but sit under water; shorts closed at a loss and have nothing open
        RunRow row = new() { ProfitLong = 800m, UnrealizedPnlLong = -1200m, ProfitShort = -150m, UnrealizedPnlShort = 0m };

        Assert.AreEqual(-400m, row.TotalPnlLong);
        Assert.AreEqual(-150m, row.TotalPnlShort);
        Assert.AreEqual(-400m, RunResultsViewModel.FilterFields["totallong"](row));
        Assert.AreEqual((-150m).ToString("N2"), row.TotalPnlShortText);
    }

    [TestMethod]
    public void TotalPnlPerSide_EmptyForOldRuns()
    {
        RunRow row = new() { ProfitLong = 800m, ProfitShort = -150m };

        Assert.IsNull(row.TotalPnlLong);
        Assert.AreEqual("—", row.TotalPnlLongText);
        Assert.AreEqual("—", row.TotalPnlShortText);
    }
}
