using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// Money that goes into or out of the account without a trade behind it, and what the capital line
/// does with it.
/// <para>
/// The line is drawn from the balances, so booking in 5.000 by hand raises it by 5.000 - which reads
/// as a very good day and is nothing of the sort. Deleting a coin is the same problem the other way
/// round, and worse: the balance disappears from the Asset table altogether, so there is nothing left
/// to explain the drop with. Both are recorded in AssetAdjustment, and the second line of the chart
/// has them taken out again.
/// </para>
/// </summary>
[TestClass]
public class AssetAdjustmentTests : TestBase
{
    private static readonly DateTime Day = new(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc);

    private static CryptoDatabase Arrange(out CryptoSymbol symbol)
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.CurrentEmulatorRunId = null;

        CryptoDatabase database = new();
        database.Open();
        symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);
        database.Connection.Execute("delete from AssetSnapshot");
        database.Connection.Execute("delete from AssetAdjustment");
        database.Connection.Execute("delete from EmulatorRun");

        AssetSnapshotTools.Reset();
        return database;
    }


    private static void SetBalance(string name, decimal total)
    {
        CryptoAsset asset = PaperAssets.FindOrCreateAsset(GlobalData.ActiveExchange!, name);
        asset.Total = total;
        asset.Free = total;
        asset.Locked = 0;
    }


    /// <summary>
    /// Only the days this test wrote itself.
    /// <para>
    /// The snapshot table is shared with every other test in the suite - and with a second
    /// "dotnet test" on the same machine, because they all run against the same TestData folder - so
    /// counting every row in it makes the outcome depend on what else is running. A window around the
    /// dates of this test keeps that out.
    /// </para>
    /// </summary>
    private static List<AssetSnapshotTools.AssetSnapshotDay> LoadTestDays() =>
        [.. AssetSnapshotTools.LoadDailyTotals(null).Where(d => d.Date >= Day && d.Date <= Day.AddDays(7))];


    private static List<CryptoAssetAdjustment> ReadLedger(CryptoDatabase database)
    {
        return [.. database.Connection.Query<CryptoAssetAdjustment>(
            "select * from AssetAdjustment order by Id")];
    }


    [TestMethod]
    public void Booking_money_in_by_hand_is_not_growth()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        IClock previousClock = GlobalData.Clock;
        try
        {
            // Day one: 10.000 in the account and nothing out of the ordinary
            GlobalData.Clock = new EmulatorClock { UtcNow = Day };
            SetBalance("USDT", 10000m);
            AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

            // Day two: the user tops the account up by 5.000
            GlobalData.Clock = new EmulatorClock { UtcNow = Day.AddDays(1).AddHours(10) };
            PaperAssets.SetAsset(GlobalData.ActiveExchange!, "USDT", 15000m);
            AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day.AddDays(1));

            List<AssetSnapshotTools.AssetSnapshotDay> days = LoadTestDays();
            Assert.AreEqual(2, days.Count, "one day before and one day after the correction");

            Assert.AreEqual(10000m, days[0].Value);
            Assert.AreEqual(0m, days[0].Adjustment);
            Assert.AreEqual(10000m, days[0].ValueWithoutAdjustments);

            Assert.AreEqual(15000m, days[1].Value, "the account really does hold 15.000");
            Assert.AreEqual(5000m, days[1].Adjustment);
            Assert.AreEqual(10000m, days[1].ValueWithoutAdjustments, "but nothing was earned");
        }
        finally
        {
            GlobalData.Clock = previousClock;
        }
    }


    [TestMethod]
    public void Deleting_a_coin_is_recorded_although_its_balance_is_gone()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        IClock previousClock = GlobalData.Clock;
        try
        {
            symbol.LastPrice = 10m;

            GlobalData.Clock = new EmulatorClock { UtcNow = Day };
            SetBalance("USDT", 9000m);
            SetBalance(symbol.Base, 100m); // worth 1.000, so 10.000 in total
            AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

            // The paper-assets screen deletes a coin by correcting it to zero, and the balance is
            // then dropped from the Asset table completely.
            GlobalData.Clock = new EmulatorClock { UtcNow = Day.AddDays(1).AddHours(10) };
            PaperAssets.SetAsset(GlobalData.ActiveExchange!, symbol.Base, 0m);
            Assert.IsFalse(GlobalData.ActiveExchange!.Data.AssetList.ContainsKey(symbol.Base),
                "the coin is gone from the balances");

            AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day.AddDays(1));

            // The ledger kept what the balances could not
            CryptoAssetAdjustment entry = ReadLedger(database).Single();
            Assert.AreEqual(symbol.Base, entry.Name);
            Assert.AreEqual(CryptoAssetAdjustmentReason.ManualCorrection, entry.Reason);
            Assert.AreEqual(-100m, entry.Quantity);
            Assert.AreEqual(-1000m, entry.Value);

            List<AssetSnapshotTools.AssetSnapshotDay> days = LoadTestDays();
            Assert.AreEqual(9000m, days[1].Value, "the account is 1.000 lighter");
            Assert.AreEqual(10000m, days[1].ValueWithoutAdjustments, "but nothing was lost on a trade");
        }
        finally
        {
            GlobalData.Clock = previousClock;
        }
    }


    [TestMethod]
    public void Starting_over_leaves_both_halves_in_the_ledger()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        IClock previousClock = GlobalData.Clock;
        CryptoQuoteData quoteData = GlobalData.AddQuoteData("USDT");
        bool previousFetchCandles = quoteData.FetchCandles;
        try
        {
            // ResetAssets only hands the start capital to quote coins that are actually traded
            quoteData.FetchCandles = true;

            GlobalData.Clock = new EmulatorClock { UtcNow = Day };
            SetBalance("USDT", 12345m);

            PaperAssets.ResetAssets(GlobalData.ActiveExchange!, 10000m);

            List<CryptoAssetAdjustment> ledger = ReadLedger(database);
            CryptoAssetAdjustment thrownAway = ledger.Single(e => e.Reason == CryptoAssetAdjustmentReason.Reset);
            Assert.AreEqual(-12345m, thrownAway.Value, "what was in the account leaves it");

            CryptoAssetAdjustment handedOut = ledger.Single(e =>
                e.Reason == CryptoAssetAdjustmentReason.StartCapital && e.Name == "USDT");
            Assert.AreEqual(10000m, handedOut.Value, "and the start capital comes in");
        }
        finally
        {
            quoteData.FetchCandles = previousFetchCandles;
            GlobalData.Clock = previousClock;
        }
    }


    [TestMethod]
    public void A_correction_that_changes_nothing_is_not_recorded()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        SetBalance("USDT", 10000m);

        PaperAssets.SetAsset(GlobalData.ActiveExchange!, "USDT", 10000m);

        Assert.AreEqual(0, ReadLedger(database).Count);
    }


    /// <summary>
    /// The accumulation on its own, without a database - the awkward cases are all about which day a
    /// booking is counted on.
    /// </summary>
    [TestMethod]
    public void Bookings_are_counted_from_the_first_day_of_the_series()
    {
        List<AssetSnapshotTools.AssetSnapshotDay> days =
        [
            new() { Date = Day, Value = 10000m },
            new() { Date = Day.AddDays(1), Value = 10100m },
            new() { Date = Day.AddDays(3), Value = 15200m },
        ];

        List<AssetAdjustmentTools.AdjustmentDay> adjustments =
        [
            // Before the series started: part of the starting point, not of its course
            new() { Date = Day.AddDays(-5), Value = 10000m },
            // On a day without a snapshot, so it counts towards the first day that follows it
            new() { Date = Day.AddDays(2), Value = 5000m },
        ];

        AssetSnapshotTools.ApplyAdjustments(days, adjustments);

        Assert.AreEqual(0m, days[0].Adjustment);
        Assert.AreEqual(10000m, days[0].ValueWithoutAdjustments);
        Assert.AreEqual(0m, days[1].Adjustment);
        Assert.AreEqual(10100m, days[1].ValueWithoutAdjustments);
        Assert.AreEqual(5000m, days[2].Adjustment);
        Assert.AreEqual(10200m, days[2].ValueWithoutAdjustments, "100 earned, 5.000 booked in");
    }
}
