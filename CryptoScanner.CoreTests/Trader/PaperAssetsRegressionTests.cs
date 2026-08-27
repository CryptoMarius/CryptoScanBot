using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// Regression tests for the four defects that made asset management unusable. Each one reproduces
/// the situation that used to go wrong and asserts the corrected outcome.
/// </summary>
[TestClass]
public class PaperAssetsRegressionTests : TestBase
{
    private static (CryptoDatabase database, CryptoSymbol symbol, CryptoAsset assetQuote) Arrange(decimal startCapital)
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);

        // The live scanner and the emulator each run one exchange at a time, so ActiveExchange IS the
        // exchange the symbols belong to. Line them up here as well: the position administration is
        // kept per exchange, and PaperAssets reads the open orders from the exchange it is handed.
        GlobalData.ActiveExchange = symbol.Exchange;
        DeleteAllPositionRelatedStuff(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = startCapital, Free = startCapital, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);
        database.Connection.Insert(assetQuote);
        return (database, symbol, assetQuote);
    }

    private static CryptoPosition CreateOpenPosition(CryptoDatabase database, CryptoSymbol symbol, CryptoTradeSide side, DateTime startTime)
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
            step.Status, tradeParams.Quantity, tradeParams.QuoteQuantity, "order-new");
        return step;
    }

    /// <summary>
    /// A market entry is placed at Symbol.LastPrice but filled at the candle close, so the amount
    /// released on the fill differs from the amount locked at placement. With the old +/- delta
    /// bookkeeping the difference stayed locked forever and the free balance kept shrinking.
    /// Locked is derived from the open steps now, so a fill leaves nothing behind.
    /// </summary>
    [TestMethod]
    public void LockedDoesNotDriftWhenFillPriceDiffersFromOrderPrice()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        // Order placed: 1 TEST @ 100 -> reserve 100 USDT
        var step = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);
        Assert.AreEqual(100m, assetQuote.Locked, "the open order reserves its order value");

        // Filled at 99 (the market slipped)
        step.Status = CryptoOrderStatus.Filled;
        step.QuantityFilled = 1m;
        step.QuoteQuantityFilled = 99m;
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, 1m, 99m, "entry-filled");

        Assert.AreEqual(0m, assetQuote.Locked, "nothing is on the book anymore, so nothing stays reserved");
        Assert.AreEqual(901m, assetQuote.Total, "1000 - 99 actually paid");
        Assert.AreEqual(901m, assetQuote.Free, "Free = Total when nothing is locked");
    }

    /// <summary>
    /// A cancelled order releases its reservation in full, even when the step was repriced between
    /// placing and cancelling.
    /// </summary>
    [TestMethod]
    public void CancelledOrderReleasesItsFullReservation()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        var step = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);
        Assert.AreEqual(100m, assetQuote.Locked);

        // The trader repriced the step before cancelling it
        step.Price = 97m;
        step.Status = CryptoOrderStatus.Canceled;
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Canceled, 1m, 97m, "entry-cancelled");

        Assert.AreEqual(0m, assetQuote.Locked, "a cancelled order reserves nothing");
        Assert.AreEqual(1000m, assetQuote.Total, "a cancel does not move money");
        Assert.AreEqual(1000m, assetQuote.Free);
    }

    /// <summary>
    /// The commission used to be booked by calling Change() with negative quantities, which flipped
    /// its sign for a futures long entry: the fee was ADDED instead of subtracted, and it counted as
    /// a reservation on top of that. BookCommission always subtracts.
    /// </summary>
    [TestMethod]
    public void CommissionIsSubtractedOnEverySide()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);

        // Long entry, commission in quote (linear futures)
        PaperAssets.BookCommission(GlobalData.ActiveExchange!, symbol, 0m, 0.1m, "long-entry-fee");
        Assert.AreEqual(999.9m, assetQuote.Total, "a fee is a cost on a long entry");
        Assert.AreEqual(0m, assetQuote.Locked, "a fee is not a reservation");

        // Short take profit, commission in quote
        PaperAssets.BookCommission(GlobalData.ActiveExchange!, symbol, 0m, 0.09m, "short-tp-fee");
        Assert.AreEqual(999.81m, assetQuote.Total, "a fee is a cost on a short take profit too");
        Assert.AreEqual(0m, assetQuote.Locked);
    }

    /// <summary>
    /// A spot commission charged in the base coin comes off the base coin.
    /// </summary>
    [TestMethod]
    public void BaseCommissionIsSubtractedFromTheBaseAsset()
    {
        var (database, symbol, _) = Arrange(1000m);

        CryptoAsset assetBase = PaperAssets.FindOrCreateAsset(GlobalData.ActiveExchange!, symbol.Base);
        assetBase.Total = 2m;

        PaperAssets.BookCommission(GlobalData.ActiveExchange!, symbol, 0.002m, 0m, "spot-entry-fee");

        Assert.AreEqual(1.998m, assetBase.Total, "the base fee comes off the base coin");
    }

    /// <summary>
    /// GetAsset used to hand out a hardcoded 1,000,000 for paper trading, so everything PaperAssets
    /// maintained was ignored by the trader. It reports the real balance now.
    /// </summary>
    [TestMethod]
    public void GetAssetReportsTheMaintainedBalance()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);

        var info = AssetTools.GetAsset(GlobalData.ActiveExchange!, symbol);

        Assert.AreEqual(1000m, info.QuoteTotal, "the real balance, not a made up one");
        Assert.AreEqual(900m, info.QuoteFree, "free = total minus what the open order reserves");
    }

    /// <summary>
    /// With a real balance in place, an entry that costs more than the free balance is refused
    /// instead of silently going through on fake money.
    /// </summary>
    [TestMethod]
    public void CheckAvailableAssetsRefusesAnEntryThatDoesNotFit()
    {
        var (database, symbol, assetQuote) = Arrange(50m);
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsFalse(result.success, "100 USDT entry on a 50 USDT balance must be refused");
        StringAssert.Contains(result.reaction, "Not enough cash available");
    }

    /// <summary>
    /// A complete futures long trade including both commissions has to land on the exact amount of
    /// money: buy 1 @ 100, sell 1 @ 110, 0.1% fee on both legs.
    /// 1000 - 100 + 110 - 0.10 - 0.11 = 1009.79
    /// </summary>
    [TestMethod]
    public void AFullRoundTripAddsUpToTheCent()
    {
        var (database, symbol, assetQuote) = Arrange(1000m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        // Entry: buy 1 @ 100
        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);
        entry.Status = CryptoOrderStatus.Filled;
        entry.QuantityFilled = 1m;
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, 1m, 100m, "entry-filled");
        PaperAssets.BookCommission(GlobalData.ActiveExchange!, symbol, 0m, 0.10m, "entry-fee");

        // Take profit: sell 1 @ 110
        var tp = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Sell, 110m, 1m, startTime);
        tp.Status = CryptoOrderStatus.Filled;
        tp.QuantityFilled = 1m;
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Sell,
            CryptoOrderStatus.Filled, 1m, 110m, "tp-filled");
        PaperAssets.BookCommission(GlobalData.ActiveExchange!, symbol, 0m, 0.11m, "tp-fee");

        Assert.AreEqual(1009.79m, assetQuote.Total, "1000 - 100 + 110 - 0.10 fee - 0.11 fee");
        Assert.AreEqual(0m, assetQuote.Locked, "everything is closed, nothing reserved");
        Assert.AreEqual(1009.79m, assetQuote.Free);
    }

    /// <summary>
    /// Reset wipes the balances and hands out the start capital again for every traded quote coin -
    /// what the emulator does at the start of a run.
    /// </summary>
    [TestMethod]
    public void ResetHandsOutTheStartCapitalAgain()
    {
        var (database, symbol, assetQuote) = Arrange(37m);

        // Make sure the symbol's quote coin counts as traded, otherwise there is nothing to seed
        symbol.QuoteData!.FetchCandles = true;
        GlobalData.Settings.QuoteCoins[symbol.Quote] = symbol.QuoteData;

        PaperAssets.ResetAssets(GlobalData.ActiveExchange!, 5000m);

        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(symbol.Quote, out var seeded),
            "the traded quote coin is seeded again");
        Assert.AreEqual(5000m, seeded!.Total, "with the requested start capital");
        Assert.AreEqual(0m, seeded.Locked, "and nothing reserved");
    }

    /// <summary>
    /// An asset that still has an open order reserving it must not be dropped when its total hits
    /// zero - that used to throw the reservation away.
    /// </summary>
    [TestMethod]
    public void AssetWithAnOpenOrderIsKeptWhenTheBalanceHitsZero()
    {
        var (database, symbol, assetQuote) = Arrange(100m);
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, startTime);

        // The whole balance is on the book
        PlaceOrder(database, position, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, startTime);
        Assert.AreEqual(100m, assetQuote.Locked);

        // ...and it is spent completely
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, 1m, 100m, "entry-filled");

        Assert.AreEqual(0m, assetQuote.Total, "all the cash went into the position");
        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.ContainsKey(symbol.Base),
            "the base coin we bought must be on the books");
    }
}
