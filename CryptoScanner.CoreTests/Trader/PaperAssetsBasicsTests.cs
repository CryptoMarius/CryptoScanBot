using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The plain cases of asset bookkeeping, both directions, winning and losing.
/// <para>
/// The tests that existed before this file each pinned down one defect, and between them they only
/// ever closed a long in profit and a short both ways. A long that CLOSES BELOW ITS ENTRY was not
/// covered anywhere, and neither was a long that averaged down first - which is most of what the
/// emulator actually does.
/// </para>
/// <para>
/// The invariant at the bottom is the one worth having: profit is the change in the balance, and
/// the money flows the other way round for a short. Checked against 80.562 closed positions from
/// emulator runs 398-420 on 27-08-2026, where it held to the last decimal on both sides - these
/// tests keep it that way.
/// </para>
/// </summary>
[TestClass]
public class PaperAssetsBasicsTests : TestBase
{
    private static (CryptoDatabase database, CryptoSymbol symbol, CryptoAsset assetQuote) Arrange(decimal startCapital)
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = startCapital, Free = startCapital, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);

        // Assert on whatever is IN the list, not on the object we just built: TryAdd keeps the
        // existing entry when a coin is already there, and then PaperAssets would be updating one
        // object while the test watches another. Only shows up once another test leaves an asset
        // behind, so it passes in isolation and fails in the full suite.
        assetQuote = GlobalData.ActiveExchange!.Data.AssetList[assetQuote.Name];
        assetQuote.Total = startCapital;
        assetQuote.Free = startCapital;
        assetQuote.Locked = 0;

        database.Connection.Insert(assetQuote);
        return (database, symbol, assetQuote);
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

    /// <summary>Fill an open step at the given price, the way the trader reports a fill.</summary>
    private static void FillOrder(CryptoPosition position, CryptoPositionStep step, decimal fillPrice)
    {
        step.Status = CryptoOrderStatus.Filled;
        step.QuantityFilled = step.Quantity;
        step.QuoteQuantityFilled = step.Quantity * fillPrice;

        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, step.Side,
            CryptoOrderStatus.Filled, step.Quantity, step.QuoteQuantityFilled, "filled");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Long, the direction that was only ever tested winning
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Long that loses: buy 1 @ 100, sell 1 @ 90. The balance has to end 10 lower, and nothing may
    /// stay reserved once both orders are gone.
    /// </summary>
    [TestMethod]
    public void LongThatClosesBelowItsEntry_TakesTheLossFromTheBalance()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);
        Assert.AreEqual(100m, assetQuote.Locked, "the entry buy reserves its full cost");
        Assert.AreEqual(900m, assetQuote.Free);
        FillOrder(position, entry, 100m);

        Assert.AreEqual(900m, assetQuote.Total, "1000 - 100 paid for the entry");
        Assert.AreEqual(0m, assetQuote.Locked, "the reservation is released on the fill");

        var exit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Sell, 90m, 1m, startTime);
        FillOrder(position, exit, 90m);

        Assert.AreEqual(990m, assetQuote.Total, "1000 - 100 + 90: a loss of 10");
        Assert.AreEqual(0m, assetQuote.Locked, "alles gesloten, niets gereserveerd");
        Assert.AreEqual(990m, assetQuote.Free, "free equals total when nothing is locked");
    }


    /// <summary>
    /// Long that averages down and still loses - the mirror of ShortFuturesTest, which is the only
    /// DCA case that existed. Buy 1 @ 100, buy 1 @ 80 (average 90), sell 2 @ 85.
    /// 1000 - 100 - 80 + 170 = 990, a loss of 10.
    /// </summary>
    [TestMethod]
    public void LongWithADcaFill_AveragesDownAndSettlesOnTheExactAmount()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);
        FillOrder(position, entry, 100m);
        Assert.AreEqual(900m, assetQuote.Total, "eerste inkoop betaald");

        var dca = PlaceOrder(database, position, CryptoPartPurpose.Dca, CryptoOrderSide.Buy, 80m, 1m, startTime);
        Assert.AreEqual(80m, assetQuote.Locked, "the DCA order reserves its own cost");
        FillOrder(position, dca, 80m);
        Assert.AreEqual(820m, assetQuote.Total, "second buy paid");
        Assert.AreEqual(0m, assetQuote.Locked);

        var exit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Sell, 85m, 2m, startTime);
        FillOrder(position, exit, 85m);

        Assert.AreEqual(990m, assetQuote.Total, "gemiddeld ingekocht op 90, gesloten op 85: -10 over 2 stuks");
        Assert.AreEqual(0m, assetQuote.Locked);
        Assert.AreEqual(990m, assetQuote.Free);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Short, the losing side without a DCA in the way
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Short that loses: sell 1 @ 100, buy back 1 @ 110. The existing short tests cover a winning
    /// spot short and a losing short WITH two DCA fills; this is the plain losing one, so a change
    /// in the DCA path cannot hide a change in the short path.
    /// </summary>
    [TestMethod]
    public void ShortThatClosesAboveItsEntry_TakesTheLossFromTheBalance()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Short, startTime);

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Sell, 100m, 1m, startTime);
        Assert.AreEqual(100m, assetQuote.Locked, "the sell reserves collateral while it is on the books");
        FillOrder(position, entry, 100m);

        Assert.AreEqual(1100m, assetQuote.Total, "1000 + 100 proceeds of the sell");
        Assert.AreEqual(0m, assetQuote.Locked, "collateral released on the fill");

        var exit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Buy, 110m, 1m, startTime);
        Assert.AreEqual(110m, assetQuote.Locked, "the buy-back reserves what it is going to cost");
        FillOrder(position, exit, 110m);

        Assert.AreEqual(990m, assetQuote.Total, "1000 + 100 - 110: a loss of 10");
        Assert.AreEqual(0m, assetQuote.Locked);
        Assert.AreEqual(990m, assetQuote.Free);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The invariant the emulator is judged on
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Profit IS the change in the balance, and the money flows the other way round for a short:
    /// a long pays first and receives later, a short receives first and pays later. Reading the
    /// short with the long's formula turns every short result inside out - which is exactly what
    /// made 54% of 80.562 emulator positions look wrong on 27-08-2026 until the side was taken
    /// into account.
    /// <para>
    /// entryPrice/exitPrice are what the trade did; expectedProfit is what the account must show.
    /// </para>
    /// </summary>
    [TestMethod]
    [DataRow(CryptoTradeSide.Long, 100.0, 110.0, +10.0, DisplayName = "long die wint")]
    [DataRow(CryptoTradeSide.Long, 100.0, 90.0, -10.0, DisplayName = "long that loses")]
    [DataRow(CryptoTradeSide.Short, 100.0, 90.0, +10.0, DisplayName = "short die wint")]
    [DataRow(CryptoTradeSide.Short, 100.0, 110.0, -10.0, DisplayName = "short that loses")]
    public void ProfitIsTheChangeInTheBalance(CryptoTradeSide side, double entryPrice,
        double exitPrice, double expectedProfit)
    {
        const decimal start = 1000m;
        var (database, symbol, assetQuote) = Arrange(start);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, side, startTime);

        CryptoOrderSide entrySide = side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell;
        CryptoOrderSide exitSide = side == CryptoTradeSide.Long ? CryptoOrderSide.Sell : CryptoOrderSide.Buy;

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, entrySide, (decimal)entryPrice, 1m, startTime);
        FillOrder(position, entry, (decimal)entryPrice);
        var exit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, exitSide, (decimal)exitPrice, 1m, startTime);
        FillOrder(position, exit, (decimal)exitPrice);

        Assert.AreEqual((decimal)expectedProfit, assetQuote.Total - start,
            "the profit is the change in the balance, whatever the side");
        Assert.AreEqual(0m, assetQuote.Locked, "nothing reserved any more after closing");
        Assert.AreEqual(assetQuote.Total, assetQuote.Free, "free equals total when nothing is locked");
    }


    /// <summary>
    /// An entry that never fills has to leave the balance exactly as it was - not approximately.
    /// This is the case that produced 1.582 of vbs' 2.967 "closed positions" in run 410: limit
    /// entries that timed out. They must cost nothing at all, or every run with limit entries
    /// bleeds quietly.
    /// </summary>
    [TestMethod]
    [DataRow(CryptoTradeSide.Long, DisplayName = "long")]
    [DataRow(CryptoTradeSide.Short, DisplayName = "short")]
    public void AnEntryThatNeverFills_LeavesTheBalanceUntouched(CryptoTradeSide side)
    {
        const decimal start = 1000m;
        var (database, symbol, assetQuote) = Arrange(start);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, side, startTime);

        CryptoOrderSide entrySide = side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell;
        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, entrySide, 100m, 1m, startTime);
        Assert.AreEqual(100m, assetQuote.Locked, "while the order is on the books the money is locked");
        Assert.AreEqual(900m, assetQuote.Free);

        entry.Status = CryptoOrderStatus.Canceled;
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, side, entrySide,
            CryptoOrderStatus.Canceled, entry.Quantity, entry.Quantity * entry.Price, "entry-timeout");

        Assert.AreEqual(start, assetQuote.Total, "an unfilled entry costs nothing");
        Assert.AreEqual(0m, assetQuote.Locked, "the reservation is fully returned");
        Assert.AreEqual(start, assetQuote.Free);
    }
}
