using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for PaperTrading: order fill logic (PaperTradingCheckStep)
/// and paper trade creation (CreatePaperTrade) including fee calculation.
/// Uses a real SQLite database — same pattern as PaperAssetsTests.
/// </summary>
[TestClass]
[DoNotParallelize]
public class PaperTradingTests : TestBase
{
    // ─── helpers ─────────────────────────────────────────────────────────────

    private static (CryptoDatabase db, CryptoSymbol symbol) SetupTestEnvironment()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        DeleteAllPositionRelatedStuff(database);
        CryptoSymbol symbol = CreateTestSymbol(database);

        // CreateTestSymbol calls GlobalData.AddSymbol before DB Insert, so the
        // symbol is registered in SymbolListId under Id=0. Fix by re-registering
        // with the real DB-assigned Id so that AddOrder/AddTrade lookups work.
        var exchange = symbol.Exchange;
        if (!exchange.SymbolListId.ContainsKey(symbol.Id))
            exchange.SymbolListId[symbol.Id] = symbol;

        return (database, symbol);
    }

    private static CryptoPosition CreateTestPosition(
        CryptoDatabase database, CryptoSymbol symbol, CryptoTradeSide side)
    {
        DateTime startTime = DateTime.UtcNow.AddHours(-24);
        CryptoPosition position = PositionTools.CreatePosition(
            symbol, CryptoSignalStrategy.Stobb, side, "PaperTradingTest",
            symbol.Data.SymbolIntervalList[0], startTime);

        // Register in active exchange so HandleTradeAsync can find it
        GlobalData.ActiveExchange!.Data.PositionList[symbol.Name] = position;
        return position;
    }

    private static int _nextPartId = 1000;

    private static CryptoPositionPart CreateTestPart(
        CryptoPosition position, CryptoPartPurpose purpose = CryptoPartPurpose.Entry)
    {
        int partId = Interlocked.Increment(ref _nextPartId);
        CryptoPositionPart part = new()
        {
            Id = partId,
            Position = position,
            PositionId = position.Id,
            Exchange = position.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            Purpose = purpose,
            Strategy = position.Strategy,
            CreateTime = position.CreateTime,
            PartNumber = 0,
            SignalPrice = 100m,
        };
        position.PartList.TryAdd(part.Id, part);
        return part;
    }

    private static int _nextStepId = 2000;

    private static CryptoPositionStep CreateTestStep(
        CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        CryptoOrderSide side, CryptoOrderType orderType, decimal price, decimal quantity,
        decimal? stopPrice = null)
    {
        int stepId = Interlocked.Increment(ref _nextStepId);
        CryptoPositionStep step = new()
        {
            Id = stepId,
            PositionId = position.Id,
            PositionPartId = part.Id,
            CreateTime = position.CreateTime,
            Side = side,
            Status = CryptoOrderStatus.New,
            OrderType = orderType,
            OrderId = "STEP" + database.CreateNewUniqueId(),
            Price = price,
            Quantity = quantity,
            StopPrice = stopPrice,
        };
        part.StepList.TryAdd(step.Id, step);
        return step;
    }

    private static CryptoCandle MakeCandle(decimal open, decimal high, decimal low, decimal close, DateTime? openTime = null)
    {
        // Default to "now": PaperTradingCheckStep/CheckStepAgainstCandle reject a fill when
        // step.CreateTime > candle.Date (a step cannot fill on a candle that predates it), and
        // CreateTestPosition/CreateTestStep stamp CreateTime as DateTime.UtcNow.AddHours(-24). A
        // fixed small CandleTime (close to the 2010-01-04 epoch) would always predate that and the
        // guard would silently swallow every fill.
        return new CryptoCandle
        {
            TickDecimals = 4,
            OpenTime = CandleTime.FromDateTime(openTime ?? DateTime.UtcNow),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };
    }

    // ─── PaperTradingCheckStep: Buy Limit ────────────────────────────────────

    [TestMethod]
    public async Task CheckStep_BuyLimit_FillsWhenCandleLowBelowPrice()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        // Candle low dips below the limit price → should fill
        var candle = MakeCandle(open: 52m, high: 55m, low: 49m, close: 51m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(1, position.OrderList.Count, "Order should be created");
        Assert.AreEqual(1, position.TradeList.Count, "Trade should be created");

        var order = position.OrderList.Values[0];
        Assert.AreEqual(50m, order.Price, "Fill price should equal limit price");
        Assert.AreEqual(1m, order.QuantityFilled);
    }

    [TestMethod]
    public async Task CheckStep_BuyLimit_NoFillWhenCandleLowAbovePrice()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        // Candle low stays above limit price → should NOT fill
        var candle = MakeCandle(open: 52m, high: 55m, low: 50.5m, close: 53m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(0, position.OrderList.Count, "No order — candle did not reach limit price");
        Assert.AreEqual(0, position.TradeList.Count, "No trade — candle did not reach limit price");
    }

    // ─── PaperTradingCheckStep: Sell Limit ───────────────────────────────────

    [TestMethod]
    public async Task CheckStep_SellLimit_FillsWhenCandleHighAbovePrice()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position, CryptoPartPurpose.TakeProfit);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Sell, CryptoOrderType.Limit, price: 60m, quantity: 1m);

        // Candle high exceeds limit price → should fill
        var candle = MakeCandle(open: 55m, high: 61m, low: 54m, close: 58m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(1, position.OrderList.Count, "Order should be created");
        var order = position.OrderList.Values[0];
        Assert.AreEqual(60m, order.Price, "Fill price should equal limit price");
    }

    [TestMethod]
    public async Task CheckStep_SellLimit_NoFillWhenCandleHighBelowPrice()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position, CryptoPartPurpose.TakeProfit);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Sell, CryptoOrderType.Limit, price: 60m, quantity: 1m);

        // Candle high stays below limit price → should NOT fill
        var candle = MakeCandle(open: 55m, high: 59m, low: 54m, close: 58m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(0, position.OrderList.Count, "No fill — candle did not reach limit price");
    }

    // ─── PaperTradingCheckStep: Market orders ────────────────────────────────

    [TestMethod]
    public async Task CheckStep_BuyMarket_FillsAtCandleClose()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Market, price: 0m, quantity: 2m);

        var candle = MakeCandle(open: 100m, high: 105m, low: 98m, close: 102m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(1, position.OrderList.Count, "Market order should always fill");
        var order = position.OrderList.Values[0];
        Assert.AreEqual(102m, order.Price, "Market order fills at candle close");
        Assert.AreEqual(2m, order.QuantityFilled);
    }

    [TestMethod]
    public async Task CheckStep_SellMarket_FillsAtCandleClose()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position, CryptoPartPurpose.TakeProfit);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Sell, CryptoOrderType.Market, price: 0m, quantity: 2m);

        var candle = MakeCandle(open: 100m, high: 105m, low: 98m, close: 103m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(1, position.OrderList.Count, "Market sell should always fill");
        var order = position.OrderList.Values[0];
        Assert.AreEqual(103m, order.Price, "Market sell fills at candle close");
    }

    // ─── PaperTradingCheckStep: Stop orders ──────────────────────────────────

    [TestMethod]
    public async Task CheckStep_BuyStop_FillsWhenCandleHighReachesStopPrice()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Short);
        var part = CreateTestPart(position, CryptoPartPurpose.TakeProfit);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.StopLimit, price: 55m, quantity: 1m,
            stopPrice: 52m);

        // Candle high reaches the stop price → fill at stop price
        var candle = MakeCandle(open: 50m, high: 53m, low: 49m, close: 51m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(1, position.OrderList.Count, "Stop triggered — order should be created");
        var order = position.OrderList.Values[0];
        Assert.AreEqual(52m, order.Price, "Fill price should equal stop price");
    }

    [TestMethod]
    public async Task CheckStep_SellStop_FillsWhenCandleLowReachesStopPrice()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Sell, CryptoOrderType.StopLimit, price: 40m, quantity: 1m,
            stopPrice: 45m);

        // Candle low reaches the stop price → fill at stop price
        var candle = MakeCandle(open: 50m, high: 52m, low: 44m, close: 47m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(1, position.OrderList.Count, "Stop triggered — order should be created");
        var order = position.OrderList.Values[0];
        Assert.AreEqual(45m, order.Price, "Fill price should equal stop price");
    }

    // ─── PaperTradingCheckStep: already filled step is skipped ───────────────

    [TestMethod]
    public async Task CheckStep_AlreadyFilled_IsSkipped()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        // Mark step as already filled
        step.Status = CryptoOrderStatus.Filled;

        var candle = MakeCandle(open: 52m, high: 55m, low: 40m, close: 51m);

        await PaperTrading.PaperTradingCheckStep(db, position, part, step, candle);

        Assert.AreEqual(0, position.OrderList.Count, "Already-filled step should not create another order");
    }

    // ─── CreatePaperTrade: duplicate prevention ──────────────────────────────

    [TestMethod]
    public async Task CreatePaperTrade_DuplicateOrderId_IsRejected()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        CandleTime ct = new(100);

        // First call creates the order/trade
        await PaperTrading.CreatePaperTrade(db, position, part, step, 50m, ct);
        Assert.AreEqual(1, position.OrderList.Count);

        // Second call with same step (same OrderId) should be rejected
        await PaperTrading.CreatePaperTrade(db, position, part, step, 50m, ct);
        Assert.AreEqual(1, position.OrderList.Count, "Duplicate order should be rejected");
    }

    [TestMethod]
    public async Task CreatePaperTrade_NullOrderId_IsRejected()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);
        step.OrderId = null;

        CandleTime ct = new(100);

        await PaperTrading.CreatePaperTrade(db, position, part, step, 50m, ct);
        Assert.AreEqual(0, position.OrderList.Count, "Null OrderId should be rejected");
    }

    // ─── CreatePaperTrade: spot fee calculation ──────────────────────────────

    [TestMethod]
    public async Task CreatePaperTrade_SpotLongEntry_FeeInBase()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 100m, quantity: 10m);

        CandleTime ct = new(100);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 100m, ct);

        Assert.AreEqual(1, position.TradeList.Count);
        var trade = position.TradeList.Values[0];

        // Spot long entry (Buy): fee in base asset
        Assert.AreEqual(symbol.Base, trade.CommissionAsset,
            "Spot long entry fee should be in base asset");

        // FeeRate=0.1%, qty=10 → commission = 10 * 0.1 / 100 = 0.01
        decimal expectedFee = 10m * 0.1m / 100m;
        Assert.AreEqual(expectedFee, trade.Commission, 0.00001m,
            "Commission = quantity * feeRate / 100");
    }

    [TestMethod]
    public async Task CreatePaperTrade_SpotLongTp_FeeInQuote()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position, CryptoPartPurpose.TakeProfit);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Sell, CryptoOrderType.Limit, price: 110m, quantity: 10m);

        CandleTime ct = new(100);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 110m, ct);

        var trade = position.TradeList.Values[0];

        // Spot long TP (Sell): fee in quote asset
        Assert.AreEqual(symbol.Quote, trade.CommissionAsset,
            "Spot long TP fee should be in quote asset");

        // FeeRate=0.1%, qty=10, price=110 → commission = 10 * 110 * 0.1 / 100 = 1.1
        decimal expectedFee = 10m * 110m * 0.1m / 100m;
        Assert.AreEqual(expectedFee, trade.Commission, 0.00001m);
    }

    [TestMethod]
    public async Task CreatePaperTrade_SpotShortEntry_FeeInQuote()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Short);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Sell, CryptoOrderType.Limit, price: 100m, quantity: 10m);

        CandleTime ct = new(100);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 100m, ct);

        var trade = position.TradeList.Values[0];

        // Spot short entry (Sell): fee in quote asset
        Assert.AreEqual(symbol.Quote, trade.CommissionAsset,
            "Spot short entry fee should be in quote asset");

        decimal expectedFee = 10m * 100m * 0.1m / 100m;
        Assert.AreEqual(expectedFee, trade.Commission, 0.00001m);
    }

    // ─── CreatePaperTrade: order fields ──────────────────────────────────────

    [TestMethod]
    public async Task CreatePaperTrade_OrderFieldsAreCorrect()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 75m, quantity: 4m);

        CandleTime ct = new(200);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 75m, ct);

        var order = position.OrderList.Values[0];

        Assert.AreEqual(step.OrderId, order.OrderId);
        Assert.AreEqual(CryptoOrderSide.Buy, order.Side);
        Assert.AreEqual(75m, order.Price);
        Assert.AreEqual(75m, order.AveragePrice);
        Assert.AreEqual(4m, order.Quantity);
        Assert.AreEqual(4m, order.QuantityFilled);
        Assert.AreEqual(300m, order.QuoteQuantity, "QuoteQuantity = qty * price");
        Assert.AreEqual(300m, order.QuoteQuantityFilled);
        Assert.AreEqual(0m, order.Commission, "Order-level commission is always 0");
    }

    // ─── CreatePaperTrade: DCA part gets Filled status ───────────────────────

    [TestMethod]
    public async Task CreatePaperTrade_DcaPart_OrderStatusIsFilled()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position, CryptoPartPurpose.Dca);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 90m, quantity: 2m);

        CandleTime ct = new(100);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 90m, ct);

        var order = position.OrderList.Values[0];
        Assert.AreEqual(CryptoOrderStatus.Filled, order.Status,
            "DCA part orders get Filled status (not PartiallyAndClosed)");
    }

    [TestMethod]
    public async Task CreatePaperTrade_EntryPart_OrderStatusIsPartiallyAndClosed()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        var part = CreateTestPart(position, CryptoPartPurpose.Entry);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 90m, quantity: 2m);

        CandleTime ct = new(100);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 90m, ct);

        var order = position.OrderList.Values[0];
        Assert.AreEqual(CryptoOrderStatus.PartiallyAndClosed, order.Status,
            "Non-DCA parts get PartiallyAndClosed status");
    }

    // ─── CreatePaperTrade: HasOrdersAndTradesLoaded flag ─────────────────────

    [TestMethod]
    public async Task CreatePaperTrade_SetsHasOrdersAndTradesLoaded()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        Assert.IsFalse(position.HasOrdersAndTradesLoaded);

        var part = CreateTestPart(position);
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        CandleTime ct = new(100);
        await PaperTrading.CreatePaperTrade(db, position, part, step, 50m, ct);

        Assert.IsTrue(position.HasOrdersAndTradesLoaded,
            "Flag must be set to skip expensive DB reload in CalculatePositionResultsViaOrders");
    }

    // ─── PaperTradingCheckOrders: iterates open parts/steps ──────────────────

    [TestMethod]
    public async Task CheckOrders_FillsMultipleOpenSteps()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);

        // Entry part with buy limit
        var entryPart = CreateTestPart(position);
        var entryStep = CreateTestStep(db, position, entryPart,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        // TP part with sell limit
        var tpPart = CreateTestPart(position, CryptoPartPurpose.TakeProfit);
        var tpStep = CreateTestStep(db, position, tpPart,
            CryptoOrderSide.Sell, CryptoOrderType.Limit, price: 60m, quantity: 1m);

        // Candle spans both prices (low < 50, high > 60)
        var candle = MakeCandle(open: 55m, high: 65m, low: 45m, close: 58m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, candle);

        Assert.AreEqual(2, position.OrderList.Count, "Both entry and TP should fill");
        Assert.AreEqual(2, position.TradeList.Count);
    }

    [TestMethod]
    public async Task CheckOrders_SkipsClosedParts()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);

        var part = CreateTestPart(position);
        part.CloseTime = DateTime.UtcNow; // Mark part as closed
        var step = CreateTestStep(db, position, part,
            CryptoOrderSide.Buy, CryptoOrderType.Limit, price: 50m, quantity: 1m);

        var candle = MakeCandle(open: 52m, high: 55m, low: 40m, close: 51m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, candle);

        Assert.AreEqual(0, position.OrderList.Count, "Closed part should be skipped");
    }

    // ─── HandleTradeAsync: sets ForceCheckPosition and DelayUntil ────────────

    [TestMethod]
    public async Task HandleTradeAsync_SetsForceCheckAndDelay()
    {
        var (db, symbol) = SetupTestEnvironment();
        var position = CreateTestPosition(db, symbol, CryptoTradeSide.Long);
        position.ForceCheckPosition = false;

        CryptoOrder order = new()
        {
            Exchange = symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = symbol,
            SymbolId = position.SymbolId,
            OrderId = "TEST-ORDER-1",
            Status = CryptoOrderStatus.Filled,
            Side = CryptoOrderSide.Buy,
        };

        await TradeHandler.HandleTradeAsync(symbol, CryptoOrderStatus.Filled, order);

        Assert.IsTrue(position.ForceCheckPosition,
            "HandleTradeAsync should set ForceCheckPosition=true");
        Assert.IsTrue(position.DelayUntil > DateTime.MinValue,
            "HandleTradeAsync should set DelayUntil to a future time");
    }
}
