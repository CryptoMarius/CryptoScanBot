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
    /// Spot short (winning): sell 0.5 TEST @ 200 USDT (entry), buy back 0.5 TEST @ 180 USDT (TP).
    /// Quote (USDT) is locked as collateral on both sides; base (TEST) is not modified.
    /// Expected profit = 10 USDT — final USDT balance must be 1010.
    /// </summary>
    [TestMethod()]
    public void ShortSpotTest()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        // Arrange
        DeleteAllPositionRelatedStuff(database);
        CryptoSymbol symbol = CreateTestSymbol(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = 1000, Free = 1000, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);

        const decimal entryPrice = 200m;
        const decimal entryQty = 0.5m;
        const decimal tpPrice = 180m;
        const decimal tpQty = 0.5m;

        // Act — entry order placed (New → lock USDT as collateral)
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Sell,
            CryptoOrderStatus.New, entryQty, entryPrice * entryQty, "spot-short-entry-new");

        Assert.AreEqual(1000m, assetQuote.Total, "USDT total unchanged on New");
        Assert.AreEqual(100m, assetQuote.Locked, "USDT locked = entry collateral (100)");
        Assert.AreEqual(900m, assetQuote.Free, "USDT free = 1000 - 100");

        // Act — entry filled: collateral released, sale proceeds received
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Sell,
            CryptoOrderStatus.Filled, entryQty, entryPrice * entryQty, "spot-short-entry-filled");

        Assert.AreEqual(1100m, assetQuote.Total, "USDT = 1000 + 100 sale proceeds");
        Assert.AreEqual(0m, assetQuote.Locked, "Lock released on fill");

        // Act — TP order placed (New → lock USDT to cover buyback)
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Buy,
            CryptoOrderStatus.New, tpQty, tpPrice * tpQty, "spot-short-tp-new");

        Assert.AreEqual(1100m, assetQuote.Total, "USDT total unchanged on New");
        Assert.AreEqual(90m, assetQuote.Locked, "USDT locked = TP buyback cost (90)");
        Assert.AreEqual(1010m, assetQuote.Free, "USDT free = 1100 - 90");

        // Act — TP filled: lock released, buyback cost paid
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, tpQty, tpPrice * tpQty, "spot-short-tp-filled");

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
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        // Arrange
        DeleteAllPositionRelatedStuff(database);
        CryptoSymbol symbol = CreateTestSymbol(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = 2000, Free = 2000, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);

        // Act — entry 1 fill (0.1 contract @ 1000, +100 USDT proceeds)
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Sell,
            CryptoOrderStatus.Filled, 0.1m, 100m, "futures-short-entry1-filled");

        Assert.AreEqual(2100m, assetQuote.Total, "USDT +100 from entry1 proceeds");

        // Act — DCA entry 2 fill (0.1 contract @ 900, +90 USDT proceeds)
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Sell,
            CryptoOrderStatus.Filled, 0.1m, 90m, "futures-short-entry2-filled");

        Assert.AreEqual(2190m, assetQuote.Total, "USDT +90 from entry2 proceeds");

        // Act — TP fill: buy back 0.2 contracts @ 1000 = 200 USDT (losing trade)
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Short, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, 0.2m, 200m, "futures-short-tp-filled");

        Assert.AreEqual(1990m, assetQuote.Total, "USDT = 2000 - loss(10): avg entry 950, closed at 1000");
        Assert.AreEqual(0m, assetQuote.Locked, "No open orders, nothing locked");
        Assert.AreEqual(1990m, assetQuote.Free, "Free = Total when nothing is locked");
    }

    [TestMethod()]
    public void ChangeTest()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();

        // arrange
        DateTime startTime = DateTime.UtcNow.AddHours(-48);

        GlobalData.Settings.Trading.GlobalBuyCooldownTime = 10;
        GlobalData.Settings.Trading.TakeProfitStrategy = CryptoTakeProfitStrategy.FixedPercentage;
        GlobalData.Settings.Trading.TpList = [new CryptoTpEntry { Percentage = 1m, Factor = 100m }];

        CryptoSymbol symbol = CreateTestSymbol(database);

        DeleteAllPositionRelatedStuff(database);

        // Quote asset (USDT)
        CryptoAsset assetQuote = new()
        {
            Name = symbol.Quote,
            Total = 1000,
            Free = 1000,
            Locked = 0,
        };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);

        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb",
           CryptoTradeSide.Long, "Test", symbol.Data.SymbolIntervalList[0], startTime);

        // act
        TradeParams tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy, CryptoOrderType.Market, 5.6261m, 0.53m);
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, tradeParams.OrderSide,
            CryptoOrderStatus.New, tradeParams.Quantity, tradeParams.QuoteQuantity, "test1.1");
        Assert.AreEqual(1000m, assetQuote.Total);
        Assert.AreEqual(2.981833m, assetQuote.Locked);
        Assert.AreEqual(997.018167m, assetQuote.Free);

        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, tradeParams.OrderSide,
            CryptoOrderStatus.Filled, tradeParams.Quantity, tradeParams.QuoteQuantity, "test1.2");
        Assert.AreEqual(1000m - 2.981833m, assetQuote.Total);
        Assert.AreEqual(0, assetQuote.Locked);
        Assert.AreEqual(997.018167m, assetQuote.Free);
    }

}