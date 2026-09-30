using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// How much money a position takes out of the FREE balance, for a long and a short alike.
/// <para>
/// The older asset tests all check the TOTAL: profit is the change in the balance. That invariant
/// held for shorts all along, and so did the tests - while a filled short quietly ADDED to the free
/// balance, because its sale proceeds were booked and only the buy-back order was reserved, at its
/// take profit price. Emulator run 1765 (30-09-2026, entry 800, a DCA of 1600, start capital 10.000)
/// held 12.800 USDT of open shorts at once that way. Nothing checked what the free balance does,
/// and the free balance is the one thing that decides whether the next entry is allowed.
/// </para>
/// <para>
/// So these tests run the same order sequence on both sides and demand the same free balance after
/// every step. A long and a short of the same size tie up the same money; any difference is a leak.
/// </para>
/// </summary>
[TestClass]
public class PaperAssetsReservationTests : TestBase
{
    private const decimal Start = 10000m;

    private bool _savedUseAssetManagement;
    private List<CryptoDcaEntry> _savedDcaList = [];
    private CryptoTradeVia _savedTradeVia;

    [TestInitialize]
    public void SaveSettings()
    {
        _savedUseAssetManagement = GlobalData.Settings.Trading.UseAssetManagement;
        _savedDcaList = GlobalData.Settings.Trading.DcaList;
        _savedTradeVia = GlobalData.Settings.Trading.TradeVia;

        GlobalData.Settings.Trading.UseAssetManagement = true;
        GlobalData.Settings.Trading.DcaList = [];
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.UseAssetManagement = _savedUseAssetManagement;
        GlobalData.Settings.Trading.DcaList = _savedDcaList;
        GlobalData.Settings.Trading.TradeVia = _savedTradeVia;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private static (CryptoDatabase database, CryptoSymbol symbol, CryptoAsset assetQuote) Arrange(decimal startCapital)
    {
        InitTestSession();

        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = startCapital, Free = startCapital, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);

        // Assert on whatever is IN the list (see PaperAssetsBasicsTests.Arrange for why)
        assetQuote = GlobalData.ActiveExchange!.Data.AssetList[assetQuote.Name];
        assetQuote.Total = startCapital;
        assetQuote.Free = startCapital;
        assetQuote.Locked = 0;

        database.Connection.Insert(assetQuote);
        return (database, symbol, assetQuote);
    }

    /// <summary>A second symbol on the same quote coin, for two positions open at the same time.</summary>
    private static CryptoSymbol CreateSecondSymbol(CryptoDatabase database, CryptoSymbol first)
    {
        if (first.Exchange.SymbolListName.TryGetValue("TEST2USDT", out CryptoSymbol? symbol))
            return symbol;

        symbol = new()
        {
            Status = 1,
            Base = "TEST2",
            Quote = first.Quote,
            Name = "TEST2USDT",
            Exchange = first.Exchange,
            ExchangeId = first.ExchangeId,
            QuoteData = first.QuoteData,
            ExchangeName = "TEST2 exchange",
            QuantityTickSize = first.QuantityTickSize,
            QuantityMinimum = first.QuantityMinimum,
            QuantityMaximum = first.QuantityMaximum,
            PriceTickSize = first.PriceTickSize,
            QuoteValueMinimum = first.QuoteValueMinimum,
            QuoteValueMaximum = first.QuoteValueMaximum,
        };
        // Insert first so the symbol has its Id before it is indexed (see TestBase.CreateTestSymbol)
        database.Connection.Insert(symbol);
        GlobalData.AddSymbol(symbol);
        return symbol;
    }

    private static CryptoPosition CreateOpenPosition(CryptoDatabase database, CryptoSymbol symbol,
        CryptoTradeSide side, DateTime startTime)
    {
        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb", side, "Test",
            symbol.Data.SymbolIntervalList[0], startTime);
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);
        return position;
    }

    private static CryptoOrderSide EntrySide(CryptoTradeSide side)
        => side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell;

    private static CryptoOrderSide ExitSide(CryptoTradeSide side)
        => side == CryptoTradeSide.Long ? CryptoOrderSide.Sell : CryptoOrderSide.Buy;

    private static CryptoPositionStep PlaceOrder(CryptoDatabase database, CryptoPosition position,
        CryptoPartPurpose purpose, CryptoOrderSide side, decimal price, decimal quantity, DateTime createTime)
    {
        CryptoPositionPart part = PositionTools.ExtendPosition(database, position, purpose,
            position.Symbol.Data.SymbolIntervalList[0].Interval, "Test", price, createTime);

        TradeParams tradeParams = CreateTradeParams(database, createTime, side, CryptoOrderType.Limit, price, quantity);
        CryptoPositionStep step = PositionTools.CreatePositionStep(position, part, tradeParams);
        database.Connection.Insert<CryptoPositionStep>(step);
        PositionTools.AddPositionPartStep(part, step);

        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, side,
            step.Status, tradeParams.Quantity, tradeParams.QuoteQuantity, $"{purpose}-{side}-new");
        return step;
    }

    private static void FillOrder(CryptoPosition position, CryptoPositionStep step, decimal fillPrice)
    {
        step.Status = CryptoOrderStatus.Filled;
        step.QuantityFilled = step.Quantity;
        step.QuoteQuantityFilled = step.Quantity * fillPrice;

        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, step.Side,
            CryptoOrderStatus.Filled, step.Quantity, step.QuoteQuantityFilled, "filled");
    }

    private static void CancelOrder(CryptoPosition position, CryptoPositionStep step)
    {
        step.Status = CryptoOrderStatus.Canceled;
        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, step.Side,
            CryptoOrderStatus.Canceled, step.Quantity, step.Quantity * step.Price, "canceled");
    }


    /// <summary>
    /// One full life of a position: entry, a DCA order, a take profit, the DCA fills, the take profit
    /// is replaced by two orders for the larger position, and those close it in two halves. Every
    /// price is 100, so the position ends exactly where it started and both sides should read the
    /// same free balance after every step. Returns that balance per step.
    /// </summary>
    private static List<(string step, decimal free)> RunLifecycle(CryptoTradeSide side)
    {
        var (database, symbol, assetQuote) = Arrange(Start);
        DateTime t = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, side, t);
        List<(string, decimal)> free = [];

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, EntrySide(side), 100m, 10m, t);
        free.Add(("entry on the book", assetQuote.Free));

        FillOrder(position, entry, 100m);
        free.Add(("entry filled", assetQuote.Free));

        var dca = PlaceOrder(database, position, CryptoPartPurpose.Dca, EntrySide(side), 100m, 20m, t);
        free.Add(("dca on the book", assetQuote.Free));

        var takeProfit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, ExitSide(side), 100m, 10m, t);
        free.Add(("take profit on the book", assetQuote.Free));

        FillOrder(position, dca, 100m);
        free.Add(("dca filled", assetQuote.Free));

        CancelOrder(position, takeProfit);
        free.Add(("take profit cancelled", assetQuote.Free));

        var firstHalf = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, ExitSide(side), 100m, 15m, t);
        var secondHalf = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, ExitSide(side), 100m, 15m, t);
        free.Add(("two take profits on the book", assetQuote.Free));

        FillOrder(position, firstHalf, 100m);
        free.Add(("first half closed", assetQuote.Free));

        FillOrder(position, secondHalf, 100m);
        free.Add(("second half closed", assetQuote.Free));

        return free;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Long and short tie up the same money
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The expected free balance per step, written out so a failure says WHICH step leaks, and then
    /// the one thing that matters most: the short reads exactly what the long reads.
    /// </summary>
    [TestMethod]
    public void Lifecycle_LongAndShortLeaveTheSameFreeBalanceAfterEveryStep()
    {
        List<(string step, decimal free)> expected =
        [
            ("entry on the book", 9000m),            // 10 x 100 reserved
            ("entry filled", 9000m),                 // now in the position instead of on the book
            ("dca on the book", 7000m),              // 20 x 100 more reserved
            ("take profit on the book", 7000m),      // an exit order costs no quote of its own
            ("dca filled", 7000m),
            ("take profit cancelled", 7000m),
            ("two take profits on the book", 7000m),
            ("first half closed", 8500m),            // 15 of the 30 back at 100
            ("second half closed", Start),           // flat, nothing left reserved
        ];

        List<(string step, decimal free)> longFree = RunLifecycle(CryptoTradeSide.Long);
        List<(string step, decimal free)> shortFree = RunLifecycle(CryptoTradeSide.Short);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(expected[i].free, longFree[i].free, $"long, {expected[i].step}");
            Assert.AreEqual(expected[i].free, shortFree[i].free, $"short, {expected[i].step}");
        }
    }


    /// <summary>
    /// The guard for the leak itself, on the numbers of run 1765 (ONDO, short): 800 sold at 0.3484,
    /// 1600 more at 0.3546, the buy-back at 0.2837. The old reading reserved only that buy-back, so
    /// this position left 469 MORE free money than there was before it opened.
    /// </summary>
    [TestMethod]
    public void AFilledShort_NeverAddsFreeMoney()
    {
        var (database, symbol, assetQuote) = Arrange(Start);
        DateTime t = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Short, t);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Sell, 0.3484m, 2296.2m, t);
        FillOrder(position, entry, 0.3484m);
        var dca = PlaceOrder(database, position, CryptoPartPurpose.Dca, CryptoOrderSide.Sell, 0.3546m, 4512.3m, t);
        FillOrder(position, dca, 0.3546m);
        PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Buy, 0.2837m, 6808.5m, t);

        decimal committed = entry.QuoteQuantityFilled + dca.QuoteQuantityFilled;
        Assert.AreEqual(Start - committed, assetQuote.Free,
            "the whole 2400 is in the position, exactly as it would be for a long");
        Assert.IsTrue(assetQuote.Free < Start, "a short may never leave more free money than before it opened");
    }


    /// <summary>
    /// A DCA order that is cancelled gives its reservation back, on both sides.
    /// </summary>
    [TestMethod]
    [DataRow(CryptoTradeSide.Long, DisplayName = "long")]
    [DataRow(CryptoTradeSide.Short, DisplayName = "short")]
    public void ACancelledDca_ReleasesItsReservation(CryptoTradeSide side)
    {
        var (database, symbol, assetQuote) = Arrange(Start);
        DateTime t = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, side, t);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, EntrySide(side), 100m, 10m, t);
        FillOrder(position, entry, 100m);
        var dca = PlaceOrder(database, position, CryptoPartPurpose.Dca, EntrySide(side), 100m, 20m, t);
        Assert.AreEqual(7000m, assetQuote.Free);

        CancelOrder(position, dca);
        Assert.AreEqual(9000m, assetQuote.Free, "back to only the entry");
    }


    /// <summary>
    /// A winner and a loser of the same size, both sides. While open the free balance holds the
    /// entry value; once closed it holds the start plus the result, and nothing stays reserved.
    /// </summary>
    [TestMethod]
    [DataRow(CryptoTradeSide.Long, 110.0, 10100.0, DisplayName = "long die wint")]
    [DataRow(CryptoTradeSide.Long, 90.0, 9900.0, DisplayName = "long that loses")]
    [DataRow(CryptoTradeSide.Short, 90.0, 10100.0, DisplayName = "short die wint")]
    [DataRow(CryptoTradeSide.Short, 110.0, 9900.0, DisplayName = "short that loses")]
    public void TheResultLandsInTheFreeBalance(CryptoTradeSide side, double exitPrice, double expectedFree)
    {
        var (database, symbol, assetQuote) = Arrange(Start);
        DateTime t = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, side, t);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, EntrySide(side), 100m, 10m, t);
        FillOrder(position, entry, 100m);
        var exit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, ExitSide(side), (decimal)exitPrice, 10m, t);
        Assert.AreEqual(9000m, assetQuote.Free, "while open, the entry value is out of the free balance");

        FillOrder(position, exit, (decimal)exitPrice);
        Assert.AreEqual((decimal)expectedFree, assetQuote.Free);
        Assert.AreEqual(0m, assetQuote.Locked, "nothing reserved once closed");
    }


    /// <summary>
    /// A long and a short open at the same time on two symbols of the same quote coin: the two
    /// reservations add up, and closing one releases only its own.
    /// </summary>
    [TestMethod]
    public void ALongAndAShortTogether_AddUp()
    {
        var (database, symbol, assetQuote) = Arrange(Start);
        CryptoSymbol second = CreateSecondSymbol(database, symbol);
        DateTime t = DateTime.UtcNow.AddHours(-48);

        CryptoPosition longPosition = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, t);
        CryptoPosition shortPosition = CreateOpenPosition(database, second, CryptoTradeSide.Short, t);

        var longEntry = PlaceOrder(database, longPosition, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 10m, t);
        FillOrder(longPosition, longEntry, 100m);
        var shortEntry = PlaceOrder(database, shortPosition, CryptoPartPurpose.Entry, CryptoOrderSide.Sell, 50m, 40m, t);
        FillOrder(shortPosition, shortEntry, 50m);
        Assert.AreEqual(Start - 1000m - 2000m, assetQuote.Free, "1000 in the long, 2000 in the short");

        var shortExit = PlaceOrder(database, shortPosition, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Buy, 50m, 40m, t);
        FillOrder(shortPosition, shortExit, 50m);
        Assert.AreEqual(Start - 1000m, assetQuote.Free, "the short is gone, the long still holds its 1000");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  What the trader asks: is there room for the next entry?
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The question the trader asks before every entry, answered after a filled position with its
    /// DCA on the book. With 3000 start capital, an entry of 1000 and a DCA of 200% (2000) there is
    /// nothing left for a second one; with 6000 there is exactly enough. Same answer for both sides.
    /// </summary>
    [TestMethod]
    [DataRow(CryptoTradeSide.Long, 3000.0, false, DisplayName = "long, no room")]
    [DataRow(CryptoTradeSide.Short, 3000.0, false, DisplayName = "short, no room")]
    [DataRow(CryptoTradeSide.Long, 6000.0, true, DisplayName = "long, room for one more")]
    [DataRow(CryptoTradeSide.Short, 6000.0, true, DisplayName = "short, room for one more")]
    public void TheNextEntry_IsRefusedOrAllowedTheSameWayForBothSides(CryptoTradeSide side, double startCapital, bool expectedRoom)
    {
        var (database, symbol, _) = Arrange((decimal)startCapital);
        GlobalData.Settings.Trading.DcaList = [new CryptoDcaEntry { Percentage = 2m, Factor = 200m }];
        DateTime t = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, side, t);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, EntrySide(side), 100m, 10m, t);
        FillOrder(position, entry, 100m);
        PlaceOrder(database, position, CryptoPartPurpose.Dca, EntrySide(side), 100m, 20m, t);

        bool room = AssetTools.CheckAssetsCoverEntryAndDca(GlobalData.ActiveExchange!, symbol, 1000m, out string reason);
        Assert.AreEqual(expectedRoom, room, reason);
    }
}
