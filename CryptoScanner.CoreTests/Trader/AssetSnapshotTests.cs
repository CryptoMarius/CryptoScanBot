using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The daily capital history behind the growth chart on the dashboard.
/// <para>
/// The case worth having here is the short. A short is booked entirely in quote (see
/// <see cref="PaperAssets.Change"/>): the sale proceeds land on the balance at the entry and the
/// buyback is paid from it again at the exit. Read the balance in between and the capital looks
/// higher than it is, by the full size of the position - a run that happens to be sitting on ten
/// open shorts would draw a peak that is not there. <see cref="CryptoAssetSnapshot.ShortQuantity"/>
/// is what corrects that, and these tests pin it down.
/// </para>
/// </summary>
[TestClass]
public class AssetSnapshotTests : TestBase
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

        // Static bookkeeping of "which day did we last record", so one test cannot suppress the
        // snapshot of the next one.
        AssetSnapshotTools.Reset();
        return database;
    }


    /// <summary>Put a coin in the balance list with everything free.</summary>
    private static void SetBalance(string name, decimal total)
    {
        CryptoAsset asset = PaperAssets.FindOrCreateAsset(GlobalData.ActiveExchange!, name);
        asset.Total = total;
        asset.Free = total;
        asset.Locked = 0;
    }


    /// <summary>
    /// An open short of <paramref name="quantity"/> base coins, without going through the trader:
    /// the snapshot only reads side, status and quantity.
    /// </summary>
    private static void AddOpenShort(CryptoDatabase database, CryptoSymbol symbol, decimal quantity)
    {
        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb", CryptoTradeSide.Short, "Test",
            symbol.Data.SymbolIntervalList[0], Day);
        position.Status = CryptoPositionStatus.Trading;
        position.Quantity = quantity;
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);
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


    private static List<CryptoAssetSnapshot> ReadSnapshot(CryptoDatabase database)
    {
        return [.. database.Connection.Query<CryptoAssetSnapshot>(
            "select * from AssetSnapshot order by Name")];
    }


    [TestMethod]
    public void A_balance_without_positions_is_the_capital_of_that_day()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        SetBalance("USDT", 10000m);

        AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

        List<CryptoAssetSnapshot> rows = ReadSnapshot(database);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("USDT", rows[0].Name);
        Assert.AreEqual(1m, rows[0].Price, "the reference coin is worth one of itself");
        Assert.AreEqual(10000m, rows[0].Value);

        Assert.AreEqual(10000m, LoadTestDays().Single().Value);
    }


    [TestMethod]
    public void Base_coins_of_an_open_long_count_at_the_market_price()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        symbol.LastPrice = 10m;

        // 100 TEST bought at 10 - a long moves the money from quote into base, and without valuing
        // that base the capital would drop by the size of the position at the entry.
        SetBalance("USDT", 9000m);
        SetBalance(symbol.Base, 100m);

        AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

        Assert.AreEqual(10000m, LoadTestDays().Single().Value);
    }


    [TestMethod]
    public void An_open_short_does_not_inflate_the_capital()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        symbol.LastPrice = 10m;

        // 100 TEST sold short at 10: the 1.000 proceeds are on the balance, the 100 coins are owed.
        SetBalance("USDT", 11000m);
        AddOpenShort(database, symbol, 100m);

        AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

        List<CryptoAssetSnapshot> rows = ReadSnapshot(database);
        CryptoAssetSnapshot baseRow = rows.Single(r => r.Name == symbol.Base);
        Assert.AreEqual(100m, baseRow.ShortQuantity);
        Assert.AreEqual(-1000m, baseRow.Value, "the debt is worth what buying it back costs");

        // Nothing has been earned yet, so the capital may not have moved either
        Assert.AreEqual(10000m, LoadTestDays().Single().Value);
    }


    [TestMethod]
    public void A_short_in_profit_raises_the_capital_by_the_profit()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);

        // Same short as above, but the price dropped from 10 to 9: buying back the 100 coins now
        // costs 900 instead of 1.000, so 100 has been earned.
        symbol.LastPrice = 9m;
        SetBalance("USDT", 11000m);
        AddOpenShort(database, symbol, 100m);

        AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

        Assert.AreEqual(10100m, LoadTestDays().Single().Value);
    }


    [TestMethod]
    public void A_second_snapshot_on_the_same_day_replaces_the_first()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        SetBalance("USDT", 10000m);
        AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

        SetBalance("USDT", 10500m);
        AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

        Assert.AreEqual(1, ReadSnapshot(database).Count, "one day, one row per coin");
        Assert.AreEqual(10500m, LoadTestDays().Single().Value);
    }


    [TestMethod]
    public void A_day_that_already_has_a_snapshot_is_not_captured_again()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        IClock previousClock = GlobalData.Clock;
        try
        {
            GlobalData.Clock = new EmulatorClock { UtcNow = Day.AddHours(9) };
            SetBalance("USDT", 10000m);

            AssetSnapshotTools.CaptureIfDue(GlobalData.ActiveExchange!);

            // The balance changes during the day, but the day already has its snapshot
            SetBalance("USDT", 12345m);
            AssetSnapshotTools.CaptureIfDue(GlobalData.ActiveExchange!);
            Assert.AreEqual(10000m, LoadTestDays().Single().Value);

            // The next day does get one, and the two are returned in date order
            GlobalData.Clock = new EmulatorClock { UtcNow = Day.AddDays(1) };
            AssetSnapshotTools.CaptureIfDue(GlobalData.ActiveExchange!);

            List<AssetSnapshotTools.AssetSnapshotDay> days = LoadTestDays();
            Assert.AreEqual(2, days.Count, "the day itself and the one after it");
            Assert.AreEqual(Day, days[0].Date);
            Assert.AreEqual(10000m, days[0].Value);
            Assert.AreEqual(Day.AddDays(1), days[1].Date);
            Assert.AreEqual(12345m, days[1].Value);
        }
        finally
        {
            GlobalData.Clock = previousClock;
        }
    }


    [TestMethod]
    public void Snapshots_of_a_run_are_separated_from_the_live_ones()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol _);
        try
        {
            SetBalance("USDT", 10000m);
            AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

            // A real run row, because AssetSnapshot.EmulatorRunId is a foreign key to it and SQLite
            // does enforce that here - an invented run id silently loses the whole snapshot.
            int runId = (int)database.Connection.Insert(new CryptoEmulatorRun
            {
                StartedAt = Day,
                FromDate = Day,
                ToDate = Day.AddDays(1),
                ConfigJson = "{}",
            });

            GlobalData.CurrentEmulatorRunId = runId;
            AssetSnapshotTools.Reset();
            SetBalance("USDT", 500m);
            AssetSnapshotTools.Capture(GlobalData.ActiveExchange!, Day);

            Assert.AreEqual(10000m, LoadTestDays().Single().Value);
            Assert.AreEqual(500m, AssetSnapshotTools.LoadDailyTotals(runId).Single().Value);
        }
        finally
        {
            GlobalData.CurrentEmulatorRunId = null;
        }
    }
}
