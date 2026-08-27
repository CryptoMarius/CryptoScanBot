using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

[TestClass]
public class PaperAssetsTests : TestBase
{
    /// <summary>
    /// Place an order the way the trader does: create the part and the step, register them, and only
    /// then tell PaperAssets about it. The registration matters — since the fix, Locked is derived
    /// from the steps that are open, not from the +/- deltas of the Change() calls.
    /// </summary>
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

    /// <summary>Fill an open step, the way TradeTools.CalculatePositionResultsViaOrders does.</summary>
    private static void FillOrder(CryptoPosition position, CryptoPositionStep step, decimal fillPrice)
    {
        step.Status = CryptoOrderStatus.Filled;
        step.QuantityFilled = step.Quantity;
        step.QuoteQuantityFilled = step.Quantity * fillPrice;

        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, step.Side,
            CryptoOrderStatus.Filled, step.Quantity, step.QuoteQuantityFilled, "filled");
    }

    private static (CryptoDatabase database, CryptoSymbol symbol, CryptoAsset assetQuote) Arrange(decimal startCapital)
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);

        // No need to line ActiveExchange up with the symbol's exchange here: TestBase.SetupOnce puts
        // the session on the exchange the test symbols belong to, the same way the scanner runs one
        // exchange at a time.
        DeleteAllPositionRelatedStuff(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = startCapital, Free = startCapital, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);
        return (database, symbol, assetQuote);
    }

    /// <summary>
    /// Spot short (winning): sell 0.5 TEST @ 200 USDT (entry), buy back 0.5 TEST @ 180 USDT (TP).
    /// Quote (USDT) is locked as collateral on both sides; base (TEST) is not modified.
    /// Expected profit = 10 USDT — final USDT balance must be 1010.
    /// </summary>
    [TestMethod()]
    public void ShortSpotTest()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb",
            CryptoTradeSide.Short, "Test", symbol.Data.SymbolIntervalList[0], startTime);
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);

        const decimal entryPrice = 200m;
        const decimal entryQty = 0.5m;
        const decimal tpPrice = 180m;

        // Act — entry order placed (on the book → lock USDT as collateral)
        var entryStep = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Sell, entryPrice, entryQty, startTime);

        Assert.AreEqual(1000m, assetQuote.Total, "USDT total unchanged while the order is only placed");
        Assert.AreEqual(100m, assetQuote.Locked, "USDT locked = entry collateral (100)");
        Assert.AreEqual(900m, assetQuote.Free, "USDT free = 1000 - 100");

        // Act — entry filled: collateral released, sale proceeds received
        FillOrder(position, entryStep, entryPrice);

        Assert.AreEqual(1100m, assetQuote.Total, "USDT = 1000 + 100 sale proceeds");
        Assert.AreEqual(0m, assetQuote.Locked, "Lock released on fill");

        // Act — TP order placed (on the book → lock USDT to cover the buyback)
        var tpStep = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Buy, tpPrice, entryQty, startTime);

        Assert.AreEqual(1100m, assetQuote.Total, "USDT total unchanged while the order is only placed");
        Assert.AreEqual(90m, assetQuote.Locked, "USDT locked = TP buyback cost (90)");
        Assert.AreEqual(1010m, assetQuote.Free, "USDT free = 1100 - 90");

        // Act — TP filled: lock released, buyback cost paid
        FillOrder(position, tpStep, tpPrice);

        Assert.AreEqual(1010m, assetQuote.Total, "USDT = 1000 + profit(10): shorted at 200, closed at 180");
        Assert.AreEqual(0m, assetQuote.Locked, "No open orders, nothing locked");
        Assert.AreEqual(1010m, assetQuote.Free, "Free = Total when nothing is locked");
    }

    /// <summary>
    /// Futures short with two DCA entries (losing trade).
    /// Entry1: 0.1 contract @ 1000 USDT (+100), Entry2 DCA: 0.1 @ 900 (+90).
    /// TP: buy back 0.2 contracts @ 1000 USDT (-200).
    /// Expected loss = 10 USDT — final USDT balance must be 1990.
    /// </summary>
    [TestMethod()]
    public void ShortFuturesTest()
    {
        var (database, symbol, assetQuote) = Arrange(2000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb",
            CryptoTradeSide.Short, "Test", symbol.Data.SymbolIntervalList[0], startTime);
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);

        // Act — entry 1 fill (0.1 contract @ 1000, +100 USDT proceeds)
        var entry1 = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Sell, 1000m, 0.1m, startTime);
        FillOrder(position, entry1, 1000m);

        Assert.AreEqual(2100m, assetQuote.Total, "USDT +100 from entry1 proceeds");

        // Act — DCA entry 2 fill (0.1 contract @ 900, +90 USDT proceeds)
        var entry2 = PlaceOrder(database, position, CryptoPartPurpose.Dca, CryptoOrderSide.Sell, 900m, 0.1m, startTime);
        FillOrder(position, entry2, 900m);

        Assert.AreEqual(2190m, assetQuote.Total, "USDT +90 from entry2 proceeds");

        // Act — TP fill: buy back 0.2 contracts @ 1000 = 200 USDT (losing trade)
        var tpStep = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Buy, 1000m, 0.2m, startTime);
        FillOrder(position, tpStep, 1000m);

        Assert.AreEqual(1990m, assetQuote.Total, "USDT = 2000 - loss(10): avg entry 950, closed at 1000");
        Assert.AreEqual(0m, assetQuote.Locked, "No open orders, nothing locked");
        Assert.AreEqual(1990m, assetQuote.Free, "Free = Total when nothing is locked");
    }

    [TestMethod()]
    public void ChangeTest()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        GlobalData.Settings.Trading.GlobalBuyCooldownTime = 10;
        GlobalData.Settings.Trading.TakeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage;
        GlobalData.Settings.Trading.TpList = [new CryptoTpEntry { Percentage = 1m, Factor = 100m }];

        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb",
           CryptoTradeSide.Long, "Test", symbol.Data.SymbolIntervalList[0], startTime);
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);

        // act — a long entry buy reserves quote at the order price
        var step = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 5.6261m, 0.53m, startTime);
        Assert.AreEqual(1000m, assetQuote.Total);
        Assert.AreEqual(2.981833m, assetQuote.Locked);
        Assert.AreEqual(997.018167m, assetQuote.Free);

        FillOrder(position, step, 5.6261m);
        Assert.AreEqual(1000m - 2.981833m, assetQuote.Total);
        Assert.AreEqual(0, assetQuote.Locked);
        Assert.AreEqual(997.018167m, assetQuote.Free);
    }

}
