using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// INJUSDT.PERP on 18/19-10-2026 (Binance Futures Paper, storsi.multi long). The scanner entered at
/// 8.222 and placed its first DCA at 7.819, Altrady filled at 8.194 and measured its DCA of 5% from
/// there: 7.784. The bottom at 7.791 filled only the scanner's DCA, the scanner closed on its take
/// profit with +1%, and the close signal sold Altrady's single entry at -1.66%.
/// <para>
/// Two rules came out of it: the DCA's go to Altrady as the scanner's own prices
/// (TradeTools.DcaGridAnchor), and a position that leaves through its take profit sends no close
/// (ThreadCheckFinishedPosition.ClosedOnTakeProfit).
/// </para>
/// </summary>
[TestClass]
public class AltradyDcaAndCloseTests
{
    private static CryptoPosition MakePosition(CryptoTradeSide side = CryptoTradeSide.Long)
    {
        var exchange = new Exchange { Id = 1, Name = "Binance Perpetual", FeeRate = 0.05m };
        var symbol = new CryptoSymbol
        {
            Id = 1,
            Name = "INJUSDT.PERP",
            Base = "INJ",
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDT"),
            PriceTickSize = 0.001m,
        };
        return new CryptoPosition
        {
            Id = 1372,
            CreateTime = new DateTime(2026, 10, 18, 13, 30, 0, DateTimeKind.Utc),
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = symbol,
            SymbolId = 1,
            Interval = new CryptoInterval { Id = 4, Name = "5m", IntervalPeriod = CryptoIntervalPeriod.interval5m, Duration = 300 },
            Side = side,
            Status = CryptoPositionStatus.Trading,
            EntryPrice = 8.222m,
            HasOrdersAndTradesLoaded = true,
        };
    }

    private static CryptoPositionStep AddExit(CryptoPosition position, int id, decimal price, decimal averagePrice,
        decimal? stopPrice, DateTime closeTime, CryptoOrderStatus status = CryptoOrderStatus.PartiallyAndClosed)
    {
        if (!position.PartList.TryGetValue(10, out CryptoPositionPart? part))
        {
            part = new CryptoPositionPart { Id = 10, PositionId = position.Id, Position = position, Exchange = position.Exchange, Symbol = position.Symbol, Purpose = CryptoPartPurpose.TakeProfit };
            position.PartList.TryAdd(part.Id, part);
        }
        var step = new CryptoPositionStep
        {
            Id = id,
            PositionId = position.Id,
            PositionPartId = part.Id,
            Side = position.GetTakeProfitOrderSide(),
            OrderType = CryptoOrderType.Limit,
            Status = status,
            Price = price,
            StopPrice = stopPrice,
            AveragePrice = averagePrice,
            Quantity = 37.8m,
            QuantityFilled = averagePrice > 0 ? 37.8m : 0m,
            CloseTime = closeTime,
        };
        part.StepList.TryAdd(step.Id, step);
        return step;
    }

    private decimal _savedDays;
    private decimal _savedStop;
    private decimal _savedStopLimit;

    [TestInitialize]
    public void SaveSettings()
    {
        _savedDays = GlobalData.Settings.Trading.MaxPositionDurationDays;
        _savedStop = GlobalData.Settings.Trading.StopLossPercentage;
        _savedStopLimit = GlobalData.Settings.Trading.StopLossLimitPercentage;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = _savedDays;
        GlobalData.Settings.Trading.StopLossPercentage = _savedStop;
        GlobalData.Settings.Trading.StopLossLimitPercentage = _savedStopLimit;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The stop price Altrady gets
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Stop_IsTheScannersOwnStopForInj()
    {
        GlobalData.Settings.Trading.StopLossPercentage = 20m;
        GlobalData.Settings.Trading.StopLossLimitPercentage = 21m;
        var position = MakePosition();

        // Deepest DCA 10% + stop 20% from the entry 8.222: the stop of position 1372 was 5.756
        decimal? stop = AltradyWebhook.InitialStopPrice(position, 10m);

        Assert.AreEqual(5.756m, stop!.Value, 0.001m);
    }

    [TestMethod]
    public void Stop_SignalStopIsMeasuredFromTheEntry()
    {
        GlobalData.Settings.Trading.StopLossPercentage = 20m;
        var position = MakePosition();
        position.SlPercentage = 4m;

        // 8.222 x 0.96 = 7.89312, the deepest DCA does not count for a signal stop
        Assert.AreEqual(7.893m, AltradyWebhook.InitialStopPrice(position, 10m)!.Value, 0.001m);
    }

    [TestMethod]
    public void Stop_WithoutEntryPriceFallsBackToThePercentage()
    {
        var position = MakePosition();
        position.EntryPrice = null;

        Assert.IsNull(AltradyWebhook.InitialStopPrice(position, 10m));
        var block = AltradyWebhook.BuildStopLossBlock(30m, false, CryptoProfitLockMethod.TrailingPercentage, 0m, 0m, 1, null);
        Assert.AreEqual(30m, (decimal?)block["stop_percentage"]);
        Assert.IsNull(block["stop_price"]);
    }

    [TestMethod]
    public void Stop_BlockCarriesThePriceInsteadOfThePercentage()
    {
        var block = AltradyWebhook.BuildStopLossBlock(30m, false, CryptoProfitLockMethod.TrailingPercentage, 0m, 0m, 1, 5.756m);

        Assert.AreEqual(5.756m, (decimal?)block["stop_price"]);
        Assert.IsNull(block["stop_percentage"], "Altrady takes stop_price or stop_percentage, not both");
    }

    [TestMethod]
    public void Stop_PriceKeepsTheTrailingProtection()
    {
        var block = AltradyWebhook.BuildStopLossBlock(30m, true, CryptoProfitLockMethod.TrailingPercentage, 2m, 1m, 1, 5.756m);

        Assert.AreEqual(5.756m, (decimal?)block["stop_price"]);
        Assert.AreEqual("PRICE", (string?)block["protection_type"]);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The DCA prices Altrady gets
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BeforeTheFill_AnchorIsTheEntryPlusBothFees()
    {
        var position = MakePosition();

        // 8.222 x (1 + 2 x 0.05%) = 8.230222
        Assert.AreEqual(8.230222m, TradeTools.DcaGridAnchor(position));
    }

    [TestMethod]
    public void BeforeTheFill_PredictsTheDcaPricesTheScannerPlacedForInj()
    {
        var position = MakePosition();
        decimal anchor = TradeTools.DcaGridAnchor(position);

        decimal dca1 = PricePlacement.Adverse(position.Side, anchor, 5m)
            .ClampPrice(position.Side, 0m, 0m, position.Symbol.PriceTickSize);
        decimal dca2 = PricePlacement.Adverse(position.Side, anchor, 10m)
            .ClampPrice(position.Side, 0m, 0m, position.Symbol.PriceTickSize);

        // The DCA orders of position 1372 in the database: 7.819 and 7.408
        Assert.AreEqual(7.819m, dca1, 0.001m);
        Assert.AreEqual(7.408m, dca2, 0.001m);
        // Altrady's own 7.784 would not have filled on the bottom of 7.791, this one does
        Assert.IsTrue(dca1 > 7.791m);
    }

    [TestMethod]
    public void AfterTheFill_AnchorIsTheBreakEvenOfTheGrid()
    {
        var position = MakePosition();
        position.TpGridBreakEvenPrice = 8.2302m;

        Assert.AreEqual(8.2302m, TradeTools.DcaGridAnchor(position));
    }

    [TestMethod]
    public void Short_AnchorIsTheEntryMinusBothFees()
    {
        var position = MakePosition(CryptoTradeSide.Short);

        Assert.AreEqual(8.213778m, TradeTools.DcaGridAnchor(position));
    }

    [TestMethod]
    public void StrategyLadder_IsMeasuredFromTheSignalPrice()
    {
        var position = MakePosition();
        position.DcaLevels = DcaLadder.Format([new CryptoDcaEntry { Factor = 200m, Percentage = 3m }]);
        position.SignalPrice = 8.100m;

        Assert.AreEqual(8.100m, TradeTools.DcaGridAnchor(position));
    }

    [TestMethod]
    public void WithoutAnyPrice_AnchorIsZero()
    {
        var position = MakePosition();
        position.EntryPrice = null;

        Assert.AreEqual(0m, TradeTools.DcaGridAnchor(position));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The close signal
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TakeProfitFill_SendsNoClose()
    {
        var position = MakePosition();
        // The take profit of position 1372: limit 8.037 with the shared stop 5.756, filled on its limit
        AddExit(position, 215777, 8.037m, 8.037m, 5.756m, new DateTime(2026, 10, 18, 22, 1, 0, DateTimeKind.Utc));

        Assert.IsTrue(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void RealFill_StoredAsFilled_SendsNoClose()
    {
        var position = MakePosition();
        AddExit(position, 1, 8.037m, 8.037m, 5.756m, new DateTime(2026, 10, 18, 22, 1, 0, DateTimeKind.Utc), CryptoOrderStatus.Filled);

        Assert.IsTrue(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void StopFill_SendsTheClose()
    {
        var position = MakePosition();
        AddExit(position, 1, 8.037m, 5.756m, 5.756m, new DateTime(2026, 10, 18, 22, 1, 0, DateTimeKind.Utc));

        Assert.IsFalse(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void TrailFill_CountsAsTheTakeProfit()
    {
        var position = MakePosition();
        position.TakeProfitTrailStopPrice = 8.050m;
        AddExit(position, 1, 8.100m, 8.050m, 8.050m, new DateTime(2026, 10, 18, 22, 1, 0, DateTimeKind.Utc));

        Assert.IsTrue(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void PastMaxDuration_SendsTheClose()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 7m;
        var position = MakePosition();
        // Past the deadline the take profit is repriced to the market and fills on its own limit
        AddExit(position, 1, 8.100m, 8.100m, 5.756m, new DateTime(2026, 10, 25, 14, 0, 0, DateTimeKind.Utc));

        Assert.IsFalse(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 25, 14, 0, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void StrategyExit_SendsTheClose()
    {
        var position = MakePosition();
        position.ExitRequested = true;
        AddExit(position, 1, 8.100m, 8.100m, 5.756m, new DateTime(2026, 10, 18, 22, 1, 0, DateTimeKind.Utc));

        Assert.IsFalse(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void OnlyTheLastExitCounts()
    {
        var position = MakePosition();
        // First take profit level on its limit, the rest stopped out afterwards
        AddExit(position, 1, 8.300m, 8.300m, 7.000m, new DateTime(2026, 10, 18, 20, 0, 0, DateTimeKind.Utc));
        AddExit(position, 2, 8.500m, 7.000m, 7.000m, new DateTime(2026, 10, 18, 22, 0, 0, DateTimeKind.Utc));

        Assert.IsFalse(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }

    [TestMethod]
    public void NoExitAtAll_SendsTheClose()
    {
        var position = MakePosition();

        Assert.IsFalse(ThreadCheckFinishedPosition.ClosedOnTakeProfit(position, new DateTime(2026, 10, 18, 22, 1, 11, DateTimeKind.Utc)));
    }
}
