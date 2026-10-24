using CryptoScanner.Analyzers.FvgLadder;
using CryptoScanner.Analyzers.FvgLadder.Signal;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Several positions on one coin (StrategyRegistration.ParallelPositions), as the fvgladder strategy
/// holds them: how they are filed in the position list, that every lookup that has to see them
/// does, that a fill lands on the position it belongs to, and that an ordinary strategy keeps its
/// one position per coin exactly as before.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ParallelPositionsTests : TestBase
{
    private const string Parallel = "fvgladder";
    private const string Ordinary = "stobb";

    private static (CryptoDatabase db, CryptoSymbol symbol) Setup()
    {
        InitTestSession();
        RegisterPlugin(new FvgLadderPlugin());
        CryptoDatabase database = new();
        database.Open();
        DeleteAllPositionRelatedStuff(database);
        GlobalData.ActiveExchange!.Data.PositionList.Clear();
        return (database, CreateTestSymbol(database));
    }

    private static int _nextId = 7000;

    private static CryptoPosition AddPosition(CryptoSymbol symbol, string strategy)
    {
        CryptoPosition position = PositionTools.CreatePosition(symbol, strategy, CryptoTradeSide.Long, "ParallelPositionsTest",
            symbol.Data.SymbolIntervalList[0], DateTime.UtcNow.AddHours(-24));
        position.Id = Interlocked.Increment(ref _nextId);
        GlobalData.ActiveExchange!.Data.PositionList[PositionTools.PositionListKey(position)] = position;
        return position;
    }

    private static CryptoPositionPart AddPart(CryptoPosition position, CryptoPartPurpose purpose, int partNumber)
    {
        CryptoPositionPart part = new()
        {
            Id = Interlocked.Increment(ref _nextId),
            Position = position,
            PositionId = position.Id,
            Exchange = position.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            Purpose = purpose,
            Strategy = position.Strategy ?? "",
            CreateTime = position.CreateTime,
            PartNumber = partNumber,
            SignalPrice = 100m,
        };
        position.PartList.TryAdd(part.Id, part);
        return part;
    }

    private static CryptoPositionStep AddBuy(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        decimal price, decimal quantity, CryptoOrderStatus status = CryptoOrderStatus.New)
    {
        CryptoPositionStep step = new()
        {
            Id = Interlocked.Increment(ref _nextId),
            PositionId = position.Id,
            PositionPartId = part.Id,
            CreateTime = position.CreateTime,
            CloseTime = status == CryptoOrderStatus.New ? null : position.CreateTime.AddMinutes(part.PartNumber),
            Side = CryptoOrderSide.Buy,
            Status = status,
            OrderType = CryptoOrderType.Limit,
            OrderId = "STEP" + database.CreateNewUniqueId(),
            Price = price,
            Quantity = quantity,
        };
        PositionTools.AddPositionPartStep(part, step);
        return step;
    }

    private static CryptoCandle Candle(decimal low)
        => new()
        {
            TickDecimals = 4,
            OpenTime = CandleTime.FromDateTime(DateTime.UtcNow),
            Open = 101m,
            High = 102m,
            Low = low,
            Close = 101m,
            Volume = 1000m,
        };


    [TestMethod]
    public void TheRegistration_GivesFvgLadderItsThreeTraderProperties()
    {
        Setup();
        Assert.IsTrue(RegisterAlgorithms.HasParallelPositions(Parallel));
        Assert.IsTrue(RegisterAlgorithms.HasStandingEntry(Parallel));
        Assert.IsTrue(RegisterAlgorithms.HasNoStopLoss(Parallel));
        Assert.IsTrue(RegisterAlgorithms.IsZoneStrategy(Parallel));

        Assert.IsFalse(RegisterAlgorithms.HasParallelPositions(Ordinary));
        Assert.IsFalse(RegisterAlgorithms.HasStandingEntry(Ordinary));
        Assert.IsFalse(RegisterAlgorithms.HasNoStopLoss(Ordinary));
    }


    [TestMethod]
    public void AnOrdinaryPosition_IsStillFiledUnderTheSymbolName()
    {
        var (_, symbol) = Setup();
        CryptoPosition position = AddPosition(symbol, Ordinary);

        Assert.AreEqual(symbol.Name, PositionTools.PositionListKey(position));
        Assert.AreSame(position, PositionTools.HasPosition(GlobalData.ActiveExchange!, symbol));
    }


    [TestMethod]
    public void ParallelPositions_HaveAKeyEach_AndAreSeenByEveryLookupThatHasTo()
    {
        var (_, symbol) = Setup();
        var exchange = GlobalData.ActiveExchange!;
        CryptoPosition first = AddPosition(symbol, Parallel);
        CryptoPosition second = AddPosition(symbol, Parallel);

        Assert.AreEqual($"{symbol.Name}#{first.Id}", PositionTools.PositionListKey(first));
        Assert.AreEqual(2, exchange.Data.PositionList.Count);

        // The lookups that assume one position per coin do not see them...
        Assert.IsNull(PositionTools.HasPosition(exchange, symbol));
        // ...the ones that have to, do
        CollectionAssert.AreEqual(new[] { first, second }, PositionTools.PositionsOf(exchange, symbol));
        CollectionAssert.AreEqual(new[] { first, second }, PositionTools.ParallelPositionsOf(exchange, symbol, Parallel));
        Assert.IsTrue(PositionTools.HasAnyPosition(exchange, symbol));
        Assert.IsTrue(symbol.IsTrading());
        Assert.IsTrue(PositionTools.IsListed(exchange, second));

        // Removing one leaves the other
        PositionTools.RemovePosition(exchange, first, false);
        Assert.IsFalse(PositionTools.IsListed(exchange, first));
        CollectionAssert.AreEqual(new[] { second }, PositionTools.PositionsOf(exchange, symbol));
    }


    [TestMethod]
    public void AFill_FindsThePositionItsOrderBelongsTo()
    {
        var (db, symbol) = Setup();
        var exchange = GlobalData.ActiveExchange!;
        CryptoPosition first = AddPosition(symbol, Parallel);
        CryptoPosition second = AddPosition(symbol, Parallel);
        AddBuy(db, first, AddPart(first, CryptoPartPurpose.Entry, 0), 100m, 1m);
        CryptoPositionStep secondBuy = AddBuy(db, second, AddPart(second, CryptoPartPurpose.Entry, 0), 98m, 1m);

        Assert.AreSame(second, PositionTools.PositionOfOrder(exchange, symbol, secondBuy.OrderId));
        Assert.IsNull(PositionTools.PositionOfOrder(exchange, symbol, "unknown"));
    }


    [TestMethod]
    public async Task PaperFills_CheckEveryPositionOnTheCoin_EachOnItsOwnOrders()
    {
        var (db, symbol) = Setup();
        var exchange = GlobalData.ActiveExchange!;
        CryptoPosition high = AddPosition(symbol, Parallel);
        CryptoPosition low = AddPosition(symbol, Parallel);
        AddBuy(db, high, AddPart(high, CryptoPartPurpose.Entry, 0), 100m, 1m);
        AddBuy(db, low, AddPart(low, CryptoPartPurpose.Entry, 0), 97m, 1m);

        // The candle goes through 100 but not through 97: only the higher order fills
        await PaperTrading.PaperTradingCheckOrders(db, exchange, symbol, Candle(low: 99m));

        Assert.AreEqual(1, high.OrderList.Count, "the order of the higher position fills");
        Assert.AreEqual(100m, high.OrderList.Values[0].Price);
        Assert.AreEqual(0, low.OrderList.Count, "the lower one is not reached");
    }


    [TestMethod]
    public void Slots_ACoinWithAParallelPositionStillTakesAnotherOne()
    {
        var (_, symbol) = Setup();
        var exchange = GlobalData.ActiveExchange!;
        int slots = GlobalData.Settings.Trading.SlotsMaximalLong;
        try
        {
            GlobalData.Settings.Trading.SlotsMaximalLong = 5;
            AddPosition(symbol, Ordinary);

            // An ordinary strategy: one position per coin, as always
            Assert.IsFalse(SymbolTools.CheckAvailableSlots(exchange, symbol, CryptoTradeSide.Long, out _));
            // A parallel one only counts the long slots
            Assert.IsTrue(SymbolTools.CheckAvailableSlots(exchange, symbol, CryptoTradeSide.Long, out _, parallel: true));

            GlobalData.Settings.Trading.SlotsMaximalLong = 1;
            Assert.IsFalse(SymbolTools.CheckAvailableSlots(exchange, symbol, CryptoTradeSide.Long, out string reaction, parallel: true));
            StringAssert.Contains(reaction, "slots");
        }
        finally
        {
            GlobalData.Settings.Trading.SlotsMaximalLong = slots;
        }
    }


    [TestMethod]
    public void NoDca_IsAnEmptyLadder_NotTheGlobalOne()
    {
        var (_, symbol) = Setup();
        CryptoPosition position = AddPosition(symbol, Parallel);
        var global = GlobalData.Settings.Trading.DcaList;
        try
        {
            GlobalData.Settings.Trading.DcaList = [new CryptoScanner.Core.Settings.CryptoDcaEntry { Percentage = 2m, Factor = 100m }];

            position.DcaLevels = DcaLadder.None;
            Assert.AreEqual(0, TradeTools.EffectiveDcaList(position).Count);

            position.DcaLevels = null;
            Assert.AreEqual(1, TradeTools.EffectiveDcaList(position).Count, "null still means the global ladder");
        }
        finally
        {
            GlobalData.Settings.Trading.DcaList = global;
        }
    }


    [TestMethod]
    public void NoStopLoss_TheGlobalStopIsNotGivenToTheStrategy()
    {
        // What PositionMonitor.CalculateSlPrices hands the calculator for a fvgladder position
        var result = StopLossCalculator.Calculate(new StopLossCalculator.SlInput
        {
            Side = CryptoTradeSide.Long,
            SlPercentage = null,
            EntryPrice = 100m,
            GlobalStopLossPercentage = RegisterAlgorithms.HasNoStopLoss(Parallel) ? 0m : 5m,
            GlobalStopLossLimitPercentage = 0m,
        });

        Assert.IsNull(result.Stop);
    }


    [TestMethod]
    public void TheParallelRule_ReadsTheFirstAndLastBuyAndWhetherTheDcasAreDone()
    {
        var (db, symbol) = Setup();
        CryptoPosition position = AddPosition(symbol, Parallel);
        position.Status = CryptoPositionStatus.Trading;
        position.DcaLevels = "1.5:200;2.9775:400";
        AddBuy(db, position, AddPart(position, CryptoPartPurpose.Entry, 0), 100m, 1m, CryptoOrderStatus.Filled);
        AddBuy(db, position, AddPart(position, CryptoPartPurpose.Dca, 1), 98.5m, 2m, CryptoOrderStatus.Filled);
        CryptoPositionStep secondDca = AddBuy(db, position, AddPart(position, CryptoPartPurpose.Dca, 2), 97.0225m, 4m);

        // One of the two DCA's bought: not done, the parallel rule waits
        Assert.IsTrue(FvgLadderLong.TryGetTrade(position, out FvgLadderTrade trade));
        Assert.AreEqual(100m, trade.FirstBuyPrice);
        Assert.AreEqual(98.5m, trade.LastBuyPrice);
        Assert.IsFalse(trade.DcasDone);
        Assert.IsFalse(FvgLadderRules.NewTradeAllowed([trade], 1.5m, out _));

        // Both bought: done, and the next position goes 1.5% under the last buy
        secondDca.Status = CryptoOrderStatus.Filled;
        secondDca.CloseTime = position.CreateTime.AddMinutes(5);
        Assert.IsTrue(FvgLadderLong.TryGetTrade(position, out trade));
        Assert.AreEqual(97.0225m, trade.LastBuyPrice);
        Assert.IsTrue(trade.DcasDone);
        Assert.IsTrue(FvgLadderRules.NewTradeAllowed([trade], 1.5m, out decimal? limit));
        Assert.AreEqual(95.5671625m, limit);

        // A position that bought nothing yet does not count
        CryptoPosition waiting = AddPosition(symbol, Parallel);
        AddBuy(db, waiting, AddPart(waiting, CryptoPartPurpose.Entry, 0), 95m, 1m);
        Assert.IsFalse(FvgLadderLong.TryGetTrade(waiting, out _));
    }


    [TestMethod]
    public void TheTakeProfit_IsOneLevelOnTheBreakEvenPrice()
    {
        var (_, symbol) = Setup();
        CryptoPosition position = AddPosition(symbol, Parallel);
        position.TpPercentage = 1m;

        // One level for the whole position, at the handed over distance. The trader measures it from
        // TpGridBreakEvenPrice, the break-even of every entry and DCA fill including the fees, which
        // moves with each DCA (PositionMonitor.CalculateTpPrice).
        var levels = TradeTools.EffectiveTpList(position);
        Assert.AreEqual(1, levels.Count);
        Assert.AreEqual(1m, levels[0].Percentage);
        Assert.AreEqual(100m, levels[0].Factor);

        // Break-even 98.0 -> take profit 98.98
        Assert.AreEqual(98.98m, PricePlacement.Favorable(CryptoTradeSide.Long, 98m, levels[0].Percentage));
    }
}
