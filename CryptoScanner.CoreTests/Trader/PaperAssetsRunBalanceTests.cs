using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// Does the balance still add up after a whole run, not just after one trade?
/// <para>
/// This cannot be checked against a finished emulator run afterwards: the Asset table holds only
/// the CURRENT balance - no run id, no history - and ResetAssets deletes it at the start of every
/// run. So the question "did run 437 end on the right amount" has no stored answer. What can be
/// pinned down is the invariant itself, by putting a run's worth of traffic through PaperAssets and
/// asserting on the total: many positions, both directions, winners and losers, one that averages
/// down, one entry that never fills, and one still open at the end.
/// </para>
/// <para>
/// The per-position identity is already covered by PaperAssetsBasicsTests. What this adds is that
/// nothing accumulates over a sequence - a rounding error or a reservation that is released twice
/// stays invisible in a single trade and shows up over a few hundred.
/// </para>
/// </summary>
[TestClass]
public class PaperAssetsRunBalanceTests : TestBase
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
            step.Status, tradeParams.Quantity, tradeParams.QuoteQuantity, $"{purpose}-{side}");
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

    /// <summary>One complete trade, entry and exit both filled. Returns what it earned or cost.</summary>
    private static decimal RoundTrip(CryptoDatabase database, CryptoSymbol symbol, CryptoTradeSide side,
        decimal entryPrice, decimal exitPrice, decimal quantity, DateTime when)
    {
        CryptoPosition position = CreateOpenPosition(database, symbol, side, when);
        CryptoOrderSide entrySide = side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell;
        CryptoOrderSide exitSide = side == CryptoTradeSide.Long ? CryptoOrderSide.Sell : CryptoOrderSide.Buy;

        var entry = PlaceOrder(database, position, CryptoPartPurpose.Entry, entrySide, entryPrice, quantity, when);
        FillOrder(position, entry, entryPrice);
        var exit = PlaceOrder(database, position, CryptoPartPurpose.TakeProfit, exitSide, exitPrice, quantity, when);
        FillOrder(position, exit, exitPrice);

        int multiplier = side == CryptoTradeSide.Long ? +1 : -1;
        return multiplier * (exitPrice - entryPrice) * quantity;
    }


    /// <summary>
    /// Two hundred closed trades, alternating long and short, alternating winner and loser, at
    /// prices that do not divide evenly. The balance at the end has to be the start plus the sum of
    /// what each trade earned - to the cent, not approximately.
    /// </summary>
    [TestMethod]
    public void AfterHundredsOfTrades_TheBalanceIsStartPlusTheSumOfTheResults()
    {
        const decimal start = 100_000m;
        var (database, symbol, assetQuote) = Arrange(start);
        DateTime when = DateTime.UtcNow.AddDays(-30);

        decimal expected = 0m;
        for (int i = 0; i < 200; i++)
        {
            CryptoTradeSide side = i % 2 == 0 ? CryptoTradeSide.Long : CryptoTradeSide.Short;
            // Prices that do not land on round numbers, so a rounding slip has somewhere to hide.
            decimal entryPrice = 37.13m + i * 0.07m;
            decimal move = i % 3 == 0 ? -1.31m : +2.17m;
            decimal exitPrice = side == CryptoTradeSide.Long ? entryPrice + move : entryPrice - move;
            expected += RoundTrip(database, symbol, side, entryPrice, exitPrice, 0.37m, when.AddMinutes(i));
        }

        Assert.AreEqual(start + expected, assetQuote.Total,
            "the final balance is the starting stake plus the sum of all results");
        Assert.AreEqual(0m, assetQuote.Locked, "alle posities gesloten, dus niets meer gereserveerd");
        Assert.AreEqual(assetQuote.Total, assetQuote.Free);
    }


    /// <summary>
    /// The same, but with the traffic a real run also carries: a position that averages down, an
    /// entry that times out without ever filling, and one position still open when the run ends.
    /// The open one must show up as Locked and NOT as a result.
    /// </summary>
    [TestMethod]
    public void AtTheEndOfARun_ClosedTradesAreInTheBalanceAndTheOpenOneIsOnlyReserved()
    {
        const decimal start = 10_000m;
        var (database, symbol, assetQuote) = Arrange(start);
        DateTime when = DateTime.UtcNow.AddDays(-10);

        // 1. Twenty ordinary closed trades, both directions.
        decimal expected = 0m;
        for (int i = 0; i < 20; i++)
        {
            CryptoTradeSide side = i % 2 == 0 ? CryptoTradeSide.Long : CryptoTradeSide.Short;
            decimal entryPrice = 50m + i;
            decimal exitPrice = side == CryptoTradeSide.Long ? entryPrice + 1.5m : entryPrice - 1.5m;
            expected += RoundTrip(database, symbol, side, entryPrice, exitPrice, 0.5m, when.AddMinutes(i));
        }

        // 2. A long that averages down: buy 1 @ 100, buy 1 @ 80, sell 2 @ 85 -> -10.
        CryptoPosition dcaPosition = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, when);
        var e1 = PlaceOrder(database, dcaPosition, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 100m, 1m, when);
        FillOrder(dcaPosition, e1, 100m);
        var e2 = PlaceOrder(database, dcaPosition, CryptoPartPurpose.Dca, CryptoOrderSide.Buy, 80m, 1m, when);
        FillOrder(dcaPosition, e2, 80m);
        var e3 = PlaceOrder(database, dcaPosition, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Sell, 85m, 2m, when);
        FillOrder(dcaPosition, e3, 85m);
        expected += -10m;

        // 3. An entry that never fills and is cancelled. Costs nothing.
        CryptoPosition timedOut = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, when);
        var never = PlaceOrder(database, timedOut, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 40m, 1m, when);
        never.Status = CryptoOrderStatus.Canceled;
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Canceled, never.Quantity, never.Quantity * never.Price, "entry-timeout");

        Assert.AreEqual(start + expected, assetQuote.Total, "up to here only the closed trades count");
        Assert.AreEqual(0m, assetQuote.Locked, "the expired entry left nothing behind");

        // 4. One position still open at the end: entry filled, exit order on the book.
        CryptoPosition open = CreateOpenPosition(database, symbol, CryptoTradeSide.Long, when);
        var openEntry = PlaceOrder(database, open, CryptoPartPurpose.Entry, CryptoOrderSide.Buy, 60m, 1m, when);
        FillOrder(open, openEntry, 60m);
        PlaceOrder(database, open, CryptoPartPurpose.TakeProfit, CryptoOrderSide.Sell, 66m, 1m, when);

        Assert.AreEqual(start + expected - 60m, assetQuote.Total,
            "the open position paid 60 and got nothing back yet");
        Assert.AreEqual(0m, assetQuote.Locked,
            "the sell order of a long reserves no quote money, the coins are already there");
        Assert.AreEqual(assetQuote.Total, assetQuote.Free);
    }
}
