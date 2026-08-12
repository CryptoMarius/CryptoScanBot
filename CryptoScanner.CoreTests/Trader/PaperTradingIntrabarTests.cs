using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for the intrabar drill-down in PaperTradingCheckOrders.
///
/// A candle gives four numbers and no path between them. When one candle touches both the take
/// profit and the stop loss, the outcome depends entirely on which came first — and the candle
/// cannot say. At a coarse base interval (the emulator can replay on 5m or 15m) that situation is
/// common, so paper trading loads the finest available candles inside the base candle and walks
/// them in time order.
///
/// Each test writes real 1m candles into the candle database, because that is where the drill-down
/// reads them from. Every test uses its own time window so they cannot influence each other.
/// </summary>
[TestClass]
[DoNotParallelize]
public class PaperTradingIntrabarTests : TestBase
{
    private const uint BaseDuration = 15;

    // Take profit above, stop loss below. A 15m candle that reaches both is ambiguous.
    private const decimal TakeProfitPrice = 60m;
    private const decimal StopLossPrice = 40m;


    private static (CryptoDatabase db, CryptoSymbol symbol) SetupTestEnvironment()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        DeleteAllPositionRelatedStuff(database);
        CryptoSymbol symbol = CreateTestSymbol(database);

        // CreateTestSymbol registers the symbol before the DB assigns its Id; re-register under the
        // real Id so order/trade lookups resolve (same fixup as PaperTradingTests).
        var exchange = symbol.Exchange;
        if (!exchange.SymbolListId.ContainsKey(symbol.Id))
            exchange.SymbolListId[symbol.Id] = symbol;

        CandleDatabase.InitializeSchema(exchange);
        return (database, symbol);
    }


    private static int _nextId = 9000;


    /// <summary>
    /// Position with one sell step carrying BOTH a limit price (take profit) and a stop price
    /// (stop loss) — exactly how TradeTools.PlaceTakeProfitOrderAtPrice builds it in production,
    /// and the reason a single step can already be ambiguous.
    /// </summary>
    private static (CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step)
        CreateTakeProfitPosition(CryptoDatabase database, CryptoSymbol symbol, DateTime createTime)
    {
        CryptoPosition position = PositionTools.CreatePosition(
            symbol, "stobb", CryptoTradeSide.Long, "IntrabarTest",
            symbol.Data.SymbolIntervalList[0], createTime);
        GlobalData.ActiveExchange!.Data.PositionList[symbol.Name] = position;

        CryptoPositionPart part = new()
        {
            Id = Interlocked.Increment(ref _nextId),
            Position = position,
            PositionId = position.Id,
            Exchange = position.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = symbol,
            SymbolId = position.SymbolId,
            Purpose = CryptoPartPurpose.TakeProfit,
            Strategy = position.Strategy ?? "", // the part requires one, the position allows null
            CreateTime = createTime,
            PartNumber = 1,
            SignalPrice = 50m,
        };
        position.PartList.TryAdd(part.Id, part);

        CryptoPositionStep step = new()
        {
            Id = Interlocked.Increment(ref _nextId),
            PositionId = position.Id,
            PositionPartId = part.Id,
            CreateTime = createTime,
            Side = CryptoOrderSide.Sell,
            Status = CryptoOrderStatus.New,
            OrderType = CryptoOrderType.Limit,
            OrderId = "STEP" + database.CreateNewUniqueId(),
            Price = TakeProfitPrice,
            StopPrice = StopLossPrice,
            Quantity = 1m,
        };
        part.StepList.TryAdd(step.Id, step);

        return (position, part, step);
    }


    private static CryptoCandle MakeCandle(CandleTime openTime, decimal open, decimal high, decimal low, decimal close)
    {
        return new CryptoCandle
        {
            TickDecimals = 4,
            OpenTime = openTime,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };
    }


    /// <summary>
    /// Writes fifteen 1m candles into the candle database for the given window. Only the candle at
    /// <paramref name="takeProfitMinute"/> reaches the take profit and only the one at
    /// <paramref name="stopLossMinute"/> reaches the stop loss; every other minute stays neatly
    /// between the two levels, so the sequence is unambiguous per sub-candle and only the ORDER of
    /// those two decides the outcome.
    /// </summary>
    private static void WriteMinuteCandles(CryptoSymbol symbol, CandleTime windowStart,
        int takeProfitMinute, int stopLossMinute)
    {
        CryptoSymbolInterval oneMinute = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        oneMinute.CandleList.Clear();

        for (int minute = 0; minute < (int)BaseDuration; minute++)
        {
            decimal high = 50m;
            decimal low = 50m;
            if (minute == takeProfitMinute)
                high = TakeProfitPrice + 1m;
            if (minute == stopLossMinute)
                low = StopLossPrice - 1m;

            CandleTime openTime = windowStart + (uint)minute;
            oneMinute.CandleList.Add(openTime, MakeCandle(openTime, 50m, high, low, 50m));
        }

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        CandleDatabase.SaveCandlesForSymbol(candleDb.Connection, symbol);
        oneMinute.CandleList.Clear();
    }


    /// <summary>
    /// Start of a 15m-aligned window, at a FIXED point in time rather than relative to "now".
    /// Two reasons: the test then produces the same candles on every run (a window derived from
    /// DateTime.UtcNow would write to new timestamps each time and grow TestData/{exchange}.db
    /// indefinitely), and the outcome cannot depend on when the suite happens to run. Each test
    /// picks its own window number so they never share candles.
    /// </summary>
    private static CandleTime AlignedWindow(int windowNumber)
    {
        // 2020-01-01 00:00 UTC, comfortably after the CandleTime epoch (2010-01-04).
        CandleTime start = CandleTime.AlignFromDateTime(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), BaseDuration);
        return start + (uint)windowNumber * BaseDuration;
    }


    [TestMethod]
    public async Task Intrabar_TakeProfitBeforeStopLoss_FillsAtTakeProfit()
    {
        var (db, symbol) = SetupTestEnvironment();
        CandleTime window = AlignedWindow(8);
        WriteMinuteCandles(symbol, window, takeProfitMinute: 2, stopLossMinute: 11);

        var (position, _, _) = CreateTakeProfitPosition(db, symbol, window.ToDateTime().AddHours(-1));

        // The 15m candle reaches both levels, so on its own it cannot tell them apart.
        var baseCandle = MakeCandle(window, open: 50m, high: TakeProfitPrice + 1m, low: StopLossPrice - 1m, close: 50m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, baseCandle, BaseDuration);

        Assert.AreEqual(1, position.OrderList.Count, "Exactly one fill expected");
        Assert.AreEqual(TakeProfitPrice, position.OrderList.Values[0].Price,
            "The minute candles show the take profit was reached first, so that is the fill");
    }


    [TestMethod]
    public async Task Intrabar_StopLossBeforeTakeProfit_FillsAtStopLoss()
    {
        var (db, symbol) = SetupTestEnvironment();
        CandleTime window = AlignedWindow(12);
        WriteMinuteCandles(symbol, window, takeProfitMinute: 11, stopLossMinute: 2);

        var (position, _, _) = CreateTakeProfitPosition(db, symbol, window.ToDateTime().AddHours(-1));

        var baseCandle = MakeCandle(window, open: 50m, high: TakeProfitPrice + 1m, low: StopLossPrice - 1m, close: 50m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, baseCandle, BaseDuration);

        Assert.AreEqual(1, position.OrderList.Count, "Exactly one fill expected");
        Assert.AreEqual(StopLossPrice, position.OrderList.Values[0].Price,
            "The minute candles show the stop loss was reached first, so that is the fill");
    }


    [TestMethod]
    public async Task Intrabar_FillTimeComesFromTheMinuteCandle()
    {
        var (db, symbol) = SetupTestEnvironment();
        CandleTime window = AlignedWindow(16);
        WriteMinuteCandles(symbol, window, takeProfitMinute: 3, stopLossMinute: 11);

        var (position, _, _) = CreateTakeProfitPosition(db, symbol, window.ToDateTime().AddHours(-1));
        var baseCandle = MakeCandle(window, open: 50m, high: TakeProfitPrice + 1m, low: StopLossPrice - 1m, close: 50m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, baseCandle, BaseDuration);

        // Without the drill-down the fill would be stamped at the END of the 15m candle; with it,
        // at the close of the minute that actually reached the level.
        DateTime expected = (window + 4).ToDateTime(); // minute 3 closes at minute 4
        Assert.AreEqual(expected, position.OrderList.Values[0].UpdateTime,
            "Fill time should be the close of the minute candle, not of the whole 15m candle");
    }


    [TestMethod]
    public async Task Intrabar_NoFinerCandlesAvailable_FallsBackToStopLossFirst()
    {
        var (db, symbol) = SetupTestEnvironment();
        // Deliberately write NO minute candles for this window.
        CandleTime window = AlignedWindow(400);

        var (position, _, _) = CreateTakeProfitPosition(db, symbol, window.ToDateTime().AddHours(-1));
        var baseCandle = MakeCandle(window, open: 50m, high: TakeProfitPrice + 1m, low: StopLossPrice - 1m, close: 50m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, baseCandle, BaseDuration);

        Assert.AreEqual(1, position.OrderList.Count, "Exactly one fill expected");
        Assert.AreEqual(StopLossPrice, position.OrderList.Values[0].Price,
            "Without finer data the pessimistic reading applies: stop loss before take profit");
    }


    [TestMethod]
    public async Task Intrabar_OnlyOneLevelTouched_DoesNotNeedFinerCandles()
    {
        var (db, symbol) = SetupTestEnvironment();
        // No minute candles written — irrelevant, because a single touched level is unambiguous
        // and the drill-down must not even be attempted.
        CandleTime window = AlignedWindow(500);

        var (position, _, _) = CreateTakeProfitPosition(db, symbol, window.ToDateTime().AddHours(-1));

        // High reaches the take profit, low stays well above the stop loss.
        var baseCandle = MakeCandle(window, open: 50m, high: TakeProfitPrice + 1m, low: 48m, close: 50m);

        await PaperTrading.PaperTradingCheckOrders(db, GlobalData.ActiveExchange!, symbol, baseCandle, BaseDuration);

        Assert.AreEqual(1, position.OrderList.Count, "Exactly one fill expected");
        Assert.AreEqual(TakeProfitPrice, position.OrderList.Values[0].Price,
            "Only the take profit was touched, so that is the fill");
    }
}
