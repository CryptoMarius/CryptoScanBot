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

        Assert.AreEqual(-50m, RunResultsViewModel.FilterFields["unrealizedpnl"](row));
        Assert.AreEqual(50m, RunResultsViewModel.FilterFields["totalpnl"](row));
    }

    [TestMethod]
    public void TotalPnlPerSide_IsRealisedPlusOpenOfThatSide()
    {
        // Longs won on paper but sit under water; shorts closed at a loss and have nothing open
        RunRow row = new() { ProfitLong = 800m, UnrealizedPnlLong = -1200m, ProfitShort = -150m, UnrealizedPnlShort = 0m };

        Assert.AreEqual(-400m, row.TotalPnlLong);
        Assert.AreEqual(-150m, row.TotalPnlShort);
        Assert.AreEqual(-400m, RunResultsViewModel.FilterFields["totalpnllong"](row));
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

    [TestMethod]
    public void RecoveryFactor_IsTotalPnlOverTheFallInUsdt()
    {
        // Run 112 of the HyperLiquid Session1: start 2000, Return % 24.6, but the dip was larger than the result
        RunRow row = new()
        {
            Profit = 400m, EquityStart = 2000m, EquityEnd = 2501.65m, OpenDrawdown = 519.66m,
            PositionsWon = 2000, PositionsLost = 432, ProfitWithoutBestTen = 374.95m,
        };

        Assert.AreEqual(0.97m, Math.Round(row.RecoveryFactor!.Value, 2));
        Assert.IsTrue(row.RecoveryFactorCounts);
        Assert.IsNull(row.RecoveryFactorReason);
        Assert.AreEqual(0.97m, Math.Round(RunResultsViewModel.FilterFields["recoveryfactor"](row)!.Value, 2));
        Assert.AreEqual(1m, RunResultsViewModel.FilterFields["counts"](row));
    }

    [TestMethod]
    public void RecoveryFactor_EmptyWithoutAFallInUsdt()
    {
        // Runs from before database version 103 have no OpenDrawdown; a run that never fell has 0
        RunRow old = new() { Profit = 500m, PositionsWon = 40, ProfitWithoutBestTen = 100m };
        RunRow neverFell = new() { Profit = 500m, EquityStart = 1000m, EquityEnd = 1500m, OpenDrawdown = 0m, PositionsWon = 40, ProfitWithoutBestTen = 100m };

        Assert.IsNull(old.RecoveryFactor);
        Assert.AreEqual("—", old.RecoveryFactorText);
        Assert.IsFalse(old.RecoveryFactorCounts);
        Assert.IsNull(neverFell.RecoveryFactor);
        Assert.IsFalse(neverFell.RecoveryFactorCounts);
    }

    [TestMethod]
    public void RecoveryFactor_DoesNotCountWithFewTradesOrOnTheBestTen()
    {
        RunRow fewTrades = new() { EquityStart = 1000m, EquityEnd = 1200m, OpenDrawdown = 20m, PositionsWon = 20, PositionsLost = 9, ProfitWithoutBestTen = 50m };
        RunRow restsOnBestTen = new() { EquityStart = 1000m, EquityEnd = 1200m, OpenDrawdown = 20m, PositionsWon = 50, PositionsLost = 10, ProfitWithoutBestTen = -5m };

        Assert.AreEqual(10m, fewTrades.RecoveryFactor);
        Assert.IsFalse(fewTrades.RecoveryFactorCounts);
        Assert.IsTrue(fewTrades.RecoveryFactorReason!.Contains("fewer than 30"));
        Assert.IsFalse(restsOnBestTen.RecoveryFactorCounts);
        Assert.IsTrue(restsOnBestTen.RecoveryFactorReason!.Contains("best 10"));
        Assert.IsTrue(restsOnBestTen.RecoveryFactorOpacity < 1.0);
    }
}
